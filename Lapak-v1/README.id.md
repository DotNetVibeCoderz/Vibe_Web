# 🛍️ Lapak - Platform E-Commerce Modern

> Platform e-commerce bertenaga AI dibangun dengan .NET Blazor Server

## ✨ Fitur

### 🛒 E-Commerce Inti
- **Manajemen Produk**: CRUD produk, kategori, sub-kategori, atribut, stok, harga, promo, komentar, like & rating
- **Manajemen Toko**: Registrasi, profil, verifikasi, rating, komentar & like
- **Fitur Pembeli**: Registrasi, profil, wishlist, keranjang belanja, checkout
- **Transaksi & Pembayaran**: Integrasi payment gateway Midtrans & Xendit
- **Pengiriman & Logistik**: Integrasi kurir JNE, J&T, SiCepat, Pos Indonesia dengan tracking real-time
- **Promo & Voucher**: Diskon, cashback, loyalty points

### 🤖 Fitur AI
- **Tony Kurus - Asisten Belanja**: Chatbot AI untuk pencarian produk/toko, rekomendasi, dan bantuan belanja
- **Siti Bohay - Customer Support**: Chatbot AI dengan RAG untuk dokumen kebijakan, handover ke WhatsApp/Email
- **Dukungan Multi-LLM**: OpenAI, Gemini, Anthropic, Ollama, dan yang kompatibel dengan OpenAI
- **Fallback Otomatis**: Jika satu LLM gagal, otomatis dialihkan ke provider lain
- **Riwayat Chat**: Tersimpan per pengguna
- **Upload File**: Dukungan gambar dan dokumen dalam chat

### 🧠 AI Recommendation Engine
- Collaborative filtering berdasarkan pembelian pengguna serupa
- Rekomendasi content-based menggunakan kategori dan atribut
- Saran real-time di halaman produk dan checkout
- Rekomendasi personal berdasarkan profil pengguna

### 📊 Customer Scoring
- Skor berdasarkan jumlah transaksi, nilai transaksi, dan keragaman kategori
- Segmentasi pelanggan: Bronze, Silver, Gold, Platinum
- Promo tertarget berdasarkan tier pelanggan

### 📈 Dashboard & Pelaporan
- Desain modern, responsif dengan tema light/dark
- Grafik dan statistik interaktif
- Filter lanjutan (tanggal, kategori, toko, nilai transaksi)
- Data tabular dengan dukungan export
- Update real-time dengan SignalR

### 💾 Database & Storage
- **Database**: SQLite, SQL Server, MySQL, PostgreSQL
- **Storage**: File System, MinIO, Amazon S3, Azure Blob
- Konfigurasi fleksibel melalui `appsettings.json`

## 🚀 Mulai Cepat

### Prasyarat
- .NET 8.0 SDK atau lebih baru
- SQLite (default) atau database lain yang didukung

### Instalasi

```bash
# Clone repository
git clone https://github.com/yourusername/lapak.git
cd lapak

# Jalankan aplikasi
dotnet run
```

Aplikasi akan tersedia di `https://localhost:5001`

### Konfigurasi

Edit `appsettings.json` untuk mengkonfigurasi:

- **Database**: Ubah `DatabaseProvider` ke `SQLite`, `SqlServer`, `MySql`, atau `PostgreSql`
- **AI Providers**: Tambahkan API key di `AI.Providers`
- **Payment Gateways**: Konfigurasi kunci Midtrans/Xendit
- **Pengiriman**: Atur API key RajaOngkir
- **Storage**: Konfigurasi MinIO, S3, atau Azure Blob

## 🏗️ Arsitektur

```
Lapak/
├── Components/        # Komponen UI Blazor
│   ├── Layout/        # Layout utama, sidebar, navbar
│   ├── Pages/         # Halaman aplikasi
│   │   ├── Account/   # Login, register, profil
│   │   ├── Chat/      # Chat Tony Kurus & Siti Bohay
│   │   ├── Dashboard/ # Dashboard analitik
│   │   └── Products/  # Daftar & detail produk
│   └── Shared/        # Komponen reusable
├── Data/              # EF Core DbContext & data awal
├── Hubs/              # SignalR hubs
├── Models/            # Model entity & konfigurasi
├── Services/          # Layanan logika bisnis
│   ├── AI/            # Abstraksi layanan LLM
│   ├── Payment/       # Layanan payment gateway
│   ├── Shipping/      # Layanan kurir & pengiriman
│   └── Storage/       # Abstraksi penyimpanan file
└── wwwroot/           # Aset statis & CSS
```

## 📚 Dokumentasi

- [Arsitektur](docs/architecture.md)
- [Konfigurasi AI](docs/ai-config.md)
- [Dashboard & Pelaporan](docs/dashboard.md)
- [Setup Storage](docs/storage.md)
- [Setup Database](docs/database.md)

## 🛠️ Tech Stack

- **Framework**: .NET 8.0 Blazor Server
- **ORM**: Entity Framework Core
- **Real-time**: SignalR
- **AI/LLM**: Multi-provider abstraction (OpenAI, Gemini, Anthropic, Ollama)
- **Charts**: ChartJs.Blazor.Fork
- **Storage**: MinIO SDK, S3 SDK
- **Resilience**: Polly

## 📄 Lisensi

MIT License - lihat file LICENSE untuk detail

---

Dibuat dengan ❤️ oleh Jacky The Code Bender @ Gravicode Studios
