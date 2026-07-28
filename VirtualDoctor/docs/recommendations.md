# Rekomendasi Penyempurnaan VirtualDoctor

Disusun 28 Juli 2026, setelah audit kode dan uji jalan aplikasi.
Urutan berdasarkan risiko, bukan besarnya usaha.

---

## P0 — Perbaiki sebelum dipakai pengguna sungguhan

### 1. Kredensial asli tersimpan di `appsettings.json`

`appsettings.json` berisi kunci OpenAI, kunci Tavily, kunci Google Maps, dan
connection string Azure Storage lengkap dengan account key — semuanya dalam teks biasa.

**Dampak:** siapa pun yang mendapat salinan berkas ini dapat memakai kuota dan data Anda.
Akun Azure yang ada bahkan sudah berstatus `AccountIsDisabled`, indikasi kunci tersebut
kemungkinan sudah pernah bocor atau dinonaktifkan penyedia.

**Tindakan:**
1. Cabut dan terbitkan ulang keempat kredensial tersebut sekarang.
2. Pindahkan ke user-secrets (development) dan environment variable atau Key Vault (produksi).
3. Kosongkan nilainya di `appsettings.json`, isi lewat `/admin/settings` yang sudah tersedia.
4. Jika repositori ini akan di-`git init`, lakukan setelah nilai dibersihkan.

### 2. Kata sandi disimpan dengan hash tanpa garam — ✅ selesai 28 Juli 2026

`AuthHelpers.HashPassword` menghasilkan hash tanpa salt dan tanpa key stretching.
Dua pengguna dengan sandi sama menghasilkan hash identik, dan serangan rainbow table
menjadi praktis.

**Tindakan:** ganti ke `PasswordHasher<T>` bawaan ASP.NET Core (PBKDF2) atau Argon2id.
Sediakan jalur migrasi: verifikasi dengan algoritma lama, lalu tulis ulang hash baru saat
login berhasil.

### 3. Peran administrator ditentukan dari email hardcoded — ✅ selesai 28 Juli 2026

```csharp
public bool IsAdmin() => email == "admin@virtualdoctor.com";
```

Cek yang sama diulang di endpoint ekspor `/admin/export/{entity}`. Siapa pun yang bisa
mendaftar dengan email tersebut mendapat akses penuh, dan peran tidak bisa dicabut.

**Tindakan:** tambahkan tabel peran (`UserRole`), muat sebagai claim saat login, ganti
seluruh pemeriksaan menjadi `[Authorize(Roles = "Admin")]`, dan buat UI pemberian peran.

### 4. Embedding RAG bukan model semantik

`SimpleEmbeddingGenerator` membuat vektor dari hash byte. Pipeline RAG berjalan
mulus dari PDF sampai jawaban, tetapi dokumen yang diambil praktis acak.

**Dampak khusus di aplikasi kesehatan:** AI menjawab dengan nada meyakinkan berdasarkan
kutipan dokumen yang tidak relevan.

**Tindakan:** ganti dengan embedding sungguhan — `text-embedding-3-small` (OpenAI),
`nomic-embed-text` via Ollama untuk on-premise, atau layanan setara. Setelah itu
indeks ulang seluruh dokumen karena dimensi vektor berubah.

### 5. Kegagalan AI tersembunyi di balik fallback

Semua exception Semantic Kernel ditelan lalu dijawab pencocokan kata kunci lokal.
Operasional tidak akan tahu bahwa layanan AI mati.

**Tindakan:** pisahkan penanganan. Kegagalan kredensial/koneksi sebaiknya:
- tetap memberi jawaban fallback ke pasien, **dan**
- menaikkan status kesehatan (health check) serta indikator di dashboard admin,
- dicatat dengan level `Error`, bukan `Warning`.

---

## P1 — Melengkapi janji fitur

### 6. Kernel function yang belum benar-benar bekerja

Tiga dari sebelas alat hanya mengembalikan teks statis:

| Fungsi | Perilaku sekarang | Seharusnya |
|---|---|---|
| `scheduleDoctor` | "Kunjungi menu Booking" | Membaca `DoctorSchedule`, menawarkan slot, membuat `Appointment` |
| `askDoctor` | Teks rujukan | Membuat `Consultation` berstatus Waiting untuk dokter yang sesuai |
| `orderMedicine` | Menampilkan harga | Menambahkan ke keranjang / membuat `Order` draft |

Requirement menyebut "pemesanan obat dan jasa kesehatan (konsultasi, membuat/mengecek
jadwal dengan dokter)" lewat chat — bagian ini belum tercapai.

### 7. Pakai `Microsoft.Extensions.VectorData` sesuai requirement

