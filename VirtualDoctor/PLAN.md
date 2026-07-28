# 🗺️ Roadmap Pengembangan VirtualDoctor

Dokumen ini berisi **rencana ke depan**. Riwayat pekerjaan yang sudah selesai dicatat di
[Progress.md](Progress.md). Dasar penyusunan roadmap ini adalah
[docs/feature-audit.md](docs/feature-audit.md) dan [docs/recommendations.md](docs/recommendations.md).

Terakhir diperbarui: **28 Juli 2026**

---

## Cara memakai dokumen ini

| Dokumen | Isi |
|---|---|
| `PLAN.md` (berkas ini) | Apa yang akan dikerjakan, urutannya, dan kapan dianggap selesai |
| `Progress.md` | Apa yang sudah dikerjakan, kapan, dan bagaimana diverifikasi |
| `docs/feature-audit.md` | Kondisi fitur saat ini dibanding requirements |
| `docs/recommendations.md` | Alasan teknis di balik setiap butir roadmap |

Setiap pekerjaan punya ID tetap (`P0-1`, `P1-3`, …). ID yang sama dipakai di `Progress.md`
sehingga rencana dan realisasi bisa ditelusuri.

Status yang dipakai: `Belum` · `Berjalan` · `Selesai` · `Ditunda`

---

## Kondisi saat ini

Platform sudah berfungsi untuk demo dan uji internal: konsultasi, farmasi, homecare,
booking, AI chat multi-LLM, RAG, dashboard analitik, dan backoffice sudah berjalan
dengan data sungguhan.

Yang menghalangi pemakaian oleh pengguna nyata ada tiga: **penyimpanan kredensial**,
**keamanan akun**, dan **kualitas retrieval RAG**. Ketiganya menjadi Fase 1.

---

## Fase 1 — Keamanan & kepercayaan

**Target selesai: Agustus 2026 · Wajib sebelum pengguna nyata**

Selama fase ini belum tuntas, aplikasi hanya boleh dipakai untuk demo dengan data contoh.

### P0-1 · Pindahkan dan rotasi kredensial
- **Masalah:** `appsettings.json` memuat kunci OpenAI, Tavily, Google Maps, dan connection string Azure Storage dalam teks biasa.
- **Pekerjaan:** cabut dan terbitkan ulang keempat kredensial; pindahkan ke user-secrets (dev) dan environment variable atau Key Vault (produksi); kosongkan nilainya di berkas konfigurasi.
- **Selesai bila:** `appsettings.json` tidak memuat satu pun rahasia, aplikasi tetap berjalan dengan konfigurasi dari luar berkas, dan kunci lama sudah tidak berlaku.
- **Perkiraan:** 0,5 hari · **Bergantung pada:** —

### P0-2 · Hash kata sandi yang layak
- **Masalah:** `AuthHelpers.HashPassword` tanpa salt dan tanpa key stretching.
- **Pekerjaan:** ganti ke `PasswordHasher<T>` (PBKDF2) atau Argon2id; sediakan verifikasi ganda agar sandi lama tetap bisa login lalu ditulis ulang dengan algoritma baru.
- **Selesai bila:** dua akun dengan sandi sama menghasilkan hash berbeda, dan seluruh akun contoh masih bisa login tanpa reset manual.
- **Perkiraan:** 1 hari · **Bergantung pada:** —

### P0-3 · Peran berbasis database
- **Masalah:** `IsAdmin()` membandingkan email dengan string `admin@virtualdoctor.com`; cek yang sama diulang di endpoint ekspor.
- **Pekerjaan:** tabel `UserRole`, peran dimuat sebagai claim saat login, ganti seluruh pemeriksaan menjadi `[Authorize(Roles = "Admin")]`, tambah UI pemberian dan pencabutan peran.
- **Selesai bila:** tidak ada lagi perbandingan email untuk otorisasi di seluruh basis kode, dan peran dapat diubah lewat UI tanpa menyentuh kode.
- **Perkiraan:** 2 hari · **Bergantung pada:** —

