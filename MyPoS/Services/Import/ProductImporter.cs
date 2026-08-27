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
    public class ProductImporter : IMasterDataImporter
    {
        private const string ColName = "Nama Produk";
        private const string ColBarcode = "Barcode";
        private const string ColCategory = "Kategori";
        private const string ColPrice = "Harga Jual";
        private const string ColCost = "Harga Modal";
        private const string ColStock = "Stok";
        private const string ColMinStock = "Stok Minimum";
        private const string ColDescription = "Keterangan";
        private const string ColActive = "Aktif";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ProductImporter(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public string Key => "produk";
        public string DisplayName => "Produk";
        public bool SupportsCategoryCreation => true;

        public IReadOnlyList<ImportColumn> Columns { get; } = new[]
        {
            new ImportColumn(ColName, true, "Nama produk seperti yang tampil di kasir.", "Kopi Bubuk 200g"),
            new ImportColumn(ColBarcode, false, "Kode yang dipindai kasir. Harus unik bila diisi; dipakai untuk mencocokkan produk yang sudah ada.", "8992761111038"),
            new ImportColumn(ColCategory, true, "Nama kategori. Harus sudah ada, kecuali pilihan pembuatan kategori baru diaktifkan saat impor.", "Minuman"),
            new ImportColumn(ColPrice, true, "Harga jual per satuan, tanpa simbol mata uang.", "24000"),
            new ImportColumn(ColCost, false, "Harga modal, dipakai laporan margin. Kosong dianggap nol.", "18000"),
            new ImportColumn(ColStock, false, "Jumlah stok awal. Kosong dianggap nol.", "35"),
            new ImportColumn(ColMinStock, false, "Ambang stok menipis khusus produk ini. Kosong atau 0 berarti memakai ambang bawaan dari Pengaturan.", "10"),
            new ImportColumn(ColDescription, false, "Keterangan bebas.", "Kopi robusta giling"),
            new ImportColumn(ColActive, false, "Ya atau Tidak. Kosong dianggap Ya.", "Ya")
        };

        public async Task<byte[]> BuildTemplateAsync(CancellationToken ct = default)
        {
            using var db = await _dbContextFactory.CreateDbContextAsync(ct);
            var categories = await db.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(ct);

            var samples = new List<IReadOnlyList<string>>
            {
                new[] { $"{ExcelImportHelper.SampleMarker} Kopi Bubuk 200g", "8990000000017", categories.FirstOrDefault() ?? "Minuman", "24000", "18000", "35", "10", "Kopi robusta giling", "Ya" }
            };

            return ExcelImportHelper.BuildTemplate(
                DisplayName,
                Columns,
                samples,
                new[] { new ImportReferenceList(ColCategory, categories) });
        }

        /// <summary>Data yang sudah lolos parse untuk satu baris, siap disimpan.</summary>
        private sealed class ParsedProduct
        {
            public int? ExistingId { get; init; }
            public string Name { get; init; } = "";
            public string? Barcode { get; init; }
            public string CategoryName { get; init; } = "";
            public int? CategoryId { get; init; }
            public decimal Price { get; init; }
            public decimal Cost { get; init; }
            public int Stock { get; init; }
            public int MinStock { get; init; }
            public string? Description { get; init; }
            public bool IsActive { get; init; }
        }

        public async Task<ImportPreview> ParseAsync(Stream file, ImportOptions options, CancellationToken ct = default)
        {
            var preview = new ImportPreview();

            using var handle = ExcelImportHelper.OpenDataSheet(file, Columns, preview);
            if (handle is null) return preview;

            var sheet = handle.Sheet;
            var map = handle.HeaderMap;

            using var db = await _dbContextFactory.CreateDbContextAsync(ct);

            var categories = await db.Categories.AsNoTracking().ToListAsync(ct);
            var categoryByName = categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

            var existing = await db.Products.AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.Barcode })
                .ToListAsync(ct);

            var byBarcode = existing
                .Where(p => !string.IsNullOrWhiteSpace(p.Barcode))
                .GroupBy(p => p.Barcode!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var byName = existing
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            // Bentrok di dalam berkas itu sendiri harus tertangkap juga, bukan hanya bentrok
            // dengan data yang sudah tersimpan.
            var barcodesInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var namesInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var newCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var lastRow = ExcelImportHelper.LastDataRow(sheet);

            for (var row = 2; row <= lastRow; row++)
            {
                if (ExcelImportHelper.IsRowEmpty(sheet, row, map)) continue;
                if (ExcelImportHelper.IsSampleRow(sheet, row, map, ColName)) continue;

                var result = new ImportRowResult { RowNumber = row };

                var name = ExcelImportHelper.ReadText(sheet, row, map, ColName);
                var barcode = ExcelImportHelper.ReadText(sheet, row, map, ColBarcode);
                var categoryName = ExcelImportHelper.ReadText(sheet, row, map, ColCategory);

                result.Summary = name.Length > 0 ? name : "(nama kosong)";

                if (name.Length == 0) result.Errors.Add($"{ColName} wajib diisi.");
                if (name.Length > 200) result.Errors.Add($"{ColName} maksimal 200 karakter.");
                if (barcode.Length > 64) result.Errors.Add($"{ColBarcode} maksimal 64 karakter.");

                // ---------- Kategori ----------
                int? categoryId = null;
                if (categoryName.Length == 0)
                {
                    result.Errors.Add($"{ColCategory} wajib diisi.");
                }
                else if (categoryByName.TryGetValue(categoryName, out var foundId))
                {
                    categoryId = foundId;
                }
                else if (options.CreateMissingCategories)
                {
                    newCategories.Add(categoryName);
                    result.Warnings.Add($"Kategori \"{categoryName}\" belum ada dan akan dibuat.");
                }
                else
                {
                    result.Errors.Add($"Kategori \"{categoryName}\" tidak ditemukan. Aktifkan pilihan pembuatan kategori baru, atau perbaiki namanya.");
                }

                // ---------- Angka ----------
                var hasPrice = ExcelImportHelper.TryReadDecimal(sheet, row, map, ColPrice, out var price);
                if (!hasPrice) result.Errors.Add($"{ColPrice} wajib diisi dengan angka.");
                else if (price < 0) result.Errors.Add($"{ColPrice} tidak boleh negatif.");

                ExcelImportHelper.TryReadDecimal(sheet, row, map, ColCost, out var cost);
                if (cost < 0) result.Errors.Add($"{ColCost} tidak boleh negatif.");

                ExcelImportHelper.TryReadInt(sheet, row, map, ColStock, out var stock);
                if (stock < 0) result.Errors.Add($"{ColStock} tidak boleh negatif.");

                ExcelImportHelper.TryReadInt(sheet, row, map, ColMinStock, out var minStock);
                if (minStock < 0) result.Errors.Add($"{ColMinStock} tidak boleh negatif.");

                if (hasPrice && cost > price && price > 0)
                    result.Warnings.Add("Harga modal lebih besar daripada harga jual — marginnya akan negatif.");

                var active = ExcelImportHelper.ReadBoolean(sheet, row, map, ColActive) ?? true;

                // ---------- Duplikat di dalam berkas ----------
                if (barcode.Length > 0)
                {
                    if (barcodesInFile.TryGetValue(barcode, out var firstRow))
                        result.Errors.Add($"Barcode {barcode} sudah dipakai pada baris {firstRow} di berkas ini.");
                    else
                        barcodesInFile[barcode] = row;
                }
                else if (name.Length > 0)
                {
                    // Tanpa barcode, nama menjadi kunci pencocokan, jadi nama ganda pun bentrok.
                    if (namesInFile.TryGetValue(name, out var firstRow))
                        result.Errors.Add($"Nama produk sudah dipakai pada baris {firstRow} di berkas ini.");
                    else
                        namesInFile[name] = row;
                }

                // ---------- Cocokkan dengan data yang sudah ada ----------
                int? existingId = null;
                if (barcode.Length > 0 && byBarcode.TryGetValue(barcode, out var idByBarcode))
                    existingId = idByBarcode;
                else if (barcode.Length == 0 && name.Length > 0 && byName.TryGetValue(name, out var idByName))
                    existingId = idByName;

                if (existingId is not null)
                {
                    if (options.UpdateExisting)
                    {
                        result.Action = ImportAction.Update;
                        result.Warnings.Add("Produk sudah ada dan akan diperbarui.");
                    }
                    else
                    {
                        result.Action = ImportAction.Skip;
                        result.Warnings.Add("Produk sudah ada dan dilewati karena pembaruan dimatikan.");
                    }
                }

                result.Payload = new ParsedProduct
                {
                    ExistingId = existingId,
                    Name = name,
                    Barcode = barcode.Length == 0 ? null : barcode,
                    CategoryName = categoryName,
                    CategoryId = categoryId,
                    Price = price,
                    Cost = cost,
                    Stock = stock,
                    MinStock = minStock,
                    Description = ExcelImportHelper.ReadText(sheet, row, map, ColDescription) is { Length: > 0 } d ? d : null,
                    IsActive = active
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
                var rows = preview.Rows
                    .Where(r => r.IsValid && r.Action != ImportAction.Skip)
                    .Select(r => (Row: r, Product: (ParsedProduct)r.Payload!))
                    .ToList();

                // Kategori baru dibuat sekali di awal supaya baris-baris yang memakai kategori
                // yang sama tidak membuatnya berulang kali.
                var categoryByName = await db.Categories
                    .ToDictionaryAsync(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase, ct);

                if (options.CreateMissingCategories)
                {
                    var missing = rows
                        .Where(x => x.Product.CategoryId is null && x.Product.CategoryName.Length > 0)
                        .Select(x => x.Product.CategoryName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Where(name => !categoryByName.ContainsKey(name))
                        .ToList();

                    foreach (var name in missing)
                    {
                        var category = new Category { Name = name };
                        db.Categories.Add(category);
                        categoryByName[name] = category;
                    }

                    if (missing.Count > 0) await db.SaveChangesAsync(ct);
                }

                var created = 0;
                var updated = 0;

                foreach (var (_, parsed) in rows)
                {
                    var categoryId = parsed.CategoryId
                        ?? (categoryByName.TryGetValue(parsed.CategoryName, out var category) ? category.Id : 0);

                    if (categoryId == 0) continue;

                    if (parsed.ExistingId is int id)
                    {
                        var entity = await db.Products.FindAsync([id], ct);
                        if (entity is null) continue;

                        entity.Name = parsed.Name;
                        entity.Barcode = parsed.Barcode;
                        entity.CategoryId = categoryId;
                        entity.Price = parsed.Price;
                        entity.Cost = parsed.Cost;
                        entity.Stock = parsed.Stock;
                        entity.MinStock = parsed.MinStock;
                        entity.IsActive = parsed.IsActive;
                        if (parsed.Description is not null) entity.Description = parsed.Description;

                        updated++;
                    }
                    else
                    {
                        db.Products.Add(new Product
                        {
                            Name = parsed.Name,
                            Barcode = parsed.Barcode,
                            CategoryId = categoryId,
                            Price = parsed.Price,
                            Cost = parsed.Cost,
                            Stock = parsed.Stock,
                            MinStock = parsed.MinStock,
                            Description = parsed.Description,
                            IsActive = parsed.IsActive
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
                // Seluruh berkas dibatalkan bila ada yang gagal di tengah jalan; impor
                // separuh jadi jauh lebih sulit dibereskan daripada impor yang gagal utuh.
                await transaction.RollbackAsync(ct);
                return new ImportResult(0, 0, 0, $"Impor dibatalkan: {ex.Message}");
            }
        }
    }
}
