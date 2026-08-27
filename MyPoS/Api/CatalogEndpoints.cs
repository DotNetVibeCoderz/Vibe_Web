using Microsoft.EntityFrameworkCore;
using MyPoS.Data;
using MyPoS.Models;
using MyPoS.Services;

namespace MyPoS.Api
{
    /// <summary>Produk, kategori, dan pelanggan.</summary>
    public static class CatalogEndpoints
    {
        public static RouteGroupBuilder MapCatalogEndpoints(this RouteGroupBuilder group)
        {
            MapProducts(group.MapGroup("/products").WithTags("Produk"));
            MapCategories(group.MapGroup("/categories").WithTags("Kategori"));
            MapCustomers(group.MapGroup("/customers").WithTags("Pelanggan"));
            return group;
        }

        // ------------------------------------------------------------- Produk

        private static void MapProducts(RouteGroupBuilder products)
        {
            products.MapGet("/", async (
                IDbContextFactory<AppDbContext> factory,
                SettingsService settingsService,
                string? search,
                int? categoryId,
                bool? activeOnly,
                bool? lowStockOnly,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var (p, size) = ApiRoutes.ReadPaging(page, pageSize);
                using var db = await factory.CreateDbContextAsync(ct);

                var query = db.Products.Include(x => x.Category).AsNoTracking().AsQueryable();

                if (activeOnly == true) query = query.Where(x => x.IsActive);
                if (categoryId is int cid) query = query.Where(x => x.CategoryId == cid);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(x =>
                        EF.Functions.Like(x.Name, $"%{search}%") ||
                        (x.Barcode != null && EF.Functions.Like(x.Barcode, $"%{search}%")));
                }

                // Ambang stok per produk boleh nol, yang berarti "pakai bawaan". Karena nilai
                // bawaannya ada di pengaturan dan bukan di kolom, penyaringan stok menipis
                // dilakukan setelah data ditarik.
                if (lowStockOnly == true)
                {
                    var rows = await query.OrderBy(x => x.Name).ToListAsync(ct);
                    var settings = await settingsService.GetAsync();
                    var filtered = rows
                        .Where(x => x.Stock <= (x.MinStock > 0 ? x.MinStock : settings.LowStockThreshold))
                        .ToList();

                    var pageItems = filtered.Skip((p - 1) * size).Take(size).Select(Map).ToList();
                    return Results.Ok(ApiRoutes.ToPaged(pageItems, filtered.Count, p, size));
                }

                var total = await query.CountAsync(ct);
                var items = await query
                    .OrderBy(x => x.Name)
                    .Skip((p - 1) * size)
                    .Take(size)
                    .ToListAsync(ct);

                return Results.Ok(ApiRoutes.ToPaged(items.Select(Map).ToList(), total, p, size));
            })
            .WithSummary("Daftar produk")
            .WithDescription("Mendukung pencarian nama/barcode, penyaringan kategori, hanya produk aktif, dan hanya stok menipis.");

            products.MapGet("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var product = await db.Products.Include(x => x.Category).AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id, ct);

                return product is null
                    ? Results.NotFound(new ApiError($"Produk {id} tidak ditemukan."))
                    : Results.Ok(Map(product));
            })
            .WithSummary("Ambil satu produk");