### P0-4 · Embedding RAG sungguhan
- **Masalah:** `SimpleEmbeddingGenerator` menghasilkan vektor dari hash byte, sehingga hasil pencarian tidak bermakna secara semantik.
- **Pekerjaan:** ganti dengan model embedding nyata (`text-embedding-3-small`, atau `nomic-embed-text` via Ollama untuk on-premise); buat perintah indeks ulang karena dimensi vektor berubah.
- **Selesai bila:** pertanyaan uji mengembalikan potongan dokumen yang memang relevan, dan hasilnya dapat diperiksa ulang lewat catatan skor kemiripan.
- **Perkiraan:** 1,5 hari · **Bergantung pada:** P0-1 (kunci embedding)

### P0-5 · Kegagalan AI harus terlihat
- **Masalah:** semua exception Semantic Kernel ditelan lalu dijawab pencocokan kata kunci lokal, sehingga kunci salah atau layanan mati tidak terdeteksi.
- **Pekerjaan:** tetap berikan jawaban cadangan ke pasien, tetapi naikkan level log menjadi `Error`, tambahkan health check `/health/ai`, dan tampilkan indikator status di dashboard admin.
- **Selesai bila:** mematikan kunci API membuat indikator admin berubah merah dalam satu menit, sementara pasien tetap mendapat jawaban cadangan.
- **Perkiraan:** 1 hari · **Bergantung pada:** —

---

## Fase 2 — Melengkapi janji fitur

**Target selesai: September 2026**

Bagian requirements yang secara harfiah belum tercapai.

### P1-1 · Kernel function yang benar-benar bertransaksi
- **Masalah:** `scheduleDoctor`, `askDoctor`, dan `orderMedicine` hanya mengembalikan teks statis.
- **Pekerjaan:** `scheduleDoctor` membaca `DoctorSchedule`, menawarkan slot, dan membuat `Appointment`; `askDoctor` membuat `Consultation` berstatus Waiting; `orderMedicine` membuat keranjang atau `Order` draft. Semuanya meminta konfirmasi pengguna sebelum menulis data.
- **Selesai bila:** pasien dapat menyelesaikan pemesanan obat dan pembuatan janji temu **dari dalam AI Chat**, lalu hasilnya muncul di halaman Farmasi dan Janji Temu.
- **Perkiraan:** 3 hari · **Bergantung pada:** —

### P1-2 · Pakai `Microsoft.Extensions.VectorData`
- **Masalah:** paket direferensikan tetapi tidak dipakai; `VectorStoreService` memanggil Qdrant/Chroma/Azure lewat HTTP mentah (±700 baris).
- **Pekerjaan:** migrasi ke abstraksi resmi, satu konektor per provider, hapus kode HTTP buatan sendiri.
- **Selesai bila:** keempat provider vektor lulus rangkaian uji yang sama, dan kode HTTP manual sudah dihapus.
- **Perkiraan:** 2,5 hari · **Bergantung pada:** P0-4

### P1-3 · Alur farmasi lengkap
- **Masalah:** pemilihan apotek, verifikasi asuransi, dan pengurangan stok tidak ada di UI checkout.
- **Pekerjaan:** pilih apotek terdekat berdasarkan lokasi, verifikasi polis sebelum bayar, kurangi stok saat pesanan dibuat, kembalikan stok saat dibatalkan.
- **Selesai bila:** stok obat berubah sesuai pesanan, dan pesanan berasuransi menampilkan potongan biaya yang benar.
- **Perkiraan:** 2 hari · **Bergantung pada:** —

### P1-4 · EF Migrations menggantikan EnsureCreated
- **Masalah:** perubahan model tidak diterapkan ke database yang sudah ada; saat ini ditambal `SchemaUpgrader` khusus SQLite.
- **Pekerjaan:** buat migrasi awal dari skema saat ini, panggil `Database.MigrateAsync()` saat start, hapus `SchemaUpgrader`.
- **Selesai bila:** menambah satu kolom model dan menjalankan ulang aplikasi memperbarui database pada keempat provider tanpa kehilangan data.
- **Perkiraan:** 1 hari · **Bergantung pada:** —

