# 🏥 VirtualDoctor - Platform Kesehatan Digital

Platform kesehatan digital berbasis **.NET Blazor Server** dengan dukungan **AI Chat multi-LLM** dan integrasi **RAG (Retrieval Augmented Generation)**.

> Gabungan kekuatan Halodoc + Alodokter + AI Chat multi-LLM dengan dashboard analitik modern!

---

## 🚀 Fitur Utama

### 👨‍⚕️ Konsultasi Dokter Online
- Chat real-time dengan dokter umum, spesialis, dan psikolog
- Dukungan telepon & video call (WebRTC-ready)
- Riwayat konsultasi lengkap

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

### 📊 Dashboard & Reporting
- Statistik interaktif
- Chart data dokter, obat, appointment
- Filter advance

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
│   ├── RAG/           # Vector Store, Indexing, Query
│   └── Storage/       # File, Location, Search
├── Workers/           # Background services
├── wwwroot/           # Static files, CSS
├── docs/              # Dokumentasi
└── PLAN.md            # Development plan
```

---

© 2025 VirtualDoctor - GraviCode Studios
