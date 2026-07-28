# 📋 Progress Development VirtualDoctor

Catatan realisasi pekerjaan. Rencana ke depan ada di [PLAN.md](PLAN.md).
ID pekerjaan (`P0-1`, `P1-3`, …) mengacu ke roadmap tersebut.

Terakhir diperbarui: **28 Juli 2026**

---

## Status ringkas

| Fase | Selesai | Berjalan | Belum |
|---|---|---|---|
| Fondasi (sebelum roadmap) | 15 modul | — | — |
| Fase 1 · Keamanan & kepercayaan | 2 | 0 | 3 |
| Fase 2 · Melengkapi fitur | 3 | 0 | 5 |
| Fase 3 · Skala & operasional | 0 | 0 | 7 |
| Fase 4 · Kualitas & rilis | 0 | 0 | 4 |

**Kondisi build:** `dotnet build` sukses, tanpa error.
**Kondisi test:** belum ada proyek pengujian (lihat P3-1).

---

## Papan pekerjaan roadmap

| ID | Pekerjaan | Status | Mulai | Selesai | Catatan |
|---|---|---|---|---|---|
| P0-1 | Pindahkan & rotasi kredensial | Belum | — | — | Menunggu tindakan pemilik akun |
| P0-2 | Hash kata sandi yang layak | Selesai | 28 Jul 2026 | 28 Jul 2026 | PBKDF2 + migrasi bertahap saat login |
| P0-3 | Peran berbasis database | Selesai | 28 Jul 2026 | 28 Jul 2026 | Tabel `UserRoles`, claim, UI pemberian peran |
| P0-4 | Embedding RAG sungguhan | Belum | — | — | Bergantung P0-1 |
| P0-5 | Kegagalan AI harus terlihat | Belum | — | — | |
| P1-1 | Kernel function bertransaksi | Belum | — | — | |
| P1-2 | Pakai `Microsoft.Extensions.VectorData` | Belum | — | — | Bergantung P0-4 |
| P1-3 | Alur farmasi lengkap | Belum | — | — | |
| P1-4 | EF Migrations | Belum | — | — | Sementara ditambal `SchemaUpgrader` |
| P1-5 | Integrasi asuransi | Belum | — | — | Menunggu keputusan bisnis |
| P1-6 | Pembayaran & penagihan | Selesai | 28 Jul 2026 | 28 Jul 2026 | 4 penyedia, QRIS dinamis, verifikasi, invoice, kuitansi, laporan keuangan |
| P1-7 | Pengerasan webhook penyedia bayar | Selesai | 28 Jul 2026 | 28 Jul 2026 | Jejak `PaymentWebhookEvents`, tolak kiriman ulang & status mundur, `/admin/webhooks` |
| P1-8 | Nomor invoice aman dari balapan | Selesai | 28 Jul 2026 | 28 Jul 2026 | Tabel `InvoiceCounters`, 200 alokasi serentak diverifikasi |
| P2-1 | Streaming AI sesungguhnya | Belum | — | — | |
| P2-2 | Perbaiki cache kernel | Belum | — | — | |
| P2-3 | Filter & paging di database | Belum | — | — | |
| P2-4 | Realtime tanpa polling | Belum | — | — | |
| P2-5 | Jejak audit | Belum | — | — | |
| P2-6 | Moderasi ulasan | Belum | — | — | |
| P2-7 | Rekonsiliasi & pengembalian dana | Belum | — | — | Bergantung P1-6 |
| P3-1 | Proyek pengujian | Belum | — | — | |
| P3-2 | Integrasi berkelanjutan | Belum | — | — | |
| P3-3 | Observability | Belum | — | — | |
| P3-4 | Dokumentasi operasional | Belum | — | — | |

---

## Riwayat iterasi

### 28 Juli 2026 (larut) — P1-8 Penomoran invoice & P1-7 Pengerasan webhook

Dua tindak lanjut langsung dari P1-6. Keduanya menyangkut hal yang sama: apa yang terjadi
ketika dua hal datang bersamaan.

