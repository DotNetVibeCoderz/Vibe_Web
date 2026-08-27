# Arsitektur

Satu proyek .NET 10 (`MyPoS.csproj`), Blazor Server, tanpa proyek pendamping.

## Peta berkas

```
Pages/          Halaman berbasis rute
Shared/         Tata letak dan komponen yang dipakai bersama
Services/       Logika aplikasi
  Payments/     Antarmuka dan implementasi penyedia pembayaran
Api/            REST API untuk aplikasi luar (Minimal API + Swagger)
Services/
  Import/       Template Excel dan impor data master
Data/           DbContext, pemilihan penyedia, dan penyiapan basis data
Models/         Entitas EF Core
wwwroot/css/    mypos.css — seluruh sistem visual
docs/           Dokumentasi dan tangkapan layar
```

## Alur data

```
Halaman (.razor)  ─┐
REST API (Api/)   ─┤
                   ├─ IDbContextFactory<AppDbContext>  → konteks berumur pendek per operasi
                   ├─ SettingsService (singleton)      → PosSettings, di-cache di memori
                   ├─ MoneyFormatter                   → memformat nominal sesuai pengaturan
                   ├─ PdfReportService                 → laporan PDF siap cetak
                   └─ CheckoutService                  → satu-satunya jalur pembuatan transaksi
                        ├─ TaxCalculator               → diskon, layanan, pajak, pembulatan
                        └─ PaymentGatewayResolver      → IPaymentGateway yang sesuai
```

Halaman kasir dan REST API memakai `CheckoutService` yang sama, jadi transaksi yang
dibuat aplikasi luar melewati validasi stok, perhitungan pajak, dan pemberian poin
yang persis sama dengan transaksi yang diketik kasir.

### Penyedia basis data

`DatabaseSetup.AddMyPosDatabase` memilih penyedia sekali saat startup dari
`appsettings.json` → `Database:Provider`: SQLite, SQL Server, PostgreSQL, atau MySQL.
Model entitas dan seluruh kueri sama untuk keempatnya. Perbedaan yang sudah ditangani
— panjang kolom terindeks untuk MySQL, presisi decimal, dan pemetaan waktu Npgsql —
dijelaskan di [basis-data.md](basis-data.md).

### Akses basis data

`AppDbContext` didaftarkan lewat `AddDbContextFactory`, bukan sebagai layanan scoped. Setiap operasi membuka konteksnya sendiri:

```csharp
using var db = await DbFactory.CreateDbContextAsync();
```

Pola ini diperlukan karena circuit Blazor Server berumur jauh lebih panjang daripada masa hidup wajar sebuah `DbContext`. Jangan menyuntikkan `AppDbContext` langsung ke komponen.

### Pengaturan

`SettingsService` adalah singleton yang membaca tabel `Settings` sekali lalu menyimpan hasilnya di memori. Pemetaan properti ke baris dilakukan dengan refleksi atas `PosSettings`, sehingga menambah pengaturan baru tidak menyentuh skema basis data maupun kode pemetaan.

Menyimpan pengaturan akan memicu event `Changed`, yang dipakai `MainLayout` untuk merender ulang tema tanpa perlu memuat ulang halaman.

## Keputusan rancangan

### Transaksi dibuat di satu tempat

Seluruh pembuatan transaksi melewati `CheckoutService.CheckoutAsync`, yang menjalankan validasi stok, perhitungan total, pemanggilan penyedia, pengurangan stok, dan pemberian poin dalam satu transaksi basis data. Sebelumnya logika ini tersebar di dalam `POS.razor` dan memiliki cacat: baris yang stoknya kurang dibuang diam-diam, sementara totalnya tetap ditagih penuh.

Stok dikurangi saat transaksi dibuat, bukan saat pembayaran dikonfirmasi, supaya barang yang sama tidak terjual dua kali selagi pelanggan masih berada di halaman pembayaran. Pembatalan dan kegagalan pembayaran mengembalikannya.

### Data yang diabadikan pada transaksi

`Transaction` menyimpan `TaxRate` dan `TaxInclusive`, dan `TransactionDetail` menyimpan `ProductName` serta `UnitCost`. Ketiganya adalah salinan nilai saat transaksi terjadi. Tanpa itu, mengubah tarif pajak atau harga modal akan mengubah struk dan laporan lama secara surut.

### Otentikasi

`CustomAuthStateProvider` menyimpan sesi di `ProtectedLocalStorage`, bukan hanya di field circuit. Versi sebelumnya menyimpannya di memori saja, sehingga kasir terlempar ke halaman login setiap kali me-refresh halaman — masalah nyata di konter yang jaringannya tidak stabil.

Karena penyimpanan browser hanya dapat diakses setelah circuit hidup, `_Host.cshtml` memakai `render-mode="Server"` tanpa prerender. Dengan prerender, halaman akan selalu tampil sebagai "belum masuk" lebih dulu lalu berkedip berganti.

Kata sandi di-hash dengan PBKDF2-SHA256, 100.000 iterasi, salt acak per pengguna. Format lama berbasis Base64 masih diterima saat masuk dan langsung ditulis ulang menjadi hash begitu login berhasil, sehingga basis data lama tidak perlu di-reset.

### Skema basis data

Proyek ini memakai `EnsureCreated`, bukan migrasi EF. Konsekuensinya, mengubah entitas membuat berkas basis data lama tidak lagi cocok dengan model.

