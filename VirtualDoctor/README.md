# 🏥 VirtualDoctor - Platform Kesehatan Digital

Platform kesehatan digital berbasis **.NET Blazor Server** dengan dukungan **AI Chat multi-LLM** dan integrasi **RAG (Retrieval Augmented Generation)**.

> Gabungan kekuatan Halodoc + Alodokter + AI Chat multi-LLM dengan dashboard analitik modern!

---

## 🚀 Fitur Utama

### 👨‍⚕️ Konsultasi Dokter Online
- Chat real-time dengan dokter umum, spesialis, dan psikolog
- **Video call opsional** lewat Jitsi (tanpa kredensial), Zoom (Server-to-Server OAuth), atau Microsoft Teams (Graph API) — dipilih di `appsettings.json` maupun halaman Pengaturan Sistem
- Riwayat konsultasi lengkap + ulasan dan rating dokter

### 💊 Pembelian Obat & Vitamin
- Katalog obat lengkap (obat bebas, obat keras, vitamin, suplemen)
- Keranjang belanja & checkout
- Pilih apotek terdekat, integrasi kurir
- Dukungan pembayaran via asuransi

### 🏠 Homecare Services
- Tes lab di rumah (darah, kolesterol, gula darah)
- Vaksinasi (influenza, COVID-19)
- Vitamin booster
- Panggil dokter ke rumah
- Kunjungan perawat

### 📅 Booking Rumah Sakit/Klinik
- Buat janji konsultasi tatap muka atau online
- Estimasi biaya transparan
- Manajemen jadwal dokter

### 🤖 AI Chat Pasien (Multi-LLM)
- **OpenAI** (GPT-4o)
- **Google Gemini** (gemini-2.0-flash)
- **Anthropic Claude** (claude-3-5-sonnet)
- **Ollama** (llama3.1, lokal)
- **OpenAI-Compatible** (custom endpoint)
- Upload gambar (ImageContent via storage URL)
- Upload dokumen (PDF/DOC via storage URL)
- 11 Kernel Functions (tools): searchInternet, checkDate, mathCalc, readFileFromUrl, describeImage, scrapWebPage, askDoctor, orderMedicine, scheduleDoctor, findHospital, queryHealthDocs

### 📚 Artikel Kesehatan + RAG
- Artikel dari PDF yang di-index ke vector database
- Query artikel via AI Chat (RAG-powered)
- PDF auto-indexing worker service

### 🗺️ Lokasi Fasilitas Kesehatan
- Cari RS, klinik, puskesmas terdekat
- Integrasi Google Maps API

### 💳 Pembayaran & Penagihan
- Empat penyedia: transfer manual, QRIS mandiri, Midtrans, dan Xendit — dipilih dari konfigurasi atau UI
- QRIS dinamis dibentuk sendiri dari QR statis merchant: nominal disisipkan dan checksum dihitung ulang
- Halaman bayar dengan hitung mundur, unggah bukti transfer, dan cek status ke penyedia
- Verifikasi petugas di `/admin/payments`, lengkap dengan tampilan bukti dan catatan pemeriksaan
- Cetak **invoice** dan **bukti pembayaran** siap A4 dengan terbilang otomatis
- Riwayat tagihan pasien di `/tagihan`
- Jejak notifikasi penyedia di `/admin/webhooks`: kiriman ulang dikenali, status tidak bisa mundur, kiriman gagal dapat diproses ulang
- Nomor invoice berurutan tanpa bentrok, walau banyak checkout terjadi bersamaan
- Transfer manual selalu tersedia sebagai cadangan bila penyedia sedang bermasalah

### 📊 Dashboard & Reporting
- Grafik D3.js interaktif: area bertumpuk dengan crosshair, donut komposisi, bar peringkat, heatmap hari × jam
- KPI dengan sparkline tren dan perbandingan periode sebelumnya
- Filter rentang tanggal, skala harian/mingguan/bulanan, spesialisasi, kota, dan kanal layanan
- Tabel performa dokter (sortir + cari) dan transaksi terbaru dengan ekspor CSV
- **Laporan keuangan**: kas masuk, tagihan terbit, tingkat penagihan, nilai rata-rata, piutang berjalan, grafik arus kas, komposisi cara bayar, umur piutang, ringkasan pembukuan, dan buku tagihan
- Perbarui otomatis setiap 30 detik (dapat dimatikan)

