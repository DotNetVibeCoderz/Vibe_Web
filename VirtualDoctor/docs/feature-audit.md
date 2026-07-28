# Audit Fitur terhadap `requirements.md`

Tanggal audit: 28 Juli 2026
Metode: penelusuran kode (halaman Razor, service, konfigurasi) lalu verifikasi dengan menjalankan aplikasi.

Legenda status:

| Simbol | Arti |
|---|---|
| ✅ | Terpenuhi |
| ⚠️ | Ada, tetapi belum sesuai maksud requirement |
| ❌ | Belum ada |

---

## 1. Fitur ala Halodoc

| Fitur | Status | Catatan |
|---|---|---|
| Konsultasi dokter online (chat) | ✅ | `Consultations.razor` + `ConsultationService`, riwayat tersimpan, ulasan & rating tersedia |
| Konsultasi telepon / video | ✅ | Ditambahkan pada iterasi ini: tipe Video membuat tautan meeting via Jitsi/Zoom/Teams. Tipe Phone masih label saja |
| Katalog & pemesanan obat | ✅ | `Pharmacy.razor`, keranjang, checkout, `OrderService` |
| Pilih apotek terdekat | ⚠️ | Kolom `Order.PharmacyId` ada dan diisi seeder, tetapi UI checkout belum menyediakan pemilihan apotek |
| Integrasi kurir | ⚠️ | Kolom `CourierName`/`TrackingNumber` ada dan bisa diisi admin di Kelola Pesanan; belum ada integrasi API kurir |
| Homecare (lab, vaksin, perawat, dokter ke rumah) | ✅ | `Homecare.razor` + `HomecareAppService` |
| Booking RS/klinik | ✅ | `Appointments.razor`, estimasi biaya, jadwal dokter |
| Cari RS/klinik/puskesmas | ✅ | `Hospitals.razor`, `LocationService` + Google Maps API |
| Artikel kesehatan | ✅ | `Articles.razor`, admin CRUD, penanda terindeks RAG |
| Integrasi asuransi | ⚠️ | `InsuranceService` mengembalikan nilai **simulasi statis** (daftar provider hardcoded, coverage dari tabel rasio tetap). Belum ada koneksi ke sistem asuransi mana pun |
| Pembayaran online | ✅ | **Ditambahkan** (P1-6): QRIS dinamis dibentuk sendiri dari QR statis merchant, plus Midtrans, Xendit, dan transfer manual dengan verifikasi petugas. Invoice dan kuitansi siap cetak. Notifikasi penyedia idempoten dan berjejak (P1-7) |
| Autentikasi & profil | ✅ | Cookie auth, register, login, reset, profil |

## 2. AI Chat multi-LLM

| Item | Status | Catatan |
|---|---|---|
| OpenAI, Gemini, Anthropic, Ollama, OpenAI-compatible | ✅ | `LlmProviderFactory`, semuanya lewat `AddOpenAIChatCompletion` dengan base URL berbeda |
| Konfigurasi lewat appsettings | ✅ | Bagian `Llm` di `appsettings.json` |
| Konfigurasi lewat UI | ✅ | **Ditambahkan**: `/admin/settings` menyimpan override ke tabel `AppSettings` dan menerapkannya saat aplikasi start |
| Jawab pertanyaan kesehatan & obat | ✅ | Semantic Kernel + 11 kernel function |
| Pesan obat lewat chat | ⚠️ | `orderMedicine` hanya **mencari** obat dan menampilkan harga, tidak membuat pesanan sungguhan |
| Buat/cek jadwal dokter lewat chat | ⚠️ | `scheduleDoctor` mengembalikan teks statis ("Kunjungi menu Booking"), tidak membaca jadwal maupun membuat janji |
| Rujuk ke dokter | ⚠️ | `askDoctor` mengembalikan teks statis, tidak membuat konsultasi |
| Upload gambar & dokumen | ✅ | Tersimpan ke storage lalu dikirim sebagai `ImageContent` |
| Semantic Kernel | ✅ | v1.77 |

> **Temuan penting:** `AiChatService.GetAiResponse` menangkap **semua** exception dari Semantic Kernel lalu jatuh ke `GenerateLocalResponse`, yaitu pencocokan kata kunci bahasa Indonesia yang ditulis manual. Akibatnya API key yang salah, model yang mati, atau jaringan putus **tidak terlihat sebagai error** — pengguna tetap menerima jawaban yang terdengar wajar. Satu-satunya jejak adalah log peringatan `[AI] SK failed, using local fallback`.

## 3. RAG & Vector Database

| Item | Status | Catatan |
|---|---|---|
| InMemory, SQLite, Qdrant, Azure AI Search | ✅ | Plus Chroma. Dipilih lewat `VectorDb:Provider` |
| Pakai `Microsoft.Extensions.VectorData` | ❌ | Paket direferensikan di `.csproj` tetapi **tidak dipakai sama sekali**. `VectorStoreService` memakai `IVectorProvider` buatan sendiri dengan HTTP mentah |
| Index PDF otomatis | ✅ | `PdfIndexingWorker`, interval dapat diatur |
| Query artikel via AI | ✅ | `RagQueryService`, `queryHealthDocs` |
| Kualitas embedding | ❌ | `SimpleEmbeddingGenerator` (kelas `file` di dasar `Program.cs`) menghasilkan vektor 256 dimensi dari **hash byte**, bukan model embedding. Pipeline berjalan penuh, tetapi hasil pencarian kemiripan tidak bermakna secara semantik |