#### P1-8 · Nomor invoice aman dari balapan

`NextInvoiceNumberAsync` membaca nomor terakhir dari tabel `Payments` lalu menambah satu.
Karena kolom `InvoiceNumber` berindeks unik, dua checkout yang bersamaan membaca nomor yang
sama dan salah satunya **gagal tersimpan** — pasien melihat checkout error, bukan sekadar
nomor kembar.

| Berkas | Perubahan |
|---|---|
| `Models/Payment.cs` | Entitas `InvoiceCounter` (satu baris per awalan bulan) |
| `Services/Payment/InvoiceNumbering.cs` | **Baru.** Alokator: `UPDATE` menaikkan sekaligus mengunci baris sampai commit, baca balik di transaksi yang sama, coba lagi bila berebut |
| `Services/Payment/PaymentService.cs` | `NextInvoiceNumberAsync` memanggil alokator |
| `Data/AppDbContext.cs`, `Data/SchemaUpgrader.cs` | Tabel `InvoiceCounters` |

Awalan yang belum punya penghitung diisi dari nomor tertinggi yang **sudah terbit**, sehingga
database lama tidak mengulang nomor yang sudah tercetak di invoice pasien.

#### P1-7 · Pengerasan webhook penyedia bayar

Seluruh isi endpoint webhook dipindahkan dari `Program.cs` ke `PaymentWebhookService`. Alasannya
bukan kerapian: dengan pemeriksaan terkumpul di satu tempat, kiriman dapat dicatat apa pun
hasilnya dan isi yang sama dapat dijalankan ulang lewat jalur yang identik.

| Berkas | Perubahan |
|---|---|
| `Services/Payment/PaymentWebhookService.cs` | **Baru.** Pemeriksaan keaslian, pemetaan status, sidik jari isi, pencatatan, dan proses ulang |
| `Models/Payment.cs` | `PaymentWebhookEvent`, `WebhookOutcome`, label & pil warnanya |
| `Services/Payment/PaymentService.cs` | `ApplyExternalStatusAsync` mengembalikan alasan, bukan `bool`; `CanTransition` menolak status mundur |
| `Components/Pages/Admin/WebhookLog.razor` | **Baru.** `/admin/webhooks`: jejak, filter, isi kiriman, tombol proses ulang |
| `Program.cs` | Kedua endpoint menyusut menjadi pemanggil layanan |
| `Components/Pages/Admin/SystemSettings.razor` | **Perbaikan:** alamat webhook yang ditampilkan salah — `/api/payment/...` padahal endpointnya `/api/payments/...`. Operator yang menyalinnya akan mendaftarkan URL yang menghasilkan 404 |

Empat lapis pertahanan, berurutan: tanda tangan tidak cocok → `Ditolak`; isi yang sidik jarinya
sudah pernah diproses → `Kiriman ulang`; status yang memundurkan (lunas menjadi menunggu) →
`Diabaikan`; notifikasi bertanggal lebih dari 7 hari → `Diabaikan`. Kiriman yang **ditolak**
tidak pernah bisa diproses ulang, karena kalau bisa, tombol itu menjadi jalan memutar
pemeriksaan keaslian.

**Cara diverifikasi.** Enam skenario dijalankan terhadap aplikasi yang berjalan, dengan
notifikasi Midtrans bertanda tangan SHA-512 yang dibentuk sendiri:

```
1. Lima kiriman identik 'settlement'
   kiriman 1: HTTP 200 · Processed · Status menjadi Lunas.
   kiriman 2-5: HTTP 200 · Duplicate · Kiriman ulang, status tidak diubah.
   -> status tagihan: Paid (satu kali perubahan, PaidAt terisi sekali)
2. Tanda tangan salah      HTTP 401 · Rejected
3. 'pending' setelah lunas HTTP 200 · Ignored · Perubahan Lunas → Menunggu pembayaran tidak diizinkan.
4. Tertanggal 01 Mei 2026  HTTP 200 · Ignored · sudah kedaluwarsa
5. 'refund' sah            HTTP 200 · Processed · Status menjadi Dikembalikan.
6. Xendit tanpa token      HTTP 401 · Rejected
```

