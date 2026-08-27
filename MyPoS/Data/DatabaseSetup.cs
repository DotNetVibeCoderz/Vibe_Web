using System;
using Microsoft.EntityFrameworkCore;

namespace MyPoS.Data
{
    /// <summary>Penyedia basis data yang didukung.</summary>
    public enum DatabaseProvider
    {
        Sqlite,
        SqlServer,
        PostgreSql,
        MySql
    }

    /// <summary>Bagian "Database" pada appsettings.json.</summary>
    public class DatabaseOptions
    {
        public string Provider { get; set; } = "Sqlite";
        public string ConnectionString { get; set; } = "";

        public DatabaseProvider Resolved => Provider?.Trim().ToLowerInvariant() switch
        {
            "sqlserver" or "mssql" => DatabaseProvider.SqlServer,
            "postgres" or "postgresql" or "npgsql" => DatabaseProvider.PostgreSql,
            "mysql" or "mariadb" => DatabaseProvider.MySql,
            _ => DatabaseProvider.Sqlite
        };
    }

    public static class DatabaseSetup
    {
        /// <summary>
        /// Mendaftarkan <see cref="AppDbContext"/> pada penyedia yang dipilih di konfigurasi.
        /// SQLite tetap menjadi bawaan supaya aplikasi dapat langsung dijalankan tanpa
        /// menyiapkan server basis data apa pun.
        /// </summary>
        public static IServiceCollection AddMyPosDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
            var provider = options.Resolved;

            var connectionString = !string.IsNullOrWhiteSpace(options.ConnectionString)
                ? options.ConnectionString
                : configuration.GetConnectionString("Default") ?? DefaultConnectionString(provider);

            if (provider == DatabaseProvider.PostgreSql)
            {
                // Npgsql memetakan DateTime ke "timestamp with time zone" dan menolak nilai
                // ber-Kind Local. Transaksi dicatat memakai waktu lokal toko, jadi perilaku
                // lama - yang memetakannya ke "timestamp without time zone" - dipertahankan.
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            }

            services.AddSingleton(options);
            services.AddDbContextFactory<AppDbContext>(builder =>
            {
                switch (provider)
                {
                    // Percobaan ulang dibatasi tiga kali: basis data server memang bisa
                    // terputus sesaat, tetapi nilai bawaan EF (enam kali dengan jeda
                    // membesar) membuat startup menggantung bermenit-menit ketika
                    // servernya memang tidak ada.
                    case DatabaseProvider.SqlServer:
                        builder.UseSqlServer(connectionString, sql =>
                            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                        break;
                    case DatabaseProvider.PostgreSql:
                        builder.UseNpgsql(connectionString, npg =>
                            npg.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                        break;
                    case DatabaseProvider.MySql:
                        builder.UseMySQL(connectionString);
                        break;
                    default:
                        builder.UseSqlite(connectionString);
                        break;
                }
            });

            return services;
        }

        private static string DefaultConnectionString(DatabaseProvider provider) => provider switch
        {
            DatabaseProvider.SqlServer => "Server=localhost;Database=MyPoS;Trusted_Connection=True;TrustServerCertificate=True",
            DatabaseProvider.PostgreSql => "Host=localhost;Port=5432;Database=mypos;Username=postgres;Password=postgres",
            DatabaseProvider.MySql => "Server=localhost;Port=3306;Database=mypos;User=root;Password=root",
            _ => "Data Source=mypos.db"
        };
    }
}