### P1-6 · Pembayaran & penagihan — **Selesai 28 Juli 2026**
- **Masalah:** transaksi dibuat tanpa tagihan; `Order.PaymentStatus` diisi manual dan tidak ada bukti bayar.
- **Pekerjaan:** entitas `Payment`, empat penyedia (Manual, QRIS mandiri, Midtrans, Xendit), halaman bayar dengan QR dinamis, unggah bukti, verifikasi petugas, cetak invoice dan kuitansi, laporan keuangan di dashboard.
- **Selesai bila:** pasien dapat menyelesaikan pembayaran dari checkout sampai kuitansi tanpa campur tangan pengembang, dan admin melihat kas masuk beserta piutang.
- **Realisasi:** lihat [Progress.md](Progress.md). Verifikasi: QRIS dinamis dari tagihan sungguhan diperiksa ulang (tag `01=12`, tag `54=32000`, CRC cocok).

### P1-7 · Pengerasan webhook penyedia bayar — **Selesai 28 Juli 2026**
- **Masalah:** endpoint webhook Midtrans dan Xendit sudah ada dan memeriksa tanda tangan, tetapi belum idempoten terhadap pengiriman ulang beruntun, dan belum punya antrean ulang bila pemrosesan gagal.
- **Pekerjaan:** seluruh pemeriksaan dipindahkan ke `PaymentWebhookService`, setiap kiriman dicatat di tabel `PaymentWebhookEvents`, pengiriman ulang dikenali dari sidik jari isinya, status tidak boleh mundur, notifikasi lebih tua dari 7 hari ditolak, dan `/admin/webhooks` menyediakan tombol proses ulang.
- **Selesai bila:** notifikasi yang sama dikirim lima kali hanya mengubah status satu kali, dan setiap webhook yang gagal terlihat di UI admin beserta alasannya.
- **Realisasi:** lihat [Progress.md](Progress.md). Verifikasi: enam skenario dijalankan terhadap aplikasi yang berjalan — lima kiriman identik menghasilkan satu perubahan status dan empat kiriman ulang.
- **Sisa:** uji terhadap sandbox Midtrans/Xendit sungguhan belum dilakukan; yang diuji adalah notifikasi bertanda tangan sah yang dibentuk sendiri.

### P1-8 · Nomor invoice aman dari balapan — **Selesai 28 Juli 2026**
- **Masalah:** `PaymentService.NextInvoiceNumberAsync` membaca nomor terakhir lalu menambah satu; dua permintaan bersamaan dapat menghasilkan nomor yang sama dan salah satunya gagal di indeks unik.
- **Pekerjaan:** tabel `InvoiceCounters` per awalan bulan, dinaikkan lewat satu pernyataan `UPDATE` di dalam transaksi sehingga barisnya terkunci sampai commit.
- **Selesai bila:** 200 tagihan dibuat serentak menghasilkan 200 nomor unik dan berurutan.
- **Realisasi:** lihat [Progress.md](Progress.md). Verifikasi: 200 alokasi serentak menghasilkan nomor 1–200 tanpa celah, dan 40 checkout serentak menghasilkan 40 invoice unik tanpa kegagalan.

### P1-5 · Integrasi asuransi: nyata atau berlabel jelas
- **Masalah:** `InsuranceService` memakai daftar provider dan rasio coverage yang ditulis di kode.
- **Pekerjaan:** pilih satu — integrasi ke sistem asuransi sungguhan, atau beri label "simulasi" di UI dan dokumen komersial.
- **Selesai bila:** tidak ada lagi angka coverage simulasi yang tampil tanpa keterangan.
- **Perkiraan:** 0,5 hari (label) / 5+ hari (integrasi) · **Bergantung pada:** keputusan bisnis

---

## Fase 3 — Skala & operasional

**Target selesai: Oktober 2026**

### P2-1 · Streaming AI yang sesungguhnya
`SendStreamingMessageAsync` menunggu jawaban penuh lalu memuntahkannya per kata dengan jeda 25 ms.
Ganti ke `GetStreamingChatMessageContentsAsync`.
**Selesai bila:** token pertama tampil di bawah satu detik pada model yang mendukung streaming. · 1 hari