Jejak yang tersimpan: satu baris `Processed` dengan `kiriman=5`, bukan lima baris.
`/admin/webhooks` menampilkan keenamnya dengan tombol proses ulang hanya pada empat baris
yang bukan `Ditolak`. Proses ulang diuji tersendiri: tagihan dikembalikan ke `Pending`, lalu
menjalankan ulang kiriman `settlement` membuatnya `Paid` lagi dan mencatat pelakunya;
menjalankan ulang kiriman yang ditolak dijawab `HTTP 403`.

Penomoran diuji dengan 200 alokasi serentak dan 40 checkout serentak lewat `PaymentService`
yang sungguhan:

```
{"total":200,"unik":200,"min":1,"max":200,"berurutan":true,
 "checkout_berhasil":40,"checkout_unik":40,"checkout_gagal":[]}
```

Seluruh data uji dihapus setelahnya (846 tagihan, sama seperti sebelum pengujian), endpoint
uji sementara dibuang dari `Program.cs`, dan kunci Midtrans percobaan dihapus dari tabel
`AppSettings`. Penghitung `INV/2026/07/` disetel kembali ke 300 agar 40 nomor uji tidak
meninggalkan celah.

**Yang belum dikerjakan.** Kedua penyedia belum diuji terhadap sandbox sungguhan; payload uji
dibentuk sendiri sesuai dokumentasi mereka. Bentuk payload nyata bisa berbeda, jadi tetap
periksa `/admin/webhooks` saat pertama kali menghubungkan akun sungguhan.

---

### 28 Juli 2026 (malam) — P1-6 Pembayaran, penagihan, dan laporan keuangan

Sebelumnya tidak ada tagihan sama sekali: `Order.PaymentStatus` diisi manual, konsultasi dan
homecare tidak punya jejak uang, dan tidak ada dokumen yang bisa diberikan ke pasien.

**Domain dan penyedia**

| Berkas | Isi |
|---|---|
| `Models/Payment.cs` | Entitas `Payment` (nomor invoice, rujukan transaksi, nominal, kanal, status, jejak verifikasi) + `PaymentLabels` untuk label Indonesia |
| `Models/AppConfig.cs` | `PaymentConfig`: penyedia aktif, batas waktu, biaya penanganan, identitas penerbit tagihan, QRIS, rekening manual, kredensial Midtrans dan Xendit |
| `Services/Payment/QrisPayload.cs` | Pembaca dan penulis payload EMVCo: mengubah QR statis merchant menjadi QR dinamis bernominal, menghitung ulang CRC-16/CCITT-FALSE |
| `Services/Payment/PaymentProviders.cs` | `ManualPaymentProvider`, `QrisPaymentProvider`, `MidtransPaymentProvider` (Core API), `XenditPaymentProvider` (v2) |
| `Services/Payment/PaymentService.cs` | Penerbitan tagihan, penomoran invoice per bulan, unggah bukti, verifikasi, tanya status ke penyedia, penandaan kedaluwarsa, penyelarasan status transaksi asal |
| `Data/SchemaUpgrader.cs` | Tabel `Payments` dibuat idempoten beserta indeks unik nomor invoice |

Penyedia yang gagal tidak memblokir pasien: pembuatan tagihan jatuh kembali ke transfer
manual dan dicatat sebagai `LogError`, sehingga transaksi tetap bisa diselesaikan.

**Antarmuka**