Paket sudah direferensikan tetapi tidak dipakai; `VectorStoreService` memanggil
Qdrant/Chroma/Azure lewat HTTP mentah. Migrasi ke abstraksi resmi menghapus ±700 baris
kode buatan sendiri dan menghilangkan risiko perbedaan perilaku antarprovider.

### 8. Integrasi asuransi masih simulasi

`InsuranceService` memakai daftar provider dan rasio coverage yang ditulis di kode.
Jika ini akan ditawarkan komersial, perlu integrasi nyata (BPJS/asuransi swasta) atau
label yang jelas bahwa fitur ini demo.

### 9. Alur farmasi belum lengkap

Pemilihan apotek dan verifikasi asuransi tersedia di model dan service, tetapi tidak ada
di UI checkout. Stok juga tidak dikurangi saat pesanan dibuat.

### 10. Ganti `EnsureCreated` dengan EF Migrations

Saat ini perubahan model tidak diterapkan ke database yang sudah ada. Iterasi ini
menambal selisihnya lewat `SchemaUpgrader`, tetapi itu solusi sementara dan hanya untuk SQLite.

**Tindakan:** buat migrasi awal dari skema saat ini, lalu `Database.MigrateAsync()` saat start.

---

## P2 — Kualitas dan operasional

### 11. Streaming AI hanya simulasi

`SendStreamingMessageAsync` menunggu jawaban lengkap lalu memuntahkannya per kata dengan
jeda 25 ms. Ganti dengan `GetStreamingChatMessageContentsAsync` agar token pertama tiba
jauh lebih cepat.

### 12. Cache kernel tidak aman untuk banyak thread

`LlmProviderFactory` menyimpan `Dictionary<string, Kernel>` biasa pada objek singleton.
Dua permintaan bersamaan yang membuat provider sama dapat merusak isi dictionary.
Gunakan `ConcurrentDictionary`.

Selain itu, parameter `temperature` pada `GetKernel` diterima tetapi tidak pernah dipakai.

### 13. Halaman admin masih memuat seluruh tabel ke memori

Sebagian besar halaman admin memanggil `GetAllAsync()` lalu memfilter di memori.
Aman untuk data contoh, bermasalah saat data puluhan ribu baris. Pindahkan filter,
sortir, dan paging ke query database seperti yang sudah dilakukan `ArticleService.GetPagedAsync`.

Hal yang sama berlaku untuk `DashboardService`: agregasi dilakukan di memori.

### 14. Belum ada pengujian otomatis

Tidak ada proyek test sama sekali. Prioritas pertama yang layak diuji:
perhitungan `DashboardService`, konversi tipe `SettingsService`, dan alur
pembuatan meeting `MeetingService` (dengan HTTP handler tiruan).

### 15. Realtime yang sesungguhnya

Dashboard menyegarkan diri tiap 30 detik lewat timer. `ChatHub` dan `ConsultationHub`
sudah ada; menambahkan hub statistik yang mengirim pembaruan saat ada transaksi baru
akan memenuhi kata "realtime" pada requirement tanpa polling.

### 16. Jejak audit

Tidak ada catatan siapa mengubah data pasien, pesanan, atau jadwal. Untuk aplikasi
kesehatan ini biasanya wajib. Tambahkan tabel audit sederhana yang mencatat aktor,
entitas, aksi, dan waktu.

### 17. Moderasi ulasan

Ulasan dokter tampil apa adanya. Tambahkan halaman moderasi agar komentar tidak pantas
dapat disembunyikan.

---

## Yang sudah dikerjakan pada iterasi ini

| Perubahan | Berkas |
|---|---|
| Dashboard dari data sungguhan + D3 | `Services/Analytics/DashboardService.cs`, `Components/Pages/Dashboard.razor`, `wwwroot/js/vd-charts.js` |
| Integrasi Zoom / Teams / Jitsi | `Services/Meeting/MeetingServices.cs`, `Models/AppConfig.cs`, `Models/Consultation.cs` |
| Konfigurasi lewat UI + persistensi | `Services/SettingsService.cs`, `Models/AppSetting.cs`, `Components/Pages/Admin/SystemSettings.razor` |
| Backoffice pesanan & konsultasi | `Components/Pages/Admin/OrdersManagement.razor`, `ConsultationsManagement.razor` |
| Tema tersimpan + drawer mobile + aksesibilitas | `wwwroot/js/vd-app.js`, `wwwroot/app.css`, `Components/Layout/MainLayout.razor` |
| Storage eksternal tidak menjatuhkan aplikasi | `Services/Storage/StorageServices.cs` |
| Penambalan skema untuk database lama | `Data/SchemaUpgrader.cs` |
| Data contoh 90 hari untuk demo dashboard | `Data/DemoDataSeeder.cs` |
