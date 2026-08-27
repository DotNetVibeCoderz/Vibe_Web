# Basis data

MyPoS berjalan di atas empat penyedia: **SQLite**, **SQL Server**, **PostgreSQL**, dan **MySQL/MariaDB**. Model entitas dan seluruh kueri sama untuk keempatnya — yang berbeda hanya satu baris konfigurasi.

## Memilih penyedia

`appsettings.json`:

```json
"Database": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=mypos.db"
}
```

`Provider` menerima nilai berikut (tidak peka huruf besar-kecil):

| Nilai | Penyedia | Paket |
|---|---|---|
| `Sqlite` *(bawaan)* | SQLite | `Microsoft.EntityFrameworkCore.Sqlite` |
| `SqlServer`, `MsSql` | SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` |
| `PostgreSql`, `Postgres`, `Npgsql` | PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `MySql`, `MariaDb` | MySQL / MariaDB | `MySql.EntityFrameworkCore` |

`ConnectionString` boleh dikosongkan; bila kosong, dipakai `ConnectionStrings:Default`, lalu nilai bawaan penyedia yang bersangkutan.

## Contoh connection string

**SQLite** — bawaan, tidak perlu server apa pun:

```json
"Database": { "Provider": "Sqlite", "ConnectionString": "Data Source=mypos.db" }
```

**SQL Server**:

```json
"Database": {
  "Provider": "SqlServer",
  "ConnectionString": "Server=localhost;Database=MyPoS;Trusted_Connection=True;TrustServerCertificate=True"
}
```

**PostgreSQL**:

```json
"Database": {
  "Provider": "PostgreSql",
  "ConnectionString": "Host=localhost;Port=5432;Database=mypos;Username=postgres;Password=rahasia"
}
```

**MySQL / MariaDB**:

```json
"Database": {
  "Provider": "MySql",
  "ConnectionString": "Server=localhost;Port=3306;Database=mypos;User=root;Password=rahasia"
}
```

Untuk produksi, simpan connection string di variabel lingkungan alih-alih di berkas:

```
ConnectionStrings__Default=Host=db;Database=mypos;Username=mypos;Password=...
Database__Provider=PostgreSql
```

## Perbedaan yang sudah ditangani

Beberapa hal berbeda antar penyedia dan sudah diselesaikan di dalam kode, jadi tidak perlu diurus lagi saat berpindah:

**Panjang kolom terindeks.** MySQL tidak dapat membuat indeks pada kolom teks tanpa batas panjang. Semua kolom yang diberi indeks — `AppSetting.Key`, `AppUser.Username`, `Transaction.InvoiceNumber`, `Product.Barcode`, `ApiKey.Prefix` — punya `MaxLength` eksplisit.

**Presisi decimal.** SQLite menyimpan `decimal` sebagai teks, sedangkan penyedia lain memerlukan presisi yang dinyatakan. Seluruh properti decimal dipatok ke `decimal(18,4)` di `OnModelCreating`; tanpa itu SQL Server diam-diam memakai `decimal(18,2)` dan membulatkan nilai.

**Waktu di PostgreSQL.** Npgsql memetakan `DateTime` ke `timestamp with time zone` dan menolak nilai ber-`Kind` Local. Transaksi dicatat memakai waktu lokal toko, jadi `DatabaseSetup` mengaktifkan `Npgsql.EnableLegacyTimestampBehavior` yang mengembalikan pemetaan ke `timestamp without time zone`.

**Percobaan ulang koneksi.** SQL Server dan PostgreSQL diaktifkan `EnableRetryOnFailure`, karena basis data server dapat terputus sesaat — hal yang tidak pernah terjadi pada berkas SQLite lokal.

## Pembuatan skema

Aplikasi memakai `EnsureCreated`, bukan migrasi EF. Perilakunya berbeda menurut penyedia, dan ini disengaja:

**SQLite** dianggap basis data pengembangan. Bila skema tidak lagi cocok dengan model entitas, berkasnya disalin ke `mypos.db.bak-yyyyMMdd-HHmmss`, dicatat sebagai peringatan di log, lalu dibuat ulang beserta data contoh.

**SQL Server, PostgreSQL, MySQL tidak pernah dihapus otomatis.** Menghapus basis data server tidak dapat dibatalkan dan mungkin dipakai bersama aplikasi lain. Yang terjadi hanyalah pesan kesalahan di log:

```
Skema basis data PostgreSql tidak cocok dengan model entitas. Basis data server
tidak dihapus otomatis - terapkan perubahan skema secara manual atau lewat migrasi EF.
```

Bila basis data server belum dapat dihubungi saat startup, aplikasi tetap menyala dan mencatat pesan yang jelas, bukan meledak saat halaman pertama dibuka.

## Beralih ke migrasi EF

Untuk pemakaian sungguhan dengan basis data server, sebaiknya beralih ke migrasi:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Awal
dotnet ef database update
```

Setelah itu ganti `EnsureCreatedAsync` di `Data/DatabaseBootstrapper.cs` dengan `MigrateAsync`, dan hapus cabang penghapusan SQLite. Data contoh pada `HasData` akan ikut masuk sebagai bagian dari migrasi pertama.
