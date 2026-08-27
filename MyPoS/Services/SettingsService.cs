using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;

namespace MyPoS.Services
{
    /// <summary>
    /// Singleton pembaca/penulis <see cref="PosSettings"/>. Nilai di-cache di memori supaya
    /// halaman tidak menembak database tiap kali memformat angka; cache dibuang saat disimpan.
    /// </summary>
    public class SettingsService
    {
        private static readonly PropertyInfo[] Props =
            typeof(PosSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.CanRead && p.CanWrite)
                               .ToArray();

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private PosSettings? _cache;

        /// <summary>Dipicu setelah penyimpanan, dipakai layout untuk merender ulang tema.</summary>
        public event Action<PosSettings>? Changed;

        public SettingsService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>Nilai terakhir yang diketahui; membaca database bila cache masih kosong.</summary>
        public PosSettings Current => _cache ??= LoadCore();

        public async Task<PosSettings> GetAsync()
        {
            if (_cache is not null) return _cache;

            await _gate.WaitAsync();
            try
            {
                return _cache ??= LoadCore();
            }
            finally
            {
                _gate.Release();
            }
        }

        private PosSettings LoadCore()
        {
            var settings = new PosSettings();
            try
            {
                using var db = _dbContextFactory.CreateDbContext();
                var stored = db.Settings.AsNoTracking().ToDictionary(s => s.Key, s => s.Value);
                foreach (var prop in Props)
                {
                    if (stored.TryGetValue(prop.Name, out var raw) && TryConvert(raw, prop.PropertyType, out var value))
                        prop.SetValue(settings, value);
                }
            }
            catch
            {
                // Tabel belum tersedia (startup pertama sebelum EnsureCreated) - pakai nilai bawaan.
            }
            return settings;
        }

        public async Task SaveAsync(PosSettings settings)
        {
            await _gate.WaitAsync();
            try
            {
                using var db = await _dbContextFactory.CreateDbContextAsync();
                var existing = await db.Settings.ToDictionaryAsync(s => s.Key, s => s);

                foreach (var prop in Props)
                {
                    var raw = ToRaw(prop.GetValue(settings));
                    if (existing.TryGetValue(prop.Name, out var row))
                        row.Value = raw;
                    else
                        db.Settings.Add(new AppSetting { Key = prop.Name, Value = raw });
                }

                await db.SaveChangesAsync();
                _cache = settings.Clone();
            }
            finally
            {
                _gate.Release();
            }

            Changed?.Invoke(_cache!);
        }

        /// <summary>Menulis nilai bawaan ke database bila tabel pengaturan masih kosong.</summary>
        public async Task EnsureSeededAsync()
        {
            using var db = await _dbContextFactory.CreateDbContextAsync();
            if (await db.Settings.AnyAsync()) return;
            await SaveAsync(new PosSettings());
        }

        public void Invalidate() => _cache = null;

        private static string ToRaw(object? value) => value switch
        {
            null => "",
            bool b => b ? "true" : "false",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };

        private static bool TryConvert(string raw, Type target, out object? value)
        {
            value = null;
            try
            {
                if (target == typeof(string)) { value = raw; return true; }
                if (target == typeof(bool)) { value = raw.Equals("true", StringComparison.OrdinalIgnoreCase); return true; }
                if (target == typeof(int)) { value = int.Parse(raw, CultureInfo.InvariantCulture); return true; }
                if (target == typeof(decimal)) { value = decimal.Parse(raw, CultureInfo.InvariantCulture); return true; }
            }
            catch
            {
                return false;
            }
            return false;
        }
    }
}