### 🛠️ Backoffice
- Master data: pengguna, dokter, RS/klinik, obat, artikel, janji temu, jadwal, homecare
- Kelola pesanan obat: status, pembayaran, kurir, nomor resi
- Verifikasi pembayaran: antrean bukti transfer, setujui/tolak, tandai kedaluwarsa
- Pantau konsultasi berjalan beserta tautan video
- Pengaturan sistem: provider AI, kredensial, video call, pembayaran, integrasi, dan RAG — tersimpan di database

### 🔐 Autentikasi
- Register user, login, reset password
- User profile management

---

## ⚙️ Arsitektur Sistem

```
┌──────────────────────────────────────────────────────┐
│                 Blazor Server UI                      │
│  (Interactive Server Components + SignalR)           │
├──────────────────────────────────────────────────────┤
│  Service Layer                                       │
│  ┌──────────┬──────────┬──────────┬──────────────┐  │
│  │ AI Chat  │   RAG    │ Business │   Storage    │  │
│  │ Service  │ Service  │ Services │   Service    │  │
│  └──────────┴──────────┴──────────┴──────────────┘  │
├──────────────────────────────────────────────────────┤
│  Data Layer                                          │
│  ┌──────────┬──────────┬──────────┬──────────────┐  │
│  │  EF Core │ VectorDB │  Kernel  │   Workers    │  │
│  │ (SQLite) │(InMemory)│Functions │ (PDF Index)  │  │
│  └──────────┴──────────┴──────────┴──────────────┘  │
└──────────────────────────────────────────────────────┘
```

---

## 🔧 Konfigurasi LLM di `appsettings.json`

```json
{
  "Llm": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-your-key-here",
      "Model": "gpt-4o"
    },
    "Gemini": {
      "ApiKey": "your-gemini-key",
      "Model": "gemini-2.0-flash"
    },
    "Anthropic": {
      "ApiKey": "your-anthropic-key",
      "Model": "claude-3-5-sonnet-20241022"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "Model": "llama3.1"
    },
    "SystemPrompt": "Kamu adalah dokter Markonah Al-senyumwati...",
    "BotName": "dokter Markonah Al-senyumwati",
    "Temperature": 0.7,
    "MaxTokens": 4096
  }
}
```

---

## 🗄️ Setup Vector Database & Indexing PDF

1. Letakkan file PDF di folder `wwwroot/HealthPdfs/`
2. Worker akan auto-index setiap 30 menit (konfigurasi di `appsettings.json`)
3. Dokumen ter-index bisa ditanyakan via AI Chat (RAG)

```json
{
  "VectorDb": {
    "Provider": "InMemory",  // InMemory, SQLite, Qdrant, AzureAISearch
    "CollectionName": "health-docs"
  },
  "Indexing": {
    "PdfFolderPath": "HealthPdfs",
    "IntervalMinutes": 30,
    "AutoIndex": true
  }
}
```

---

## 🚀 Deployment Guide

### Prerequisites
- .NET 10 SDK
- SQLite (default) / SQL Server / PostgreSQL / MySQL

### Run Development
```bash
dotnet run
# Buka https://localhost:5001
```

### Database
- Default: SQLite (`VirtualDoctor.db`)
- Untuk production: ubah provider di `appsettings.json`

### Demo Account
- Email: `budi@email.com`
- Password: `Password123!`

---

## 📂 Struktur Project

```
VirtualDoctor/
├── Components/
│   ├── Layout/        # MainLayout, MinimalLayout
│   ├── Pages/         # Semua halaman Blazor
│   │   ├── Auth/      # Login, Register
│   │   ├── Dashboard/ # Dashboard & reporting
│   │   └── ...        # AI Chat, Doctors, dll.
│   └── Shared/        # Shared components
├── Data/              # DbContext & Seeder
├── Hubs/              # SignalR hubs
├── Models/            # Entity models
├── Services/
│   ├── AI/            # LLM, Chat, Kernel Functions
│   ├── Analytics/     # Dashboard & laporan keuangan
│   ├── Meeting/       # Jitsi, Zoom, Teams
│   ├── Payment/       # QRIS, Midtrans, Xendit, transfer manual, penomoran invoice, webhook
│   ├── RAG/           # Vector Store, Indexing, Query
│   └── Storage/       # File, Location, Search
├── Workers/           # Background services
├── wwwroot/           # Static files, CSS
├── docs/              # Dokumentasi
└── PLAN.md            # Development plan
```

---