| Halaman | Fungsi |
|---|---|
| `/bayar/{id}` | Pilih cara bayar, QR dinamis bernominal, rincian rekening, hitung mundur, unggah bukti, tanya status ke penyedia |
| `/invoice/{id}` · `/kuitansi/{id}` | Dokumen cetak A4 dengan terbilang; kuitansi menolak terbit sebelum lunas |
| `/tagihan` | Riwayat tagihan pasien beserta tautan bayar, invoice, dan kuitansi |
| `/admin/payments` | Antrean verifikasi (yang menunggu diperiksa naik ke atas), lihat bukti, setujui atau tolak, tandai kedaluwarsa |
| `/admin/settings` → tab **Pembayaran** | Penyedia aktif, identitas penerbit tagihan, payload QRIS dengan tombol uji, rekening manual, kredensial gateway, alamat webhook |

Checkout farmasi kini menerbitkan tagihan lalu mengarahkan pasien ke halaman bayar. Bila
pembuatan tagihan gagal, pesanan tetap tersimpan dan pasien diarahkan ke menu Tagihan Saya.

**Laporan keuangan**

`DashboardService.BuildFinanceAsync` menghitung dari tabel `Payments`, bukan dari nilai
transaksi — yang dilaporkan adalah uang yang benar-benar tertagih. Isinya: lima KPI (kas
masuk, tagihan terbit, tingkat penagihan, nilai rata-rata, piutang berjalan), grafik arus kas,
komposisi cara bayar dan lini layanan, umur piutang, ringkasan pembukuan, dan buku tagihan
yang dapat diunduh sebagai CSV.

**Bug yang ditemukan saat pengujian**

| Temuan | Penanganan |
|---|---|
| Grafik arus kas menumpuk "tagihan terbit" di atas "kas masuk", padahal keduanya saling tumpang tindih sehingga jumlahnya tidak bermakna | `vdCharts` menerima `stack: false`; kedua deret digambar dari nol dengan opasitas lebih rendah |
| `<select>` yang di-bind ke `bool` tidak memilih option apa pun — kolom Status di tab Pembayaran tampil kosong | Field form diubah menjadi string `"true"`/`"false"` |
| `SettingsService.ApplyToConfig` tidak mengenal `decimal`, sehingga `Payment:ServiceFee` diam-diam terbuang | Ditambah konversi `decimal` dan `long`, serta pembacaan baliknya |
| `LooksSecret` tidak menutupi `ServerKey` dan `CallbackToken` | Kedua pola ditambahkan |
| Tombol "Kembali" pada invoice dan kuitansi memakai warna teks tema, padahal latar halaman dokumen selalu gelap — pada tema terang labelnya hilang | Tombol garis luar di `.vd-doc-toolbar` dipaksa terang |
| Halaman `/admin/payments` dan `/tagihan` memuat seluruh baris sekaligus (846 baris, tinggi halaman 8385 px) | Paginasi 25 baris mengikuti pola `UsersManagement` |
| Nominal terpotong antara "Rp" dan angkanya di tabel sempit | `.vd-num` diberi `white-space: nowrap` |

**Verifikasi**

| Uji | Hasil |
|---|---|
| Alur nyata: pasien belanja → checkout → halaman bayar | Tagihan `INV/2026/07/0300` terbit, diarahkan ke `/bayar/{id}`, QR PNG 392 px tampil |
| Payload QRIS dinamis dari tagihan tersebut dibaca ulang dari database | Tag `01`=`12` (dinamis), tag `54`=`32000` sesuai total, urutan tag `00,01,26,52,53,54,58,59,60,61,63`, CRC `003E` cocok dengan hitungan ulang |
| Uji payload QRIS di UI pengaturan | "Payload valid untuk merchant VIRTUALDOCTOR DEMO", contoh QR Rp10.000 tergambar |
| Invoice `/invoice/{id}` | Terbilang "Tiga puluh dua ribu rupiah", QR pembayaran ikut tercetak |
| Kuitansi untuk tagihan belum lunas | Ditolak dengan penjelasan, mengarahkan kembali ke halaman bayar |
| Admin menyetujui tagihan lalu membuka kuitansi | Cap LUNAS, tanggal bayar, dan nama pemeriksa tampil |
| Paginasi `/admin/payments` | 25 baris per halaman, "Hal 1/34 · Total 846 tagihan" |
| Seluruh permintaan HTTP pada alur pembayaran | Tidak ada respons ≥ 400 |

