# 🗄️ Setup Database - Lapak

## Provider yang Didukung

| Provider | Connection String Key | Keterangan |
|----------|----------------------|------------|
| SQLite | DefaultConnection | Default, File-based |
| SQL Server | SqlServer | Microsoft SQL Server |
| MySQL | MySql | MySQL/MariaDB |
| PostgreSQL | PostgreSql | PostgreSQL |

## Konfigurasi

Edit `appsettings.json`:

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lapak.db",
    "SqlServer": "Server=localhost;Database=LapakDb;Trusted_Connection=true;",
    "MySql": "Server=localhost;Database=LapakDb;User=root;Password=;",
    "PostgreSql": "Host=localhost;Database=LapakDb;Username=postgres;Password=;"
  }
}
```

## SQLite (Default)

Tidak memerlukan setup tambahan. Database akan dibuat otomatis sebagai file `lapak.db`.

## SQL Server

### Docker Setup
```bash
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourPassword123!' \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

### Connection String
```
Server=localhost,1433;Database=LapakDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true
```

## MySQL

### Docker Setup
```bash
docker run -e MYSQL_ROOT_PASSWORD=password -e MYSQL_DATABASE=LapakDb \
  -p 3306:3306 -d mysql:8.0
```

### Connection String
```
Server=localhost;Database=LapakDb;User=root;Password=password;
```

## PostgreSQL

### Docker Setup
```bash
docker run -e POSTGRES_PASSWORD=password -e POSTGRES_DB=LapakDb \
  -p 5432:5432 -d postgres:16
```

### Connection String
```
Host=localhost;Database=LapakDb;Username=postgres;Password=password;
```

## Migrasi Database

Aplikasi menggunakan `EnsureCreated()` untuk development. Untuk production:

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Create migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update
```

## Seed Data

Aplikasi otomatis men-seed data awal saat database kosong:
- 4 Kategori utama
- 7 Sub-kategori
- 6 Produk sample
- 1 Toko demo
- 2 Voucher