`Data/DatabaseBootstrapper.cs` menangani hal ini: ia menyentuh setiap tabel saat startup, dan bila ada yang gagal, berkas lama **disalin lebih dulu** ke `mypos.db.bak-yyyyMMdd-HHmmss` sebelum basis data dibuat ulang, disertai peringatan di log. Versi sebelumnya hanya memeriksa tabel `Products` dan menghapus basis data tanpa memberi tahu siapa pun.

Bila data penjualan yang tersimpan berharga, gunakan migrasi EF yang sesungguhnya alih-alih mengandalkan mekanisme ini.

### Sistem visual

`wwwroot/css/mypos.css` memegang seluruh token warna, dan `Shared/AppTheme.cs` memetakan token yang sama ke palet MudBlazor. Keduanya harus tetap sejalan: komponen MudBlazor mengambil warna dari `MudThemeProvider`, sementara gaya khusus aplikasi mengambilnya dari variabel CSS pada kelas `body`.

Karena itu, tombol ganti tema menggeser keduanya sekaligus — `_isDarkMode` untuk MudBlazor dan `setBodyClass` untuk CSS. Menggeser salah satu saja akan menyisakan separuh halaman di tema sebelumnya.

Warna grafik dipisahkan dari warna antarmuka (`--chart-1`) dan dipilih agar lolos pemeriksaan rentang lightness, ambang chroma, dan kontras terhadap permukaan kartu pada mode terang maupun gelap.

### Penyimpanan berkas

`IStorageService` melayani empat penyedia: sistem berkas lokal, Azure Blob, AWS S3, dan
MinIO. Pilihannya ditentukan sekali saat startup dari `appsettings.json` → `Storage:Provider`.

S3 dan MinIO memakai satu kelas yang sama, `S3StorageService`, karena protokolnya memang
identik — yang membedakan hanya `ServiceUrl` dan gaya penulisan alamat. Menduplikasi kelas
hanya untuk mengganti satu URL akan membuat dua jalur kode yang harus diperbaiki bersamaan.
Rinciannya di [penyimpanan.md](penyimpanan.md).

### REST API

`Api/` berisi endpoint Minimal API untuk aplikasi luar, dikelompokkan di bawah `/api/v1`
dan didokumentasikan dengan Swagger di `/swagger`.

Filter kunci API dipasang pada **grupnya**, bukan pada tiap endpoint, sehingga endpoint
baru tidak mungkin terlupa dilindungi. Kunci disimpan sebagai hash PBKDF2 dengan prefix
terindeks untuk pencarian; izin baca-saja ditolak pada metode yang mengubah data.
Rinciannya di [api.md](api.md).

### Impor data master

`Services/Import/` berisi satu importer per jenis data master, semuanya di balik
`IMasterDataImporter`, dan satu dialog bersama `Shared/ImportDialog.razor`.

Pemeriksaan dan penulisan dipisah menjadi dua langkah: `ParseAsync` membaca berkas dan
mengembalikan pratinjau per baris tanpa menyentuh basis data, `CommitAsync` baru menulis
setelah pengguna menyetujuinya — dan membatalkan seluruhnya bila ada yang gagal di tengah.
Rinciannya di [impor.md](impor.md).

### Laporan PDF

`PdfReportService` menyusun laporan dengan QuestPDF memakai token warna yang sama dengan
antarmuka. Kolom, ringkasan, dan penyaring dikirim dari halaman pemanggil, sehingga isi
berkas selalu sama dengan yang terlihat di layar saat tombol ditekan.

## Peran dan hak akses

| Halaman | Admin | Manager | Operator |
|---|:-:|:-:|:-:|
| Dasbor | ✓ | ✓ | ✓ |
| Kasir | ✓ | | ✓ |
| Produk, Kategori, Pelanggan | ✓ | ✓ | |
| Transaksi | ✓ | ✓ | ✓ |
| Laporan Penjualan | ✓ | ✓ | |
| Pengguna, Pengaturan | ✓ | | |

Pembatalan transaksi hanya untuk Admin dan Manager. Pengguna yang sudah masuk tetapi perannya tidak mencukupi akan melihat halaman "Akses ditolak", bukan dilempar kembali ke halaman login.

## Rute

Setiap halaman memiliki rute Indonesia sekaligus rute Inggris lamanya, sehingga tautan lama tidak patah.

| Indonesia | Lama |
|---|---|
| `/` | |
| `/pos`, `/kasir` | `/pos` |
| `/produk` | `/products` |
| `/kategori` | `/categories` |
| `/pelanggan` | `/customers` |
| `/pengguna` | `/users` |
| `/transaksi` | `/transactions` |
| `/laporan-penjualan` | `/sales-report` |
| `/pengaturan` | `/settings` |

Endpoint di luar halaman:

| Rute | Kegunaan |
|---|---|
| `/api/v1/...` | REST API untuk aplikasi luar, dilindungi kunci API |
| `/swagger` | Dokumentasi interaktif REST API |
| `/swagger/v1/swagger.json` | Dokumen OpenAPI |
| `/api/payments/{provider}/callback` | Webhook penyedia pembayaran |
| `/pembayaran/{sukses|gagal}` | Halaman kembalinya pelanggan dari gateway |
| `/kesalahan` | Halaman kesalahan untuk lingkungan produksi |

## Perintah

```bash
dotnet build
dotnet run                        # profil https, port 7198 dan 5296
dotnet run --launch-profile http  # http saja, port 5296
```

Belum ada berkas uji otomatis di dalam repositori. Verifikasi selama pengembangan dilakukan
lewat skrip Chrome DevTools Protocol yang dijelaskan di [tangkapan-layar.md](tangkapan-layar.md),
dan REST API dapat diperiksa langsung dari halaman `/swagger`.