**Catatan untuk operasional**

- Penyedia bawaan adalah **Manual**; QRIS, Midtrans, dan Xendit baru aktif setelah
  kredensialnya diisi lewat `/admin/settings`.
- Payload QRIS yang dipakai saat pengujian adalah **merchant contoh**, bukan merchant
  sungguhan. Ganti sebelum dipakai menerima uang.
- Data contoh menerbitkan tagihan untuk transaksi demo yang sudah ada; langkah ini idempoten
  dan berhenti bila tabel `Payments` sudah terisi.

---

### 28 Juli 2026 (sore) — P0-2 Hash kata sandi & P0-3 Peran berbasis database

**P0-2 · Hash kata sandi**

Sebelumnya `AuthHelpers.HashPassword` memakai SHA-256 dengan salt tetap yang sama untuk
seluruh akun. Akibatnya terlihat langsung di database: tiga pengguna berbeda dengan sandi
sama menyimpan hash yang **persis identik**, sehingga serangan rainbow table menjadi praktis.

| Berkas | Perubahan |
|---|---|
| `Services/AuthHelpers.cs` | `HashPassword` memakai `PasswordHasher<ApplicationUser>` (PBKDF2-HMAC-SHA256, salt acak per sandi). `VerifyPassword` baru mengembalikan `Failed`/`Success`/`SuccessNeedsRehash` dan masih menerima hash lama. Ditambah `ValidatePasswordRules` |
| `Program.cs`, `Services/CoreServices.cs` | Seluruh pemeriksaan sandi lewat `VerifyPassword`; hash lama ditulis ulang begitu sandi terbukti benar |

Migrasi berjalan bertahap: akun lama tetap bisa masuk dengan sandi yang sama, lalu hash-nya
otomatis naik ke format baru. Tidak ada reset sandi massal.

**P0-3 · Peran berbasis database**

Sebelumnya administrator ditentukan dari perbandingan alamat email
`admin@virtualdoctor.com`, diulang di `AuthService.IsAdmin()` dan di endpoint ekspor.
Peran tidak bisa diberikan maupun dicabut tanpa mengubah kode.

| Berkas | Perubahan |
|---|---|
| `Models/UserRole.cs` | Entitas peran + konstanta `AppRoles` |
| `Services/AuthClaims.cs` | Satu-satunya tempat pembentukan identitas login, dipakai oleh handler minimal API maupun `AuthService` |
| `Services/CoreServices.cs` | `IsAdmin`/`IsDoctor`/`GetDoctorId` membaca claim; `SetRolesAsync`, `GetRolesForAllAsync`, `CountAdminsAsync` |
| `Program.cs` | Endpoint ekspor memakai `RequireRole`, bukan pemeriksaan email inline |
| `Data/DataSeeder.cs` | `EnsureRolesAsync` memindahkan konvensi email lama menjadi data, idempoten |
| `Components/Pages/Admin/*.razor` | 11 halaman memakai `[Authorize(Roles = AppRoles.Admin)]` |
| `Components/Pages/Admin/UsersManagement.razor` | Kolom peran + dialog pemberian dan pencabutan peran |
| `Components/Shared/AccessDenied.razor`, `Components/Pages/AccessDeniedPage.razor`, `Components/Routes.razor` | Halaman akses ditolak yang benar |

Pengaman: peran administrator terakhir tidak dapat dicabut, dan penanda `IsDoctor` pada
profil ikut diselaraskan dengan peran.

**Bug yang ditemukan saat pengujian**

`AccessDeniedPath` diisi `"/auth/login?error=denied"`. Karena `PathString` meng-escape query
string, pengguna tanpa akses diarahkan ke URL rusak
`/auth/login%3Ferror=denied?ReturnUrl=…` dan berakhir di halaman login, bukan halaman
"akses ditolak". Diganti menjadi path bersih `/akses-ditolak`.

**Verifikasi**

