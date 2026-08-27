using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyPoS.Services;

namespace MyPoS.Data
{
    /// <summary>
    /// Menyiapkan basis data saat aplikasi menyala.
    ///
    /// Proyek ini memakai EnsureCreated, bukan migrasi EF, sehingga perubahan pada entitas
    /// membuat basis data lama tidak lagi cocok dengan model. Untuk SQLite - yang memang
    /// ditujukan sebagai basis data pengembangan - berkasnya disalin lebih dulu ke
    /// <c>mypos.db.bak-yyyyMMdd-HHmmss</c> lalu dibuat ulang.
    ///
    /// Untuk SQL Server, PostgreSQL, dan MySQL, basis data TIDAK PERNAH dihapus otomatis.
    /// Menghapus basis data server adalah tindakan yang tidak dapat dibatalkan dan mungkin
    /// dipakai bersama aplikasi lain; yang terjadi hanyalah peringatan di log agar
    /// perbedaan skema ditangani dengan migrasi yang sesungguhnya.
    /// </summary>
    public static class DatabaseBootstrapper
    {
        public static async Task InitialiseAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var options = scope.ServiceProvider.GetRequiredService<DatabaseOptions>();

            using (var db = await factory.CreateDbContextAsync())
            {
                logger.LogInformation("Penyedia basis data: {Provider}", options.Resolved);

                if (!await CanConnectAsync(db, logger, options))
                    return;

                // Basis data yang benar-benar kosong bukan pertanda skema berubah, melainkan
                // pemasangan baru: EnsureCreated di bawah akan mengisinya tanpa peringatan.
                var creator = db.Database.GetService<IRelationalDatabaseCreator>();
                var hasTables = await creator.HasTablesAsync();

                if (hasTables && !await SchemaMatchesModelAsync(db))
                {
                    if (options.Resolved == DatabaseProvider.Sqlite)
                    {
                        var path = GetSqliteFilePath(db);
                        BackupSqliteFile(path, logger);

                        logger.LogWarning(
                            "Skema tidak lagi cocok dengan model entitas, basis data SQLite dibuat ulang " +
                            "beserta data contoh. Salinan data lama tersimpan di samping {Path}.",
                            path ?? "(tidak diketahui)");

                        await db.Database.EnsureDeletedAsync();
                    }
                    else
                    {
                        logger.LogError(
                            "Skema basis data {Provider} tidak cocok dengan model entitas. Basis data server " +
                            "tidak dihapus otomatis - terapkan perubahan skema secara manual atau lewat migrasi EF.",
                            options.Resolved);
                        return;
                    }
                }

                await db.Database.EnsureCreatedAsync();
            }

            // Nilai bawaan halaman Pengaturan ditulis sekali agar halaman tidak tampil kosong.
            var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();
            await settings.EnsureSeededAsync();
        }

        /// <summary>Berapa lama startup boleh menunggu basis data server sebelum menyerah.</summary>
        private static readonly TimeSpan ConnectProbeTimeout = TimeSpan.FromSeconds(20);

        /// <summary>
        /// SQLite membuat berkasnya sendiri, tetapi basis data server harus sudah ada dan
        /// dapat dihubungi. Kegagalan koneksi dilaporkan sebagai pesan yang jelas, bukan
        /// sebagai tumpukan galat saat halaman pertama dibuka.
        ///
        /// Percobaan dibatasi waktu: tanpa batas ini, strategi percobaan ulang EF akan
        /// menahan startup bermenit-menit ketika servernya memang tidak dapat dijangkau,
        /// sehingga aplikasi tampak menggantung tanpa penjelasan.
        /// </summary>
        private static async Task<bool> CanConnectAsync(AppDbContext db, ILogger logger, DatabaseOptions options)
        {
            if (options.Resolved == DatabaseProvider.Sqlite) return true;

            using var timeout = new CancellationTokenSource(ConnectProbeTimeout);

            try
            {
                if (await db.Database.CanConnectAsync(timeout.Token)) return true;

                logger.LogError(
                    "Basis data {Provider} menolak koneksi. Periksa Database:ConnectionString " +
                    "pada appsettings.json dan pastikan basis datanya sudah dibuat.", options.Resolved);
                return false;
            }
            catch (OperationCanceledException)
            {
                logger.LogError(
                    "Basis data {Provider} tidak menjawab dalam {Detik} detik. Aplikasi tetap dijalankan, " +
                    "tetapi halaman yang membaca data akan gagal sampai koneksinya diperbaiki.",
                    options.Resolved, ConnectProbeTimeout.TotalSeconds);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Tidak dapat menghubungi basis data {Provider}. Periksa Database:ConnectionString " +
                    "pada appsettings.json dan pastikan servernya berjalan.", options.Resolved);
                return false;
            }
        }

        /// <summary>
        /// Menyentuh setiap tabel sekali. Pemeriksaan lama hanya menyentuh Products, sehingga
        /// perubahan pada entitas lain lolos dan baru meledak saat halaman dibuka pengguna.
        /// </summary>
        private static async Task<bool> SchemaMatchesModelAsync(AppDbContext db)
        {
            try
            {
                await db.Products.AsNoTracking().FirstOrDefaultAsync();
                await db.Categories.AsNoTracking().FirstOrDefaultAsync();
                await db.Customers.AsNoTracking().FirstOrDefaultAsync();
                await db.Users.AsNoTracking().FirstOrDefaultAsync();
                await db.Transactions.AsNoTracking().FirstOrDefaultAsync();
                await db.TransactionDetails.AsNoTracking().FirstOrDefaultAsync();
                await db.Settings.AsNoTracking().FirstOrDefaultAsync();
                await db.ApiKeys.AsNoTracking().FirstOrDefaultAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string? GetSqliteFilePath(AppDbContext db)
        {
            try
            {
                var dataSource = db.Database.GetDbConnection().DataSource;
                return string.IsNullOrWhiteSpace(dataSource) ? null : Path.GetFullPath(dataSource);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void BackupSqliteFile(string? path, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

            try
            {
                var backup = $"{path}.bak-{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(path, backup, overwrite: false);
                logger.LogWarning("Basis data lama disalin ke {Backup}", backup);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gagal menyalin basis data lama sebelum dibuat ulang.");
            }
        }
    }
}
