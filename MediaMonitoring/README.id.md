# Sistem Media Monitoring OSINT

![Versi](https://img.shields.io/badge/versi-1.0.0-blue)
![Framework](https://img.shields.io/badge/framework-.NET%208-purple)
![UI](https://img.shields.io/badge/UI-Blazor%20Server-green)

## 📋 Ringkasan

**Media Monitoring** adalah aplikasi Open Source Intelligence (OSINT) komprehensif yang dibangun dengan Blazor Server. Aplikasi ini menyediakan pemantauan, analisis, dan visualisasi konten media secara real-time dari berbagai sumber termasuk media sosial, portal berita, blog, forum, dan website resmi.

Sistem ini menampilkan desain Brutalist modern, visualisasi interaktif D3.js, dan kemampuan pemrosesan data cerdas.

---

## ✨ Fitur Utama

### 🔍 Pengumpulan Data
- **Crawling Multi-Sumber**: Simulasi crawling dari Twitter, Facebook, situs Berita, Blog, Reddit, YouTube, dan lainnya
- **Dukungan Multi-Format**: Penanganan teks, gambar, metadata video
- **Siap Integrasi API**: Arsitektur siap untuk integrasi Twitter API, Facebook Graph API, YouTube Data API
- **Pemrosesan Real-time**: Ingesti dan normalisasi data otomatis

### ⚙️ Pemrosesan Data
- **Normalisasi Data**: Pembersihan noise, penghapusan duplikasi
- **Dukungan Multi-bahasa**: Termasuk bahasa informal dan bahasa campuran (misalnya Arabizi)
- **Kategorisasi Otomatis**: Politik, Ekonomi, Keamanan, Teknologi, Sosial, Bencana, Kesehatan
- **Ekstraksi Metadata**: Pelacakan penulis, lokasi, timestamp, sumber

### 🧠 Analisis & Intelijen
- **Analisis Sentimen**: Klasifikasi Positif, Negatif, Netral dengan skor (-1.0 hingga 1.0)
- **Analisis Tren**: Deteksi topik trending secara real-time
- **Pemantauan Kata Kunci**: Lacat kata kunci spesifik secara real-time
- **Analisis Jaringan**: Pemetaan hubungan antar akun dan sumber
- **Analisis Geospasial**: Pelacakan informasi berbasis lokasi

### 📊 Visualisasi
- **Dashboard Interaktif**: Dibangun dengan D3.js untuk grafik dinamis
- **Grafik Tren**: Analisis time-series dengan diagram garis
- **Distribusi Kategori**: Diagram batang untuk kategorisasi topik
- **Word Cloud**: Visualisasi kata kunci populer (dapat dikembangkan)
- **Grafik Jaringan**: Pemetaan hubungan (dapat dikembangkan)

### 🚨 Alert & Pelaporan
- **Notifikasi Real-time**: Alert instan untuk kata kunci kritis
- **Aturan yang Dapat Disesuaikan**: Atur tingkat keparahan (Rendah, Sedang, Tinggi, Kritis)
- **Laporan Otomatis**: Ringkasan harian, mingguan, bulanan (dapat dikembangkan)
- **Kemampuan Ekspor**: Arsitektur siap ekspor PDF/Excel

### 🔐 Kolaborasi & Keamanan
- **Otentikasi Terkonfigurasi**: Siap untuk kontrol akses berbasis peran
- **Audit Trail**: Kemampuan logging aktivitas pengguna
- **Penyimpanan Aman**: Database SQLite lokal dengan dukungan enkripsi
- **Manajemen Konfigurasi**: Pengaturan dinamis via UI (API key, endpoint, dll)

---

## 🛠️ Stack Teknologi

- **Backend**: .NET 8, ASP.NET Core Blazor Server
- **Database**: SQLite dengan Entity Framework Core
- **Frontend**: Komponen Razor, D3.js v7
- **Desain**: Framework CSS Brutalist kustom
- **Manajemen Paket**: NuGet

---

## 🚀 Memulai

### Prasyarat
- .NET 8 SDK atau lebih baru
- Visual Studio 2022 / VS Code / Rider
- Browser modern (Chrome, Firefox, Edge)

### Langkah Instalasi

1. **Clone atau Download Proyek**
   ```bash
   cd MediaMonitoring
   ```

2. **Restore Dependensi**
   ```bash
   dotnet restore
   ```

3. **Build Proyek**
   ```bash
   dotnet build
   ```

4. **Jalankan Aplikasi**
   ```bash
   dotnet run
   ```

5. **Akses Aplikasi**
   - Buka browser dan navigasi ke: `https://localhost:5001` atau `http://localhost:5000`
   - URL exact akan ditampilkan di output console

---

## 📁 Struktur Proyek

```
MediaMonitoring/
├── Components/
│   ├── Layout/          # Layout utama dan navigasi
│   ├── Pages/           # Komponen halaman Razor
│   │   ├── Home.razor       # Dashboard dengan grafik
│   │   ├── Monitoring.razor # Feed data langsung
│   │   ├── Alerts.razor     # Manajemen alert
│   │   └── Settings.razor   # Konfigurasi sistem
│   └── App.razor        # Komponen root
├── Data/
│   └── MediaMonitoringContext.cs  # DbContext EF Core
├── Models/
│   ├── MediaPost.cs         # Entitas postingan media
│   ├── SystemConfiguration.cs # Entitas konfigurasi dinamis
│   └── AlertRule.cs         # Entitas aturan alert
├── Services/
│   ├── ConfigService.cs       # Manajemen konfigurasi
│   └── OsintEngineService.cs  # Engine crawling & analisis
├── wwwroot/
│   ├── css/
│   │   └── brutalist.css    # Desain Brutalist kustom
│   └── js/
│       └── charts.js        # Kode visualisasi D3.js
├── Program.cs             # Entry point aplikasi
└── README.md              # File ini
```

---

## 🎨 Filosofi Desain

Aplikasi menggunakan pendekatan desain **Brutalist Modern**:
- **Tipografi Tebal**: Font uppercase berat untuk header
- **Kontras Tinggi**: Hitam putih dengan aksen warna strategis
- **Bayangan Keras**: Border solid 2px dengan drop shadow tajam
- **Layout Fungsional**: Interface padat informasi berbasis grid
- **Estetika Raw**: Elemen struktural terbuka, dekorasi minimal

---

## ⚙️ Konfigurasi

Semua konfigurasi sistem dapat dikelola melalui halaman **Settings**:

- **API Keys**: Twitter, Facebook, YouTube (field placeholder siap integrasi)
- **Pengaturan Crawling**: Interval menit, maksimal posting per siklus
- **Toggle Fitur**: Monitoring dark web, prediksi AI
- **Pengaturan Notifikasi**: Email admin untuk alert

Konfigurasi disimpan di database SQLite dan dapat dimodifikasi tanpa restart aplikasi.

---

## 📊 Menggunakan Dashboard

1. **Setup Awal**: 
   - Navigasi ke `/settings` dan atur preferensi Anda
   - Tambahkan aturan alert untuk kata kunci yang ingin dipantau

2. **Jalankan Crawling**:
   - Buka Dashboard (`/`)
   - Klik **[ RUN CRAWLING ]** untuk mensimulasikan pengumpulan data
   - Lihat saat posting dianalisis dan dikategorikan secara otomatis

3. **Monitor Live Feed**:
   - Kunjungi `/monitoring` untuk melihat semua data terkumpul
   - Gunakan filter untuk mencari berdasarkan kata kunci, sentimen, atau kategori
   - Paginasi melalui hasil

4. **Lihat Alerts**:
   - Periksa `/alerts` untuk aturan alert yang terpicu
   - Tinjau pencocokan terbaru dan statistik

5. **Analisis Tren**:
   - Grafik D3.js interaktif menunjukkan tren sepanjang waktu
   - Distribusi kategori membantu mengidentifikasi topik dominan

---

## 🔮 Peningkatan Masa Depan (Roadmap)

- [ ] Integrasi API nyata (Twitter, Facebook, Instagram)
- [ ] NLP canggih dengan ML.NET untuk analisis sentimen lebih baik
- [ ] Integrasi monitoring dark web
- [ ] Otentikasi pengguna dan manajemen peran
- [ ] Pembuatan laporan PDF/Excel
- [ ] Integrasi notifikasi Email/Slack
- [ ] Visualisasi peta geospasial
- [ ] Analisis grafik jaringan
- [ ] Prediksi tren berbasis AI
- [ ] Dukungan multi-tenant

---

## 🤝 Kontribusi

Proyek ini dibuat oleh **Jacky the Code Bender** dari **Gravicode Studios**.

Kontribusi dipersilakan! Jangan ragu untuk mengirim pull request atau membuka issue untuk laporan bug dan permintaan fitur.

---

## 📄 Lisensi

Proyek ini adalah perangkat lunak proprietari yang dikembangkan oleh Gravicode Studios.
Hak cipta dilindungi undang-undang.

---

## ☕ Dukungan

Jika Anda menemukan proyek ini berguna, pertimbangkan untuk mendukung developer:

- **Traktir Pulsa/Kopi**: https://studios.gravicode.com/products/budax
- **Kontak**: Tim di Gravicode Studios yang dipimpin oleh Kang Fadhil

---

## 🙏 Penghargaan

- Dibangun dengan ❤️ menggunakan .NET 8 dan Blazor
- D3.js untuk visualisasi data yang powerful
- Terinspirasi oleh tool OSINT modern dan prinsip desain brutalist

---

*Terakhir Diperbarui: 2025*