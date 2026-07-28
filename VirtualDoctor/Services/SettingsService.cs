using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

public interface ISettingsService
{
    /// <summary>Semua override yang tersimpan di database.</summary>
    Task<Dictionary<string, string?>> GetOverridesAsync();
    /// <summary>Simpan override lalu langsung terapkan ke AppConfig yang aktif.</summary>
    Task SaveAsync(IDictionary<string, string?> values, string? updatedBy = null);
    /// <summary>Hapus override sehingga nilai kembali mengikuti appsettings.json (perlu restart).</summary>
    Task ResetAsync(string keyPrefix);
    /// <summary>Baca nilai berjalan dari AppConfig lewat path, mis. "Llm:OpenAI:Model".</summary>
    string? ReadCurrent(string path);
}

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    private readonly AppConfig _config;
    private readonly ILogger<SettingsService> _log;

    public SettingsService(AppDbContext db, AppConfig config, ILogger<SettingsService> log)
    { _db = db; _config = config; _log = log; }

    public async Task<Dictionary<string, string?>> GetOverridesAsync() =>
        await _db.AppSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);

    public async Task SaveAsync(IDictionary<string, string?> values, string? updatedBy = null)
    {
        foreach (var (key, value) in values)
        {
            var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (existing == null)
            {
                _db.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    IsSecret = LooksSecret(key),
                    UpdatedBy = updatedBy,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Value = value;
                existing.UpdatedBy = updatedBy;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            ApplyToConfig(_config, key, value);
        }
        await _db.SaveChangesAsync();
        _log.LogInformation("[Settings] {N} pengaturan disimpan oleh {By}", values.Count, updatedBy ?? "-");
    }

    public async Task ResetAsync(string keyPrefix)
    {
        var rows = await _db.AppSettings.Where(s => s.Key.StartsWith(keyPrefix)).ToListAsync();
        _db.AppSettings.RemoveRange(rows);
        await _db.SaveChangesAsync();
    }

    public string? ReadCurrent(string path) => ReadFromConfig(_config, path);

    public static bool LooksSecret(string key) =>
        key.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("AccessKey", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("ServerKey", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Dipanggil sekali saat startup: override dari database menimpa appsettings.json.
    /// </summary>
    public static async Task ApplyStoredOverridesAsync(AppDbContext db, AppConfig config, ILogger? log = null)
    {
        List<AppSetting> rows;
        try { rows = await db.AppSettings.AsNoTracking().ToListAsync(); }
        catch { return; } // tabel belum ada (database lama)

        foreach (var row in rows)
        {
            try { ApplyToConfig(config, row.Key, row.Value); }
            catch (Exception ex) { log?.LogWarning(ex, "[Settings] Gagal menerapkan {Key}", row.Key); }
        }
        if (rows.Count > 0) log?.LogInformation("[Settings] {N} override diterapkan dari database", rows.Count);
    }

    // ---- reflection helpers: path "Llm:OpenAI:Model" -> config.Llm.OpenAI.Model ----

    private static (object? Owner, PropertyInfo? Prop) Resolve(AppConfig config, string path, bool createMissing)
    {
        var parts = path.Split(':', StringSplitOptions.RemoveEmptyEntries);
        object? current = config;

        for (var i = 0; i < parts.Length; i++)
        {
            if (current == null) return (null, null);
            var prop = current.GetType().GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return (null, null);

            if (i == parts.Length - 1) return (current, prop);

            var next = prop.GetValue(current);
            if (next == null)
            {
                if (!createMissing || prop.PropertyType.GetConstructor(Type.EmptyTypes) == null) return (null, null);
                next = Activator.CreateInstance(prop.PropertyType);
                prop.SetValue(current, next);
            }
            current = next;
        }
        return (null, null);
    }

    private static void ApplyToConfig(AppConfig config, string path, string? value)
    {
        var (owner, prop) = Resolve(config, path, createMissing: true);
        if (owner == null || prop == null || !prop.CanWrite) return;

        var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        object? converted;

        if (string.IsNullOrEmpty(value) && target != typeof(string))
            converted = null;
        else if (target == typeof(string)) converted = value;
        else if (target == typeof(int)) converted = int.TryParse(value, out var i) ? i : 0;
        else if (target == typeof(double)) converted = double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0d;
        else if (target == typeof(decimal)) converted = decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m) ? m : 0m;
        else if (target == typeof(long)) converted = long.TryParse(value, out var l) ? l : 0L;
        else if (target == typeof(bool)) converted = bool.TryParse(value, out var b) && b;
        else if (target.IsEnum) converted = Enum.TryParse(target, value, true, out var e) ? e : null;
        else return;

        prop.SetValue(owner, converted);
    }

    private static string? ReadFromConfig(AppConfig config, string path)
    {
        var (owner, prop) = Resolve(config, path, createMissing: false);
        if (owner == null || prop == null) return null;
        var val = prop.GetValue(owner);
        return val switch
        {
            double dv => dv.ToString(System.Globalization.CultureInfo.InvariantCulture),
            decimal mv => mv.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => val?.ToString()
        };
    }
}