| Uji | Hasil |
|---|---|
| Login akun berhash lama | Berhasil, hash otomatis berubah dari 64 karakter heksadesimal menjadi PBKDF2 84 karakter |
| Login sandi salah | Ditolak ke `?error=invalid` |
| Hash kembar setelah migrasi | Tinggal pada akun yang memang belum pernah login ulang |
| Backfill peran saat start | 11 pengguna diberi peran: 1 Admin, 3 Doctor, 7 Patient |
| Admin membuka 3 halaman admin + endpoint ekspor | Semuanya 200 |
| Pasien membuka halaman yang sama | Diarahkan ke `/akses-ditolak`, isi halaman admin tidak pernah terkirim |
| Mencabut administrator terakhir | Ditolak dengan pesan yang jelas, database tetap punya 1 admin |
| Memberi peran Admin ke pasien lalu login ulang | Halaman admin terbuka (200), membuktikan claim dibentuk dari data peran |
| Mencabut kembali peran tersebut | Database kembali ke kondisi semula |

**Catatan untuk operasional**

- Perubahan peran baru berlaku saat pengguna **masuk berikutnya**, karena peran ikut
  tersimpan di dalam cookie sesi.
- Sembilan akun contoh masih memakai hash lama sampai pemiliknya login. Ini disengaja;
  tidak ada data yang perlu dipulihkan.

---

### 28 Juli 2026 — Dashboard analitik, video call, backoffice, perbaikan UI/UX

Pekerjaan di luar roadmap (menutup celah yang ditemukan saat audit).

**Audit fitur**
Seluruh requirement ditelusuri ke kode lalu diverifikasi dengan menjalankan aplikasi.
Hasil lengkap di [docs/feature-audit.md](docs/feature-audit.md), rekomendasi di
[docs/recommendations.md](docs/recommendations.md).

Tiga temuan yang mengubah anggapan tentang kondisi proyek:

1. Dashboard lama **seluruhnya hardcoded di markup** — tidak ada query database sama sekali.
2. Embedding RAG memakai hash byte, bukan model semantik, sehingga hasil retrieval tidak bermakna.
3. Kegagalan Semantic Kernel ditelan diam-diam dan diganti pencocokan kata kunci lokal.

**Dashboard analitik dengan D3**

| Berkas | Peran |
|---|---|
| `Services/Analytics/DashboardService.cs` | Agregasi KPI, deret waktu, komposisi, peringkat, heatmap, tabel |
| `Components/Pages/Dashboard.razor` | Halaman dashboard, filter, tabel, ekspor CSV |
| `wwwroot/js/vd-charts.js` | Pustaka chart D3: area bertumpuk, donut, bar, heatmap, sparkline |
| `wwwroot/lib/d3/d3.min.js` | D3 v7.9.0 dibundel lokal, bukan CDN |

- Filter: rentang preset dan kustom, skala harian/mingguan/bulanan, spesialisasi, kota, kanal.
- KPI dibandingkan dengan periode sebelumnya, dilengkapi sparkline tren.
- Chart membaca CSS custom property sehingga ikut berubah saat tema diganti.
- Perbarui otomatis 30 detik yang dapat dimatikan; ekspor transaksi ke CSV.

**Video call untuk konsultasi**

| Berkas | Peran |
|---|---|
| `Services/Meeting/MeetingServices.cs` | `IMeetingService` + provider Jitsi, Zoom, Teams |
| `Models/AppConfig.cs` | Bagian `Meeting` beserta kredensial tiap provider |
| `Models/Consultation.cs`, `Models/Appointment.cs` | Kolom penyimpan tautan meeting |
| `Components/Pages/Consultations.razor` | Pilihan Chat/Video, tombol gabung dan salin tautan |

- Zoom lewat Server-to-Server OAuth, Teams lewat Microsoft Graph, Jitsi tanpa kredensial.
- Dapat diatur dari `appsettings.json` maupun halaman Pengaturan Sistem, lengkap dengan tombol uji koneksi.
- Bila pembuatan ruang gagal, konsultasi tetap berjalan lewat chat disertai pesan yang jelas.