## 4. Platform & infrastruktur

| Item | Status | Catatan |
|---|---|---|
| Blazor Server .NET | ✅ | .NET 10, Interactive Server |
| Database SQLite/SqlServer/MySQL/PostgreSQL | ✅ | Dipilih di `Database:Provider` |
| Storage FileSystem/MinIO/S3/Azure Blob | ✅ | `StorageServiceFactory`. **Diperbaiki**: kegagalan storage eksternal tidak lagi menjatuhkan aplikasi |
| Migrasi database | ⚠️ | Memakai `EnsureCreated`, bukan EF Migrations. Perubahan model pada database lama ditambal `SchemaUpgrader` (khusus SQLite) |
| README & dokumentasi | ✅ | `README.md`, `docs/architecture.md`, `docs/features.md`, `docs/deployment.md` |

## 5. Tampilan & UX

| Item | Status | Catatan |
|---|---|---|
| Modern & konsisten | ✅ | Design system `vd-*` dengan token warna, tipografi Cormorant + Mulish + IBM Plex Mono |
| Responsive | ✅ | **Diperbaiki**: sebelumnya di layar <992px sidebar menutupi konten secara bawaan. Sekarang drawer tertutup dengan tombol menu dan lapisan gelap |
| Light & dark theme | ✅ | **Diperbaiki**: pilihan tema kini tersimpan di localStorage, mengikuti preferensi sistem, dan diterapkan sebelum render pertama |
| Aksesibilitas | ⚠️ | **Diperbaiki sebagian**: cincin fokus, skip link, label ARIA, dukungan `prefers-reduced-motion`. Belum ada audit kontras menyeluruh dan uji pembaca layar |
| State memuat & kosong | ✅ | **Ditambahkan**: skeleton dan empty state pada halaman baru |

## 6. Dashboard & laporan

| Item | Status | Catatan |
|---|---|---|
| Data real | ✅ | **Diganti total**: sebelumnya seluruh angka dan tabel **hardcoded di markup** (10 dokter, 12 obat, dst.) tanpa query database sama sekali |
| Chart interaktif | ✅ | **Ditambahkan**: D3 v7 (dibundel lokal, bukan CDN) — area bertumpuk dengan crosshair, donut, bar peringkat, heatmap hari×jam, sparkline KPI |
| Filter advance | ✅ | Rentang preset & kustom, skala harian/mingguan/bulanan, spesialisasi, kota, kanal |
| Tabel data | ✅ | Performa dokter (sortir + cari) dan transaksi terbaru (filter jenis, ekspor CSV) |
| Laporan keuangan | ✅ | **Ditambahkan** (P1-6): dihitung dari tabel `Payments`, jadi yang dilaporkan adalah uang yang benar-benar tertagih — kas masuk, tingkat penagihan, umur piutang, komposisi cara bayar, buku tagihan |
| Realtime | ⚠️ | Perbarui otomatis tiap 30 detik yang bisa dinyalakan/dimatikan. Belum push realtime lewat SignalR |

## 7. Backoffice

| Modul | Status | Catatan |
|---|---|---|
| Pengguna, Dokter, RS/Klinik, Obat, Artikel, Janji Temu, Jadwal, Homecare | ✅ | CRUD + filter + paging + sortir + ekspor CSV/Excel |
| Pesanan obat | ✅ | **Ditambahkan** `/admin/orders`: ubah status, status bayar, kurir, resi |
| Pemantauan konsultasi | ✅ | **Ditambahkan** `/admin/consultations`: status, durasi, tautan video |
| Pengaturan sistem | ✅ | **Ditambahkan** `/admin/settings`: LLM, video call, pembayaran, integrasi, RAG |
| Verifikasi pembayaran | ✅ | **Ditambahkan** (P1-6) `/admin/payments`: antrean bukti transfer, setujui/tolak beserta catatan, tandai kedaluwarsa, cetak kuitansi |
| Jejak webhook penyedia bayar | ✅ | **Ditambahkan** (P1-7) `/admin/webhooks`: setiap notifikasi tercatat beserta alasannya, kiriman ulang dikenali dari sidik jari isinya, tombol proses ulang untuk kiriman yang gagal |
| Ulasan dokter | ❌ | `DoctorReview` ada dan tampil di dashboard, tetapi belum ada halaman moderasi |
| Manajemen peran | ✅ | **Selesai 28 Juli 2026** (P0-3): tabel `UserRoles`, peran dibentuk menjadi claim saat login, UI pemberian dan pencabutan di `/admin/users` |
| Jejak audit | ❌ | Perubahan data tidak dicatat siapa dan kapan (kecuali tabel `AppSettings`) |

---

## Ringkasan angka

| Kategori | Terpenuhi | Sebagian | Belum ada |
|---|---|---|---|
| Fitur Halodoc | 9 | 3 | 0 |
| AI Chat | 6 | 3 | 0 |
| RAG | 3 | 0 | 2 |
| Infrastruktur | 4 | 1 | 0 |
| Tampilan | 4 | 1 | 0 |
| Dashboard | 5 | 1 | 0 |
| Backoffice | 7 | 0 | 2 |

Rekomendasi tindak lanjut ada di [recommendations.md](recommendations.md).
