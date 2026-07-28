using Microsoft.EntityFrameworkCore;

namespace VirtualDoctor.Data;

/// <summary>
/// Proyek ini memakai EnsureCreated, bukan EF Migrations. Akibatnya database
/// yang sudah ada tidak ikut berubah saat model bertambah. Kelas ini menambal
/// selisihnya secara idempoten supaya data lama tidak perlu dihapus.
///
/// Catatan: hanya SQLite yang ditangani otomatis. Provider lain diberi peringatan
/// agar perubahan skema dijalankan lewat migrasi resmi.
/// </summary>
public static class SchemaUpgrader
{
    private record ColumnPatch(string Table, string Column, string SqliteType);

    private static readonly ColumnPatch[] Columns =
    {
        new("Consultations", "MeetingProvider", "TEXT"),
        new("Consultations", "MeetingId", "TEXT"),
        new("Consultations", "MeetingUrl", "TEXT"),
        new("Consultations", "MeetingHostUrl", "TEXT"),
        new("Consultations", "MeetingPassword", "TEXT"),
        new("Appointments", "MeetingProvider", "TEXT"),
        new("Appointments", "MeetingId", "TEXT"),
        new("Appointments", "MeetingUrl", "TEXT"),
        new("Appointments", "MeetingHostUrl", "TEXT"),
        new("Appointments", "MeetingPassword", "TEXT")
    };

    public static async Task UpgradeAsync(AppDbContext db, string provider, ILogger logger)
    {
        if (!provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("[Schema] Provider {P}: lewati penambalan otomatis, gunakan migrasi EF bila skema berubah", provider);
            return;
        }

        var applied = 0;

        // Tabel pengaturan
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                    ""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY,
                    ""Value"" TEXT NULL,
                    ""IsSecret"" INTEGER NOT NULL DEFAULT 0,
                    ""UpdatedBy"" TEXT NULL,
                    ""UpdatedAt"" TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'
                );");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Schema] Gagal memastikan tabel AppSettings");
        }

        // Tabel peran pengguna
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""UserRoles"" (
                    ""UserId"" TEXT NOT NULL,
                    ""Role"" TEXT NOT NULL,
                    ""GrantedAt"" TEXT NOT NULL DEFAULT '1970-01-01 00:00:00',
                    ""GrantedBy"" TEXT NULL,
                    CONSTRAINT ""PK_UserRoles"" PRIMARY KEY (""UserId"", ""Role"")
                );");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Schema] Gagal memastikan tabel UserRoles");
        }

        // Tabel pembayaran
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Payments"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_Payments"" PRIMARY KEY,
                    ""InvoiceNumber"" TEXT NOT NULL,
                    ""ReferenceType"" INTEGER NOT NULL DEFAULT 0,
                    ""ReferenceId"" TEXT NOT NULL DEFAULT '',
                    ""UserId"" TEXT NOT NULL DEFAULT '',
                    ""Description"" TEXT NOT NULL DEFAULT '',
                    ""Amount"" TEXT NOT NULL DEFAULT '0',
                    ""ServiceFee"" TEXT NOT NULL DEFAULT '0',
                    ""Discount"" TEXT NOT NULL DEFAULT '0',
                    ""InsuranceCoverage"" TEXT NOT NULL DEFAULT '0',
                    ""Total"" TEXT NOT NULL DEFAULT '0',
                    ""Channel"" INTEGER NOT NULL DEFAULT 0,
                    ""Provider"" TEXT NOT NULL DEFAULT 'Manual',
                    ""State"" INTEGER NOT NULL DEFAULT 0,
                    ""ExternalId"" TEXT NULL,
                    ""QrPayload"" TEXT NULL,
                    ""PaymentUrl"" TEXT NULL,
                    ""VirtualAccountNumber"" TEXT NULL,
                    ""ProofUrl"" TEXT NULL,
                    ""PayerNote"" TEXT NULL,
                    ""VerifiedBy"" TEXT NULL,
                    ""VerifiedAt"" TEXT NULL,
                    ""VerificationNote"" TEXT NULL,
                    ""CreatedAt"" TEXT NOT NULL DEFAULT '1970-01-01 00:00:00',
                    ""ExpiresAt"" TEXT NULL,
                    ""PaidAt"" TEXT NULL
                );");
            await db.Database.ExecuteSqlRawAsync(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Payments_InvoiceNumber"" ON ""Payments"" (""InvoiceNumber"");");
            await db.Database.ExecuteSqlRawAsync(
                @"CREATE INDEX IF NOT EXISTS ""IX_Payments_Reference"" ON ""Payments"" (""ReferenceType"", ""ReferenceId"");");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Schema] Gagal memastikan tabel Payments");
        }

        // Penomoran invoice (P1-8) — sumber tunggal urutan nomor per bulan
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""InvoiceCounters"" (
                    ""Prefix"" TEXT NOT NULL CONSTRAINT ""PK_InvoiceCounters"" PRIMARY KEY,
                    ""LastNumber"" INTEGER NOT NULL DEFAULT 0,
                    ""UpdatedAt"" TEXT NOT NULL DEFAULT '1970-01-01 00:00:00'
                );");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Schema] Gagal memastikan tabel InvoiceCounters");
        }

        // Jejak webhook penyedia bayar (P1-7)
        try
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""PaymentWebhookEvents"" (
                    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_PaymentWebhookEvents"" PRIMARY KEY,
                    ""Provider"" TEXT NOT NULL DEFAULT '',
                    ""InvoiceNumber"" TEXT NULL,
                    ""ExternalId"" TEXT NULL,
                    ""RawStatus"" TEXT NULL,
                    ""MappedState"" INTEGER NULL,
                    ""Fingerprint"" TEXT NOT NULL DEFAULT '',
                    ""Payload"" TEXT NOT NULL DEFAULT '',
                    ""SignatureValid"" INTEGER NOT NULL DEFAULT 0,
                    ""Outcome"" INTEGER NOT NULL DEFAULT 0,
                    ""Message"" TEXT NULL,
                    ""ReceivedAt"" TEXT NOT NULL DEFAULT '1970-01-01 00:00:00',
                    ""ProcessedAt"" TEXT NULL,
                    ""Attempts"" INTEGER NOT NULL DEFAULT 1,
                    ""ReplayedBy"" TEXT NULL,
                    ""ReplayedAt"" TEXT NULL
                );");
            await db.Database.ExecuteSqlRawAsync(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PaymentWebhookEvents_Fingerprint"" ON ""PaymentWebhookEvents"" (""Fingerprint"");");
            await db.Database.ExecuteSqlRawAsync(
                @"CREATE INDEX IF NOT EXISTS ""IX_PaymentWebhookEvents_ReceivedAt"" ON ""PaymentWebhookEvents"" (""ReceivedAt"");");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Schema] Gagal memastikan tabel PaymentWebhookEvents");
        }

        // Kolom meeting
        foreach (var patch in Columns)
        {
            try
            {
                if (await ColumnExistsAsync(db, patch.Table, patch.Column)) continue;
                await db.Database.ExecuteSqlRawAsync(
                    $@"ALTER TABLE ""{patch.Table}"" ADD COLUMN ""{patch.Column}"" {patch.SqliteType} NULL;");
                applied++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Schema] Gagal menambah kolom {T}.{C}", patch.Table, patch.Column);
            }
        }

        if (applied > 0)
            logger.LogInformation("[Schema] {N} kolom baru ditambahkan ke database yang sudah ada", applied);
    }

    private static async Task<bool> ColumnExistsAsync(AppDbContext db, string table, string column)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // kolom ke-1 pada PRAGMA table_info adalah nama
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