            products.MapGet("/barcode/{barcode}", async (string barcode, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var product = await db.Products.Include(x => x.Category).AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Barcode == barcode, ct);

                return product is null
                    ? Results.NotFound(new ApiError($"Produk dengan barcode {barcode} tidak ditemukan."))
                    : Results.Ok(Map(product));
            })
            .WithSummary("Cari produk berdasarkan barcode")
            .WithDescription("Dipakai perangkat pemindai atau aplikasi kasir pendamping.");

            products.MapPost("/", async (ProductWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                var error = await ValidateProductAsync(body, null, factory, ct);
                if (error is not null) return Results.BadRequest(error);

                using var db = await factory.CreateDbContextAsync(ct);
                var product = new Product();
                Apply(body, product);
                db.Products.Add(product);
                await db.SaveChangesAsync(ct);

                await db.Entry(product).Reference(x => x.Category).LoadAsync(ct);
                return Results.Created($"/api/v1/products/{product.Id}", Map(product));
            })
            .WithSummary("Buat produk baru");

            products.MapPut("/{id:int}", async (int id, ProductWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                var error = await ValidateProductAsync(body, id, factory, ct);
                if (error is not null) return Results.BadRequest(error);

                using var db = await factory.CreateDbContextAsync(ct);
                var product = await db.Products.FindAsync([id], ct);
                if (product is null) return Results.NotFound(new ApiError($"Produk {id} tidak ditemukan."));

                Apply(body, product);
                await db.SaveChangesAsync(ct);

                await db.Entry(product).Reference(x => x.Category).LoadAsync(ct);
                return Results.Ok(Map(product));
            })
            .WithSummary("Perbarui produk");

            products.MapPost("/{id:int}/stock", async (
                int id,
                StockAdjustmentDto body,
                IDbContextFactory<AppDbContext> factory,
                CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var product = await db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id, ct);
                if (product is null) return Results.NotFound(new ApiError($"Produk {id} tidak ditemukan."));

                if (product.Stock + body.Delta < 0)
                    return Results.BadRequest(new ApiError($"Stok tidak boleh negatif. Stok kini {product.Stock}, penyesuaian {body.Delta}."));

                product.Stock += body.Delta;
                await db.SaveChangesAsync(ct);
                return Results.Ok(Map(product));
            })
            .WithSummary("Sesuaikan stok")
            .WithDescription("Delta positif menambah stok (penerimaan barang), delta negatif mengurangi (penyusutan).");

            products.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var product = await db.Products.FindAsync([id], ct);
                if (product is null) return Results.NotFound(new ApiError($"Produk {id} tidak ditemukan."));

                // Produk yang pernah terjual hanya dinonaktifkan; menghapusnya akan merusak
                // riwayat transaksi yang mengacu kepadanya.
                if (await db.TransactionDetails.AnyAsync(d => d.ProductId == id, ct))
                {
                    product.IsActive = false;
                    await db.SaveChangesAsync(ct);
                    return Results.Ok(new { deactivated = true, message = "Produk pernah terjual, jadi dinonaktifkan agar riwayat tetap utuh." });
                }

                db.Products.Remove(product);
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .WithSummary("Hapus produk");
        }

        // ----------------------------------------------------------- Kategori

        private static void MapCategories(RouteGroupBuilder categories)
        {
            categories.MapGet("/", async (IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var rows = await db.Categories
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, Count = c.Products.Count })
                    .AsNoTracking()
                    .ToListAsync(ct);

                return Results.Ok(rows.Select(c => new CategoryDto(c.Id, c.Name, c.Count)).ToList());
            })
            .WithSummary("Daftar kategori");

            categories.MapPost("/", async (CategoryWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(body.Name))
                    return Results.BadRequest(new ApiError("Nama kategori wajib diisi."));

                using var db = await factory.CreateDbContextAsync(ct);
                var name = body.Name.Trim();

                if (await db.Categories.AnyAsync(c => c.Name == name, ct))
                    return Results.Conflict(new ApiError($"Kategori \"{name}\" sudah ada."));

                var category = new Category { Name = name };
                db.Categories.Add(category);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/v1/categories/{category.Id}", new CategoryDto(category.Id, category.Name, 0));
            })
            .WithSummary("Buat kategori");

            categories.MapPut("/{id:int}", async (int id, CategoryWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(body.Name))
                    return Results.BadRequest(new ApiError("Nama kategori wajib diisi."));

                using var db = await factory.CreateDbContextAsync(ct);
                var category = await db.Categories.FindAsync([id], ct);
                if (category is null) return Results.NotFound(new ApiError($"Kategori {id} tidak ditemukan."));

                category.Name = body.Name.Trim();
                await db.SaveChangesAsync(ct);

                var count = await db.Products.CountAsync(p => p.CategoryId == id, ct);
                return Results.Ok(new CategoryDto(category.Id, category.Name, count));
            })
            .WithSummary("Perbarui kategori");

            categories.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var category = await db.Categories.FindAsync([id], ct);
                if (category is null) return Results.NotFound(new ApiError($"Kategori {id} tidak ditemukan."));

                var used = await db.Products.CountAsync(p => p.CategoryId == id, ct);
                if (used > 0)
                    return Results.Conflict(new ApiError($"Kategori masih dipakai {used} produk."));

                db.Categories.Remove(category);
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .WithSummary("Hapus kategori");
        }

        // ---------------------------------------------------------- Pelanggan

        private static void MapCustomers(RouteGroupBuilder customers)
        {
            customers.MapGet("/", async (
                IDbContextFactory<AppDbContext> factory,
                string? search,
                int? page,
                int? pageSize,
                CancellationToken ct) =>
            {
                var (p, size) = ApiRoutes.ReadPaging(page, pageSize);
                using var db = await factory.CreateDbContextAsync(ct);

                var query = db.Customers.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.Name, $"%{search}%") ||
                        (c.Phone != null && EF.Functions.Like(c.Phone, $"%{search}%")) ||
                        (c.Email != null && EF.Functions.Like(c.Email, $"%{search}%")));
                }

                var total = await query.CountAsync(ct);
                var items = await query.OrderBy(c => c.Name).Skip((p - 1) * size).Take(size).ToListAsync(ct);

                return Results.Ok(ApiRoutes.ToPaged(
                    items.Select(c => new CustomerDto(c.Id, c.Name, c.Phone, c.Email, c.LoyaltyPoints)).ToList(),
                    total, p, size));
            })
            .WithSummary("Daftar pelanggan");

            customers.MapGet("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

                return customer is null
                    ? Results.NotFound(new ApiError($"Pelanggan {id} tidak ditemukan."))
                    : Results.Ok(new CustomerDto(customer.Id, customer.Name, customer.Phone, customer.Email, customer.LoyaltyPoints));
            })
            .WithSummary("Ambil satu pelanggan");

            customers.MapPost("/", async (CustomerWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(body.Name))
                    return Results.BadRequest(new ApiError("Nama pelanggan wajib diisi."));

                using var db = await factory.CreateDbContextAsync(ct);
                var customer = new Customer
                {
                    Name = body.Name.Trim(),
                    Phone = body.Phone?.Trim(),
                    Email = body.Email?.Trim(),
                    LoyaltyPoints = body.LoyaltyPoints
                };

                db.Customers.Add(customer);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/v1/customers/{customer.Id}",
                    new CustomerDto(customer.Id, customer.Name, customer.Phone, customer.Email, customer.LoyaltyPoints));
            })
            .WithSummary("Buat pelanggan");

            customers.MapPut("/{id:int}", async (int id, CustomerWriteDto body, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(body.Name))
                    return Results.BadRequest(new ApiError("Nama pelanggan wajib diisi."));

                using var db = await factory.CreateDbContextAsync(ct);
                var customer = await db.Customers.FindAsync([id], ct);
                if (customer is null) return Results.NotFound(new ApiError($"Pelanggan {id} tidak ditemukan."));

                customer.Name = body.Name.Trim();
                customer.Phone = body.Phone?.Trim();
                customer.Email = body.Email?.Trim();
                customer.LoyaltyPoints = body.LoyaltyPoints;
                await db.SaveChangesAsync(ct);

                return Results.Ok(new CustomerDto(customer.Id, customer.Name, customer.Phone, customer.Email, customer.LoyaltyPoints));
            })
            .WithSummary("Perbarui pelanggan");

            customers.MapDelete("/{id:int}", async (int id, IDbContextFactory<AppDbContext> factory, CancellationToken ct) =>
            {
                using var db = await factory.CreateDbContextAsync(ct);
                var customer = await db.Customers.FindAsync([id], ct);
                if (customer is null) return Results.NotFound(new ApiError($"Pelanggan {id} tidak ditemukan."));

                // Relasi transaksi memakai DeleteBehavior.SetNull, jadi riwayatnya tetap ada.
                db.Customers.Remove(customer);
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .WithSummary("Hapus pelanggan");
        }

        // ------------------------------------------------------------ Bantuan

        private static ProductDto Map(Product p) => new(
            p.Id, p.Name, p.Barcode, p.CategoryId, p.Category?.Name,
            p.Price, p.Cost, p.Stock, p.MinStock, p.IsActive, p.Description, p.ImageUrl);

        private static void Apply(ProductWriteDto body, Product product)
        {
            product.Name = body.Name.Trim();
            product.Barcode = string.IsNullOrWhiteSpace(body.Barcode) ? null : body.Barcode.Trim();
            product.CategoryId = body.CategoryId;
            product.Price = body.Price;
            product.Cost = body.Cost;
            product.Stock = body.Stock;
            product.MinStock = body.MinStock;
            product.IsActive = body.IsActive;
            product.Description = body.Description;
            product.ImageUrl = body.ImageUrl;
        }

        private static async Task<ApiError?> ValidateProductAsync(
            ProductWriteDto body, int? existingId, IDbContextFactory<AppDbContext> factory, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(body.Name)) return new ApiError("Nama produk wajib diisi.");
            if (body.Price < 0 || body.Cost < 0) return new ApiError("Harga tidak boleh negatif.");
            if (body.Stock < 0) return new ApiError("Stok tidak boleh negatif.");

            using var db = await factory.CreateDbContextAsync(ct);

            if (!await db.Categories.AnyAsync(c => c.Id == body.CategoryId, ct))
                return new ApiError($"Kategori {body.CategoryId} tidak ditemukan.");

            if (!string.IsNullOrWhiteSpace(body.Barcode))
            {
                var barcode = body.Barcode.Trim();
                var clash = await db.Products.AnyAsync(p => p.Barcode == barcode && p.Id != (existingId ?? 0), ct);
                if (clash) return new ApiError($"Barcode {barcode} sudah dipakai produk lain.");
            }

            return null;
        }
    }
}