**Backoffice**

| Halaman | Isi |
|---|---|
| `/admin/orders` | Status pesanan, status bayar, kurir, nomor resi |
| `/admin/consultations` | Pemantauan sesi berjalan, durasi, tautan video |
| `/admin/settings` | Provider AI, kredensial, video call, integrasi, RAG |

`Services/SettingsService.cs` dan `Models/AppSetting.cs` menyimpan perubahan ke database
lalu menerapkannya saat aplikasi start, sehingga requirement "dapat dikonfigurasi dari
appsetting **dan ui**" terpenuhi. Kolom rahasia dibiarkan kosong berarti nilai lama dipertahankan.

**Perbaikan UI/UX**

| Masalah | Perbaikan |
|---|---|
| Pada layar di bawah 992px sidebar menutupi konten secara bawaan (logika `:not(.collapsed)` terbalik) | Drawer dengan tombol menu dan lapisan gelap |
| Pilihan tema hilang setiap muat ulang dan sempat berkedip | Disimpan di localStorage, mengikuti preferensi sistem, diterapkan sebelum render pertama |
| Hanya input yang punya penanda fokus | Cincin fokus untuk semua kontrol, skip link, label ARIA |
| Animasi tidak menghormati preferensi pengguna | Dukungan `prefers-reduced-motion` |
| Tidak ada status memuat maupun kosong | Skeleton dan empty state |
| Kontras rendah pada tema gelap (segmented control, badge) | Token `--vd-on-surface-accent` khusus tema gelap |

**Perbaikan lain**

- `Services/Storage/StorageServices.cs` — kegagalan storage eksternal tidak lagi menjatuhkan
  aplikasi. Ditemukan karena aplikasi memang **gagal start**: `Storage:Provider` berisi AzureBlob
  dengan akun berstatus `AccountIsDisabled`.
- `Data/SchemaUpgrader.cs` — menambal 10 kolom baru pada database SQLite yang sudah ada,
  sehingga data lama tidak perlu dihapus. Solusi sementara sampai P1-4 selesai.
- `Data/DemoDataSeeder.cs` — transaksi contoh 90 hari agar dashboard punya data. Hanya berjalan
  di Development, hanya bila database masih kosong dari transaksi, dan dapat dimatikan lewat
  `Seed:DemoTransactions`.
- `Components/Pages/Home.razor` — statistik beranda diambil dari database, bukan angka tetap.
- `Components/App.razor` — favicon ditambahkan (sebelumnya 404 di setiap muat halaman).

**Verifikasi**

| Cara | Hasil |
|---|---|
| `dotnet build` | Sukses tanpa error |
| Jalankan aplikasi | Start bersih, skema tertambal 10 kolom, data contoh terisi |
| Muat halaman utama (login sebagai admin) | `/`, `/dashboard`, `/settings`, `/admin/settings`, `/admin/orders`, `/admin/consultations`, `/consultations` → semuanya HTTP 200 |
| Render browser (Edge headless) | 12 SVG chart ter-render pada tema terang dan gelap, tanpa empty state |
| Pengukuran geometri SVG dari DOM | Sparkline sesuai tinggi wadah, label angka 58px di luar batang |
| Pemantauan konsol browser | Tidak ada error selain satu peringatan pada alur login yang sudah ada sebelumnya |

**Bug yang ditemukan saat verifikasi visual dan langsung diperbaiki**

1. Sparkline KPI terpotong — `frame()` memaksa tinggi minimum 80px pada wadah 46px.
2. Kontras rendah pada tema gelap — `--vd-primary` tidak diturunkan untuk latar gelap.
3. Label enum berbahasa Inggris bocor ke tabel transaksi (`Unpaid`, `Vaccination`).
4. Indikator perubahan 0% menampilkan panah naik; kini memakai penanda netral.

---

### Sebelum 28 Juli 2026 — Fondasi platform

Dipindahkan dari `PLAN.md` versi lama. Seluruh modul di bawah ini sudah berjalan.