### P2-2 · Perbaiki cache kernel
`LlmProviderFactory` memakai `Dictionary` biasa pada singleton (tidak aman untuk banyak thread), dan parameter `temperature` pada `GetKernel` tidak pernah dipakai.
**Selesai bila:** `ConcurrentDictionary` dipakai dan parameter yang tidak berguna dihapus dari tanda tangan metode. · 0,5 hari

### P2-3 · Filter dan paging di sisi database
Halaman admin dan `DashboardService` memuat seluruh tabel ke memori.
**Selesai bila:** dengan 100.000 baris, halaman admin tetap terbuka di bawah dua detik. · 2 hari

### P2-4 · Realtime tanpa polling
Dashboard menyegarkan diri lewat timer 30 detik. Tambahkan hub statistik yang mengirim pembaruan saat ada transaksi baru.
**Selesai bila:** transaksi baru muncul di dashboard tanpa menunggu siklus timer. · 1,5 hari

### P2-5 · Jejak audit
Perubahan data pasien, pesanan, dan jadwal tidak tercatat pelakunya.
**Selesai bila:** setiap operasi tulis pada entitas sensitif tercatat beserta aktor, entitas, aksi, dan waktu, serta dapat ditelusuri dari UI admin. · 2 hari

### P2-7 · Rekonsiliasi, pengembalian dana, dan pelunasan
`PaymentState.Refunded` kini hanya bisa datang dari notifikasi penyedia (P1-7); petugas belum
dapat menerbitkan pengembalian dana sendiri. Belum ada pula pencocokan antara catatan aplikasi
dengan mutasi rekening atau laporan settlement penyedia.
**Selesai bila:** petugas dapat menerbitkan pengembalian dana beserta alasannya, dan laporan
harian menunjukkan selisih antara kas tercatat dan kas yang benar-benar diterima. · 2 hari

### P2-6 · Moderasi ulasan
Ulasan dokter tampil apa adanya.
**Selesai bila:** admin dapat menyembunyikan ulasan tidak pantas dan perubahannya tercatat di jejak audit. · 1 hari

---

## Fase 4 — Kualitas & kesiapan rilis

**Target selesai: November 2026**

### P3-1 · Proyek pengujian
Belum ada satu pun test. Mulai dari yang paling berisiko: perhitungan `DashboardService`,
konversi tipe `SettingsService`, dan pembuatan meeting `MeetingService` dengan HTTP handler tiruan.
**Selesai bila:** `dotnet test` berjalan di CI dan menutup jalur kritis di ketiga service tersebut. · 3 hari

### P3-2 · Integrasi berkelanjutan
Build, test, dan analisis statis berjalan otomatis pada setiap perubahan.
**Selesai bila:** perubahan yang gagal build atau test tidak dapat digabungkan. · 1 hari

### P3-3 · Observability
Log terstruktur, health check untuk database, storage, AI, dan vector store, serta metrik dasar.
**Selesai bila:** satu halaman status menunjukkan kesehatan seluruh dependensi. · 1,5 hari

### P3-4 · Dokumentasi operasional
Panduan pemulihan bencana, prosedur rotasi kunci, dan runbook insiden.
**Selesai bila:** orang baru dapat memulihkan layanan hanya dengan membaca dokumen. · 1 hari

---

## Ringkasan waktu

| Fase | Fokus | Pekerjaan | Perkiraan | Target |
|---|---|---|---|---|
| 1 | Keamanan & kepercayaan | 5 | ~6 hari | Agustus 2026 |
| 2 | Melengkapi fitur | 5 sisa (P1-6, P1-7, P1-8 selesai) | ~9 hari | September 2026 |
| 3 | Skala & operasional | 7 | ~10 hari | Oktober 2026 |
| 4 | Kualitas & rilis | 4 | ~6,5 hari | November 2026 |

Perkiraan di atas adalah waktu pengerjaan bersih untuk satu pengembang, belum termasuk
peninjauan, pengujian manual, dan koordinasi.

---

## Di luar cakupan saat ini

Dicatat agar tidak berulang kali dibahas:

- Aplikasi mobile native — web sudah responsif; native ditinjau ulang setelah Fase 3.
- Multi-bahasa — antarmuka saat ini hanya bahasa Indonesia.
- Multi-tenant — arsitektur sekarang satu instansi untuk satu institusi.
