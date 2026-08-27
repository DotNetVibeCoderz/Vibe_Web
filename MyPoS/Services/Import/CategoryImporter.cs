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
    public class CategoryImporter : IMasterDataImporter
    {
        private const string ColName = "Nama Kategori";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public CategoryImporter(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public string Key => "kategori";
        public string DisplayName => "Kategori";

        public IReadOnlyList<ImportColumn> Columns { get; } = new[]
        {
            new ImportColumn(ColName, true, "Nama kategori. Harus unik; kategori dengan nama yang sama akan dilewati.", "Minuman")
        };

        public Task<byte[]> BuildTemplateAsync(CancellationToken ct = default)
        {
            var samples = new List<IReadOnlyList<string>>
            {
                new[] { $"{ExcelImportHelper.SampleMarker} Minuman" }
            };

            return Task.FromResult(ExcelImportHelper.BuildTemplate(DisplayName, Columns, samples));
        }

        public async Task<ImportPreview> ParseAsync(Stream file, ImportOptions options, CancellationToken ct = default)
        {
            var preview = new ImportPreview();

            using var handle = ExcelImportHelper.OpenDataSheet(file, Columns, preview);
            if (handle is null) return preview;

            var sheet = handle.Sheet;
            var map = handle.HeaderMap;

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var existing = await db.Categories.AsNoTracking()
                .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastRow = ExcelImportHelper.LastDataRow(sheet);

            for (var row = 2; row <= lastRow; row++)
            {
                if (ExcelImportHelper.IsRowEmpty(sheet, row, map)) continue;
                if (ExcelImportHelper.IsSampleRow(sheet, row, map, ColName)) continue;

                var result = new ImportRowResult { RowNumber = row };
                var name = ExcelImportHelper.ReadText(sheet, row, map, ColName);

                result.Summary = name.Length > 0 ? name : "(nama kosong)";

                if (name.Length == 0) result.Errors.Add($"{ColName} wajib diisi.");
                if (name.Length > 120) result.Errors.Add($"{ColName} maksimal 120 karakter.");

                if (name.Length > 0)
                {
                    if (seen.TryGetValue(name, out var firstRow))
                        result.Errors.Add($"Nama kategori sudah dipakai pada baris {firstRow} di berkas ini.");
                    else
                        seen[name] = row;
                }

                // Kategori hanya punya satu kolom, jadi "memperbarui" tidak berarti apa-apa:
                // nama yang sudah ada cukup dilewati.
                if (name.Length > 0 && existing.ContainsKey(name))
                {
                    result.Action = ImportAction.Skip;
                    result.Warnings.Add("Kategori sudah ada dan dilewati.");
                }

                result.Payload = name;
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

                foreach (var row in preview.Rows.Where(r => r.IsValid && r.Action == ImportAction.Create))
                {
                    db.Categories.Add(new Category { Name = (string)row.Payload! });
                    created++;
                }

                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new ImportResult(created, 0, preview.SkipCount);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                return new ImportResult(0, 0, 0, $"Impor dibatalkan: {ex.Message}");
            }
        }
    }
}
