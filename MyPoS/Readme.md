# MyPoS

**Aplikasi kasir berbasis web untuk toko dan minimarket.** Dibangun dengan .NET 10 Blazor Server, MudBlazor, dan EF Core. Mendukung empat basis data, empat penyimpanan berkas, pembayaran digital lewat Xendit/Midtrans/Stripe, laporan PDF, dan REST API untuk integrasi aplikasi luar.

![Dasbor MyPoS](docs/screenshots/02-dasbor.png)

---

## Daftar isi

- [Sekilas](#sekilas)
- [Tangkapan layar](#tangkapan-layar)
- [Menjalankan](#menjalankan)
- [Akun bawaan](#akun-bawaan)
- [Fitur](#fitur)
- [Basis data](#basis-data)
- [Penyimpanan berkas](#penyimpanan-berkas)
- [Impor data master](#impor-data-master)
- [Pembayaran digital](#pembayaran-digital)
- [Pajak dan mata uang](#pajak-dan-mata-uang)
- [REST API](#rest-api)
- [Dokumentasi lengkap](#dokumentasi-lengkap)
- [English summary](#english-summary)

---

## Sekilas

MyPoS dirancang untuk dipakai berdiri di depan konter: satu layar untuk memindai barang, satu panel untuk struk, dan satu tombol untuk membayar. Seluruh nominal ditampilkan dengan angka monospace tabular sehingga kolom rupiah selalu lurus dan selisihnya langsung terlihat.

| | |
|---|---|
| **Kerangka** | .NET 10 · Blazor Server · MudBlazor 9 |
| **Basis data** | SQLite (bawaan), SQL Server, PostgreSQL, MySQL/MariaDB |
| **Penyimpanan berkas** | Sistem berkas lokal, Azure Blob, AWS S3, MinIO |
| **Pembayaran** | Tunai, Xendit, Midtrans, Stripe |
| **Ekspor** | PDF, Excel, CSV |
| **Integrasi** | REST API dengan kunci akses, didokumentasikan lewat Swagger |
| **Bahasa antarmuka** | Indonesia, mata uang Rupiah |

---

## Tangkapan layar

### Halaman masuk
![Halaman masuk](docs/screenshots/01-login.png)

### Kasir
Katalog di kiri, struk berjalan di kanan. Pajak, diskon, kembalian, dan metode pembayaran dihitung langsung saat barang ditambahkan.

![Halaman kasir](docs/screenshots/03-kasir.png)

### Struk
Struk yang sama dipakai untuk pratinjau layar, pencetakan, dan penyalinan teks ke printer termal.

![Struk](docs/screenshots/06-struk.png)

### Riwayat transaksi
![Riwayat transaksi](docs/screenshots/05-transaksi.png)

### Laporan penjualan
![Laporan penjualan](docs/screenshots/07-laporan.png)

### Laporan PDF
Ekspor PDF memakai kolom, ringkasan, dan penyaring yang sama dengan yang sedang tampil di layar.

![Laporan PDF](docs/screenshots/13-laporan-pdf.png)

### Produk
![Manajemen produk](docs/screenshots/04-produk.png)

### Impor data master
Template siap pakai, lalu berkas diperiksa baris per baris sebelum satu baris pun tersimpan.

![Dialog impor](docs/screenshots/14-impor-template.png)
![Pratinjau impor](docs/screenshots/15-impor-pratinjau.png)

### Pengaturan
Semua nilai yang sering berubah tinggal di sini, lengkap dengan simulasi perhitungan yang memakai mesin hitung yang sama dengan kasir.

![Pengaturan pajak](docs/screenshots/08-pengaturan-pajak.png)
![Pengaturan pembayaran](docs/screenshots/09-pengaturan-pembayaran.png)

### REST API
![Kunci API](docs/screenshots/11-pengaturan-api.png)
![Swagger UI](docs/screenshots/12-swagger.png)

### Mode gelap
![Mode gelap](docs/screenshots/10-dasbor-gelap.png)

Semua tangkapan layar dan cara membuatnya ulang ada di [docs/tangkapan-layar.md](docs/tangkapan-layar.md).

---

## Menjalankan

Prasyarat: **.NET SDK 10.0** atau lebih baru.

```bash
dotnet build
dotnet run
```

Aplikasi terbuka di `https://localhost:7198` (atau `http://localhost:5296`). Basis data SQLite beserta data contohnya dibuat otomatis saat pertama kali dijalankan — tidak perlu menyiapkan apa pun.

Profil tanpa HTTPS:

```bash
dotnet run --launch-profile http
```

---

## Akun bawaan

| Nama pengguna | Kata sandi | Peran | Akses |
|---|---|---|---|
| `admin` | `admin123` | Admin | Semua halaman termasuk Pengaturan dan Pengguna |
| `manager` | `manager123` | Manager | Data master, transaksi, laporan |
| `operator` | `operator123` | Operator | Kasir dan riwayat transaksi |

Kata sandi disimpan sebagai hash **PBKDF2-SHA256** dengan salt acak per pengguna. Gantilah kata sandi bawaan sebelum dipakai sungguhan.

---

## Fitur

**Kasir**
- Pemindaian barcode: ketik atau pindai lalu tekan Enter, barang langsung masuk keranjang dan kolom kembali fokus untuk pindaian berikutnya
- Penyaring kategori dan pencarian nama produk
- Diskon per transaksi berupa persen maupun nominal
- Perhitungan uang diterima, kembalian, dan kekurangan bayar dengan saran pecahan uang
- Pemilihan pelanggan untuk poin loyalitas
- Struk yang bisa dicetak, disalin sebagai teks, dan dicetak ulang kapan saja

**Data master**
- Produk lengkap dengan harga modal, margin, ambang stok per produk, gambar, dan status aktif
- Kategori, pelanggan, serta pengguna dengan peran
- Impor massal dari Excel dengan template siap pakai dan pratinjau sebelum disimpan
- Ekspor Excel dan CSV di setiap halaman daftar

**Transaksi dan laporan**
- Riwayat transaksi dengan penyaring tanggal, status, dan pencarian
- Pembatalan transaksi yang mengembalikan stok serta menarik kembali poin loyalitas
- Pemeriksaan ulang status pembayaran langsung ke penyedia
- Laporan penjualan per barang dengan harga pokok, laba kotor, dan margin
- Ekspor PDF siap cetak dengan kop toko, ringkasan, dan nomor halaman

**Operasional**
- Sesi bertahan setelah halaman dimuat ulang, penting di konter yang jaringannya kurang stabil
- Mode terang dan gelap, tersimpan per peramban
- Basis data SQLite otomatis disalin sebelum dibuat ulang bila skema berubah

---

## Basis data

Ganti satu baris konfigurasi untuk berpindah penyedia:

```json
"Database": {
  "Provider": "PostgreSql",
  "ConnectionString": "Host=localhost;Port=5432;Database=mypos;Username=postgres;Password=rahasia"
}
```

| Nilai `Provider` | Penyedia |
|---|---|
| `Sqlite` *(bawaan)* | SQLite — tidak perlu server apa pun |
| `SqlServer` | SQL Server |
| `PostgreSql` | PostgreSQL |
| `MySql` | MySQL / MariaDB |

Model entitas dan seluruh kueri sama untuk keempatnya. Perbedaan antar penyedia — panjang kolom terindeks untuk MySQL, presisi decimal, dan pemetaan waktu Npgsql — sudah ditangani di dalam kode.

Basis data server **tidak pernah dihapus otomatis** saat skema berubah; hanya SQLite yang dibuat ulang, dan itupun setelah berkas lamanya disalin. Rinciannya di [docs/basis-data.md](docs/basis-data.md).

---

## Penyimpanan berkas

Gambar produk dan logo toko dapat disimpan di empat tempat:

```json
"Storage": {
  "Provider": "MinIO",
  "ServiceUrl": "http://localhost:9000",
  "BucketOrContainerName": "mypos-uploads",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin"
}
```

| Nilai `Provider` | Penyedia |
|---|---|
| `FileSystem` *(bawaan)* | Berkas lokal di `wwwroot/uploads/` |
| `AzureBlob` | Azure Blob Storage |
| `AwsS3` | AWS S3 |
| `MinIO` | MinIO atau penyimpanan kompatibel-S3 lain |

S3 dan MinIO memakai satu implementasi yang sama karena protokolnya identik. Rinciannya di [docs/penyimpanan.md](docs/penyimpanan.md).

---

## Impor data master

Halaman Produk, Kategori, dan Pelanggan masing-masing punya tombol **Impor Excel**:

1. Unduh template — berisi judul kolom yang benar, baris contoh, dan lembar petunjuk
2. Isi berkasnya
3. Unggah — berkas diperiksa **tanpa menulis apa pun**
4. Periksa pratinjau — setiap baris menunjukkan akan ditambah, diperbarui, dilewati, atau bermasalah beserta alasannya
5. Simpan — hanya baris yang lolos yang ditulis, dan kegagalan di tengah membatalkan seluruhnya

Produk dicocokkan lewat barcode, pelanggan lewat telepon, sehingga berkas yang sama dapat
diunggah ulang untuk memperbarui data tanpa menghasilkan duplikat. Duplikat di dalam berkas
itu sendiri juga terdeteksi, lengkap dengan nomor baris pasangannya.

Rincian kolom dan aturannya ada di [docs/impor.md](docs/impor.md).

---

## Pembayaran digital

Aktifkan penyedia di **Pengaturan → Pembayaran**, isi kuncinya, lalu penyedia tersebut langsung muncul sebagai pilihan di halaman kasir.

| Penyedia | Yang perlu diisi | Catatan |
|---|---|---|
| **Xendit** | Secret Key, Callback Verification Token | Memakai Invoice API |
| **Midtrans** | Server Key, Client Key, mode produksi | Memakai Snap; sandbox aktif secara bawaan |
| **Stripe** | Secret Key, kode mata uang | Memakai Checkout Session |

Alamat webhook yang perlu didaftarkan di dasbor masing-masing penyedia tertera langsung di halaman Pengaturan, berbentuk:

```
https://domain-anda.com/api/payments/{xendit|midtrans|stripe}/callback
```

Isi notifikasi hanya dipakai untuk mengetahui invoice mana yang berubah; statusnya selalu ditanyakan ulang langsung ke penyedia, sehingga notifikasi palsu tidak dapat menandai transaksi sebagai lunas.

Rincian lengkap ada di [docs/pembayaran.md](docs/pembayaran.md).

---

## Pajak dan mata uang

Tarif pajak, namanya, dan cara penerapannya seluruhnya diatur dari halaman Pengaturan:

- **Pajak eksklusif atau inklusif** — pajak ditambahkan di atas harga, atau diurai dari harga yang sudah tertera
- **Dasar Pengenaan Pajak setelah diskon** — sesuai praktik umum, DPP adalah nilai setelah potongan
- **Biaya layanan** dengan pilihan ikut dikenai pajak atau tidak
- **Pembulatan total** ke kelipatan 100, 500, atau 1.000
- **Mata uang** — simbol, kode, culture, jumlah desimal, dan posisi simbol; bawaannya Rupiah dengan nol desimal

Setiap transaksi menyimpan tarif pajak yang berlaku saat itu, sehingga mengubah tarif tidak pernah mengubah struk dan laporan lama.

Rumus dan contoh perhitungan ada di [docs/pajak.md](docs/pajak.md).

---

## REST API

Aplikasi luar dapat membaca dan menulis data lewat REST API di `/api/v1`, didokumentasikan secara interaktif di **`/swagger`**.

```bash
curl -H "X-Api-Key: mps_xxxxxxxxxxxxxxxx" \
     http://localhost:5296/api/v1/products?activeOnly=true
```

Kunci dibuat dari **Pengaturan → API**. Setiap kunci dapat diberi izin **baca saja** atau **baca & tulis**, tanggal kedaluwarsa, dan dapat dinonaktifkan kapan saja. Kunci disimpan sebagai hash — nilainya hanya diperlihatkan satu kali saat dibuat.

Tersedia untuk produk, kategori, pelanggan, transaksi, dan laporan. Membuat transaksi lewat API menjalankan alur yang sama persis dengan halaman kasir: validasi stok, perhitungan pajak, pemanggilan penyedia pembayaran, pengurangan stok, dan poin loyalitas.

Daftar endpoint lengkap ada di [docs/api.md](docs/api.md).

---

## Dokumentasi lengkap

| Berkas | Isi |
|---|---|
| [docs/arsitektur.md](docs/arsitektur.md) | Struktur proyek, alur data, keputusan rancangan |
| [docs/basis-data.md](docs/basis-data.md) | SQLite, SQL Server, PostgreSQL, MySQL |
| [docs/penyimpanan.md](docs/penyimpanan.md) | Sistem berkas, Azure Blob, AWS S3, MinIO |
| [docs/pengaturan.md](docs/pengaturan.md) | Rujukan seluruh parameter yang tersedia |
| [docs/pajak.md](docs/pajak.md) | Aturan perhitungan pajak, diskon, dan pembulatan |
| [docs/pembayaran.md](docs/pembayaran.md) | Pemasangan Xendit, Midtrans, Stripe, dan webhook |
| [docs/api.md](docs/api.md) | REST API, kunci akses, dan daftar endpoint |
| [docs/laporan-pdf.md](docs/laporan-pdf.md) | Ekspor PDF dan cara menambah laporan baru |
| [docs/impor.md](docs/impor.md) | Impor data master dari Excel beserta templatnya |
| [docs/tangkapan-layar.md](docs/tangkapan-layar.md) | Galeri tangkapan layar dan cara membuatnya ulang |

---

## English summary

**MyPoS** is a web-based point-of-sale application for shops and minimarkets, built on .NET 10 Blazor Server with MudBlazor and EF Core. The interface is in Indonesian and defaults to Rupiah, but currency, tax, receipt, stock, and payment settings are all configurable from the in-app Settings page rather than being hard-coded.

Run it with `dotnet build && dotnet run`; the SQLite database and its sample data are created on first start. Sign in as `admin` / `admin123`.

Highlights: barcode-driven checkout, configurable tax (exclusive/inclusive, applied after discount, optional service charge and rounding), cash-and-change handling, printable receipts, transaction history with void and stock restoration, gross-margin sales reporting with professional PDF export, bulk master-data import from Excel (downloadable templates, dry-run preview before anything is written), and card/wallet payments through Xendit, Midtrans, and Stripe.

It runs on **SQLite, SQL Server, PostgreSQL, or MySQL** and stores uploaded files on the **local file system, Azure Blob, AWS S3, or MinIO** — each selected with a single configuration value. A **REST API** at `/api/v1`, secured with per-key read or read-write API keys and documented with Swagger at `/swagger`, exposes products, categories, customers, transactions, and reports to external applications.

Full documentation lives in `docs/` (in Indonesian).