| Modul | Isi |
|---|---|
| Fondasi | Proyek Blazor Server .NET 10, paket NuGet, struktur folder, dependency injection |
| Database | 11 model entitas, `AppDbContext`, `EnsureCreated`, data awal (8 pengguna, 10 dokter, 12 obat, 7 rumah sakit) |
| Autentikasi | Cookie authentication, registrasi, login, reset sandi, profil pengguna |
| Tata letak | `MainLayout` dengan sidebar, tema terang/gelap, desain responsif |
| AI Chat | Semantic Kernel, lima provider LLM, riwayat percakapan, unggah gambar dan dokumen |
| Kernel functions | 11 alat: `searchInternet`, `checkDate`, `mathCalc`, `readFileFromUrl`, `describeImage`, `scrapWebPage`, `askDoctor`, `orderMedicine`, `scheduleDoctor`, `findHospital`, `queryHealthDocs` |
| RAG | Vector store (InMemory, SQLite, Qdrant, Chroma, Azure AI Search), pengindeksan PDF berkala |
| Layanan | Konsultasi, janji temu, farmasi, homecare, lokasi, artikel, rekomendasi, asuransi |
| Backoffice awal | CRUD pengguna, dokter, RS/klinik, obat, artikel, janji temu, jadwal, homecare |
| Penyimpanan | FileSystem, MinIO, S3, Azure Blob |
| Dokumentasi | `README.md`, `docs/architecture.md`, `docs/features.md`, `docs/deployment.md` |

Catatan: modul-modul di atas sebelumnya ditandai selesai seluruhnya. Audit 28 Juli 2026
menemukan bahwa **Dashboard & Reporting** ternyata hanya markup statis, dan beberapa
kernel function hanya mengembalikan teks tetap. Rinciannya ada di
[docs/feature-audit.md](docs/feature-audit.md).

---

## Hutang teknis yang diketahui

| Hal | Dampak | Rencana |
|---|---|---|
| `EnsureCreated` + `SchemaUpgrader` | Perubahan model tidak otomatis diterapkan pada provider selain SQLite | P1-4 |
| Agregasi dashboard di memori | Melambat saat data besar | P2-3 |
| Cache kernel tidak aman untuk banyak thread | Berpotensi rusak saat permintaan bersamaan | P2-2 |
| `Microsoft.Extensions.VectorData` direferensikan tapi tidak dipakai | ±700 baris kode HTTP buatan sendiri | P1-2 |
| Belum ada pengujian otomatis | Perubahan berisiko tanpa jaring pengaman | P3-1 |
| Data contoh dan data nyata berada di database yang sama | Angka dashboard bercampur saat pengembangan | Matikan `Seed:DemoTransactions` sebelum memakai data nyata |
| Webhook penyedia bayar belum diuji terhadap sandbox Midtrans/Xendit sungguhan | Bentuk payload sebenarnya bisa berbeda dari yang diasumsikan | Uji saat akun sandbox tersedia |
| Petugas belum bisa menerbitkan pengembalian dana | `Refunded` hanya bisa datang dari notifikasi penyedia | P2-7 |
| Jejak webhook menyimpan isi kiriman apa adanya | Payload penyedia bisa memuat data pribadi dan belum punya masa simpan | P2-5 (jejak audit) |

---

## Cara memperbarui dokumen ini

1. Saat mulai mengerjakan sebuah ID, ubah statusnya menjadi `Berjalan` dan isi kolom **Mulai**.
2. Saat selesai, ubah menjadi `Selesai`, isi kolom **Selesai**, dan tambahkan satu entri di
   **Riwayat iterasi** yang memuat: apa yang berubah, berkas mana, dan **bagaimana diverifikasi**.
3. Perbarui **Status ringkas** dan **Hutang teknis** bila ada perubahan.
4. Bila menemukan pekerjaan baru di luar roadmap, tambahkan dulu ke `PLAN.md` agar punya ID,
   baru catat realisasinya di sini.
