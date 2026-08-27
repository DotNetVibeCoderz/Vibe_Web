using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;

namespace MyPoS.Services.Import
{
    public class CustomerImporter : IMasterDataImporter
    {
        private const string ColName = "Nama Pelanggan";
        private const string ColPhone = "Telepon";
        private const string ColEmail = "Email";
        private const string ColPoints = "Poin Loyalitas";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CustomerImporter(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public string Key => "pelanggan";
        public string DisplayName => "Pelanggan";

        public IReadOnlyList<ImportColumn> Columns { get; } = new[]
        {
            new ImportColumn(ColName, true, "Nama pelanggan.", "Budi Santoso"),
            new ImportColumn(ColPhone, false, "Nomor telepon. Dipakai untuk mencocokkan pelanggan yang sudah ada.", "081234567890"),
            new ImportColumn(ColEmail, false, "Alamat email. Dipakai penyedia pembayaran untuk mengirim tautan tagihan.", "budi@mail.com"),
            new ImportColumn(ColPoints, false, "Poin awal. Kosong dianggap nol.", "0")
        };

        public Task<byte[]> BuildTemplateAsync(CancellationToken ct = default)
        {
            var samples = new List<IReadOnlyList<string>>
            {
                new[] { $"{ExcelImportHelper.SampleMarker} Budi Santoso", "081234567890", "budi@mail.com", "0" }
            };

            return Task.FromResult(ExcelImportHelper.BuildTemplate(DisplayName, Columns, samples));
        }

        private sealed class ParsedCustomer
        {
            public int? ExistingId { get; init; }
            public string Name { get; init; } = "";
            public string? Phone { get; init; }
            public string? Email { get; init; }
            public int Points { get; init; }
        }

        public async Task<ImportPreview> ParseAsync(Stream file, ImportOptions options, CancellationToken ct = default)
        {
            var preview = new ImportPreview();

            using var handle = ExcelImportHelper.OpenDataSheet(file, Columns, preview);
            if (handle is null) return preview;

            var sheet = handle.Sheet;
            var map = handle.HeaderMap;

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var existing = await db.Customers.AsNoTracking()
                .Select(c => new { c.Id, c.Name, c.Phone, c.Email })
                .ToListAsync(ct);

            var byPhone = existing
                .Where(c => !string.IsNullOrWhiteSpace(c.Phone))
                .GroupBy(c => c.Phone!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var byEmail = existing
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .GroupBy(c => c.Email!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var phonesInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastRow = ExcelImportHelper.LastDataRow(sheet);

            for (var row = 2; row <= lastRow; row++)
            {
                if (ExcelImportHelper.IsRowEmpty(sheet, row, map)) continue;
                if (ExcelImportHelper.IsSampleRow(sheet, row, map, ColName)) continue;

                var result = new ImportRowResult { RowNumber = row };

                var name = ExcelImportHelper.ReadText(sheet, row, map, ColName);
                var phone = ExcelImportHelper.ReadText(sheet, row, map, ColPhone);
                var email = ExcelImportHelper.ReadText(sheet, row, map, ColEmail);

                result.Summary = name.Length > 0 ? name : "(nama kosong)";

                if (name.Length == 0) result.Errors.Add($"{ColName} wajib diisi.");
                if (name.Length > 150) result.Errors.Add($"{ColName} maksimal 150 karakter.");
                if (phone.Length > 40) result.Errors.Add($"{ColPhone} maksimal 40 karakter.");
                if (email.Length > 200) result.Errors.Add($"{ColEmail} maksimal 200 karakter.");

                if (email.Length > 0 && !email.Contains('@'))
                    result.Errors.Add($"{ColEmail} tidak terlihat seperti alamat email yang sah.");

                ExcelImportHelper.TryReadInt(sheet, row, map, ColPoints, out var points);
                if (points < 0) result.Errors.Add($"{ColPoints} tidak boleh negatif.");

                if (phone.Length > 0)
                {
                    if (phonesInFile.TryGetValue(phone, out var firstRow))
                        result.Errors.Add($"Telepon {phone} sudah dipakai pada baris {firstRow} di berkas ini.");
                    else
                        phonesInFile[phone] = row;
                }

                // Telepon lebih dapat diandalkan daripada email untuk mengenali pelanggan
                // toko, jadi ia dicoba lebih dulu.
                int? existingId = null;
                if (phone.Length > 0 && byPhone.TryGetValue(phone, out var idByPhone)) existingId = idByPhone;
                else if (email.Length > 0 && byEmail.TryGetValue(email, out var idByEmail)) existingId = idByEmail;

                if (existingId is not null)
                {
                    if (options.UpdateExisting)
                    {
                        result.Action = ImportAction.Update;
                        result.Warnings.Add("Pelanggan sudah ada dan akan diperbarui.");
                    }
                    else
                    {
                        result.Action = ImportAction.Skip;
                        result.Warnings.Add("Pelanggan sudah ada dan dilewati karena pembaruan dimatikan.");
                    }
                }

                result.Payload = new ParsedCustomer
                {
                    ExistingId = existingId,
                    Name = name,
                    Phone = phone.Length == 0 ? null : phone,
                    Email = email.Length == 0 ? null : email,
                    Points = points
                };

                preview.Rows.Add(result);
            }

            if (preview.Rows.Count == 0 && preview.FileErrors.Count == 0)
                preview.FileErrors.Add("Tidak ada baris data yang dapat dibaca. Isi lembar Data terlebih dahulu.");

            return preview;
        }

        public async Task<ImportResult> CommitAsync(ImportPreview preview, ImportOptions options, CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var created = 0;
                var updated = 0;

                foreach (var row in preview.Rows.Where(r => r.IsValid && r.Action != ImportAction.Skip))
                {
                    var parsed = (ParsedCustomer)row.Payload!;

                    if (parsed.ExistingId is int id)
                    {
                        var entity = await db.Customers.FindAsync([id], ct);
                        if (entity is null) continue;

                        entity.Name = parsed.Name;
                        if (parsed.Phone is not null) entity.Phone = parsed.Phone;
                        if (parsed.Email is not null) entity.Email = parsed.Email;
                        entity.LoyaltyPoints = parsed.Points;

                        updated++;
                    }
                    else
                    {
                        db.Customers.Add(new Customer
                        {
                            Name = parsed.Name,
                            Phone = parsed.Phone,
                            Email = parsed.Email,
                            LoyaltyPoints = parsed.Points
                        });

                        created++;
                    }
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ImportResult(created, updated, preview.SkipCount);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return new ImportResult(0, 0, 0, $"Impor dibatalkan: {ex.Message}");
            }
        }
    }
}