## 📹 Konfigurasi Video Call (opsional)

```json
{
  "Meeting": {
    "Provider": "None",           // None | Jitsi | Zoom | Teams
    "DefaultDurationMinutes": 30,
    "Zoom":  { "AccountId": "", "ClientId": "", "ClientSecret": "", "HostUserId": "me" },
    "Teams": { "TenantId": "", "ClientId": "", "ClientSecret": "", "OrganizerUserId": "" },
    "Jitsi": { "Domain": "meet.jit.si", "RoomPrefix": "vdoctor" }
  }
}
```

- **Jitsi** — tanpa kredensial, langsung jalan. Untuk produksi pasang server Jitsi sendiri.
- **Zoom** — buat app *Server-to-Server OAuth* di Zoom Marketplace, aktifkan scope `meeting:write:admin`.
- **Teams** — daftarkan app di Entra ID dengan permission aplikasi `OnlineMeetings.ReadWrite.All` dan kebijakan akses aplikasi untuk user organizer.

Semua nilai di atas juga bisa diubah dari **Pengaturan Sistem** (`/admin/settings`) tanpa menyunting berkas, lengkap dengan tombol uji koneksi. Nilai dari UI disimpan ke tabel `AppSettings` dan menimpa `appsettings.json` saat aplikasi start.

---

## 💳 Konfigurasi Pembayaran

```json
{
  "Payment": {
    "Provider": "Manual",         // Manual | Qris | Midtrans | Xendit
    "Enabled": true,
    "ExpiryMinutes": 120,
    "ServiceFee": 0,
    "InvoicePrefix": "INV",
    "Merchant": { "Name": "", "LegalName": "", "Address": "", "Phone": "", "Email": "", "TaxId": "" },
    "Manual":   { "BankName": "", "AccountNumber": "", "AccountHolder": "", "Instructions": "" },
    "Qris":     { "StaticPayload": "", "MerchantName": "", "MerchantCity": "" },
    "Midtrans": { "ServerKey": "", "ClientKey": "", "IsProduction": false },
    "Xendit":   { "SecretKey": "", "CallbackToken": "" }
  }
}
```

- **Manual** — tanpa kredensial. Pasien transfer lalu mengunggah bukti, petugas memverifikasi di `/admin/payments`.
- **Qris** — tempel payload QR statis dari bank/PJSP Anda ke `StaticPayload`. Aplikasi mengubahnya menjadi QR dinamis bernominal dan menghitung ulang checksum-nya. Gunakan tombol **Uji payload QRIS** di Pengaturan Sistem untuk memastikan payload terbaca.
- **Midtrans** — Core API. Daftarkan webhook ke `https://domain-anda/api/payments/webhook/midtrans`.
- **Xendit** — API v2. Daftarkan callback ke `https://domain-anda/api/payments/webhook/xendit` dan isi `CallbackToken`.

Transfer manual selalu tetap ditawarkan sebagai cadangan, sehingga penyedia yang sedang
bermasalah tidak menghentikan transaksi. Seluruh nilai di atas juga dapat diubah dari tab
**Pembayaran** di `/admin/settings`.

Setiap notifikasi penyedia tercatat di `/admin/webhooks`, termasuk yang ditolak. Kiriman
dengan isi yang sama hanya mengubah status satu kali; sisanya dihitung sebagai kiriman ulang.
Kiriman yang gagal diproses dapat dijalankan ulang dari halaman itu tanpa meminta penyedia
mengirim lagi — kecuali kiriman yang tidak lolos pemeriksaan keaslian, yang tidak pernah
boleh diproses ulang.

> Payload QRIS pada data contoh adalah merchant fiktif untuk keperluan demo. Ganti dengan
> payload merchant Anda sebelum menerima pembayaran sungguhan.

---

## 📚 Dokumentasi Lain

| Berkas | Isi |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Arsitektur sistem |
| [docs/features.md](docs/features.md) | Detail fitur |
| [docs/deployment.md](docs/deployment.md) | Panduan deployment |
| [docs/feature-audit.md](docs/feature-audit.md) | Audit implementasi terhadap requirements |
| [docs/recommendations.md](docs/recommendations.md) | Rekomendasi penyempurnaan berikutnya |
| [PLAN.md](PLAN.md) | Roadmap pengembangan |
| [Progress.md](Progress.md) | Catatan realisasi pekerjaan |

---

© 2025 VirtualDoctor - GraviCode Studios
