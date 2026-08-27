using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;

namespace MyPoS.Services
{
    /// <summary>Hasil pembuatan kunci. <see cref="PlainKey"/> hanya ada satu kali di sini.</summary>
    public record ApiKeyIssued(ApiKey Record, string PlainKey);

    /// <summary>
    /// Pengelolaan kunci REST API.
    ///
    /// Kunci penuh tidak pernah disimpan — yang tersimpan hanyalah hash PBKDF2-nya, sama
    /// seperti kata sandi pengguna. Delapan karakter acak pertamanya disimpan terpisah
    /// sebagai <c>Prefix</c> supaya pencarian saat verifikasi tetap satu kueri berindeks,
    /// tanpa perlu mencocokkan hash seluruh baris.
    /// </summary>
    public class ApiKeyService
    {
        private const string KeyPrefix = "mps_";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ApiKeyService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<ApiKey>> ListAsync(CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            return await db.ApiKeys.OrderByDescending(k => k.CreatedAt).AsNoTracking().ToListAsync(ct);
        }

        public async Task<ApiKeyIssued> CreateAsync(string name, bool canWrite, DateTime? expiresAt, CancellationToken ct = default)
        {
            // 32 byte acak yang disandikan base64url: cukup panjang untuk tidak dapat ditebak,
            // dan aman dipakai di header maupun URL.
            var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "").Replace("/", "").Replace("=", "");

            var plainKey = KeyPrefix + secret;
            var prefix = plainKey[..12];

            var record = new ApiKey
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Tanpa nama" : name.Trim(),
                Prefix = prefix,
                KeyHash = PasswordHasher.Hash(plainKey),
                CanWrite = canWrite,
                IsActive = true,
                CreatedAt = DateTime.Now,
                ExpiresAt = expiresAt
            };

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            db.ApiKeys.Add(record);
            await db.SaveChangesAsync(ct);

            return new ApiKeyIssued(record, plainKey);
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var record = await db.ApiKeys.FindAsync([id], ct);
            if (record is null) return false;

            record.IsActive = isActive;
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var record = await db.ApiKeys.FindAsync([id], ct);
            if (record is null) return false;

            db.ApiKeys.Remove(record);
            await db.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Memeriksa kunci yang dikirim pemanggil. Mengembalikan null bila kunci tidak dikenal,
        /// dinonaktifkan, atau sudah kedaluwarsa.
        /// </summary>
        public async Task<ApiKey?> ValidateAsync(string? presentedKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(presentedKey) || presentedKey.Length < 16) return null;
            if (!presentedKey.StartsWith(KeyPrefix, StringComparison.Ordinal)) return null;

            var prefix = presentedKey[..12];

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var record = await db.ApiKeys.FirstOrDefaultAsync(k => k.Prefix == prefix, ct);

            if (record is null) return null;
            if (!record.IsActive) return null;
            if (record.ExpiresAt is DateTime expiry && expiry < DateTime.Now) return null;
            if (!PasswordHasher.Verify(presentedKey, record.KeyHash)) return null;

            // Jejak pemakaian terakhir membantu menemukan kunci yang sudah tidak dipakai lagi.
            record.LastUsedAt = DateTime.Now;
            await db.SaveChangesAsync(ct);

            return record;
        }
    }
}
