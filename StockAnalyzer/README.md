# 📈 StockAnalyzer - Platform Analisa & Rekomendasi Saham

**StockAnalyzer** adalah aplikasi analisa saham berbasis **Blazor Server .NET** dengan dukungan **multi-LLM** (OpenAI, Gemini, Anthropic, Ollama, dll). Menampilkan data teknikal, fundamental, dan sentimen berita emiten dengan dashboard interaktif.

[🇬🇧 English](#english) | [🇮🇩 Bahasa Indonesia](#bahasa-indonesia)

---

## 🇬🇧 English

### Features
- **📊 Technical Analysis**: Price history, MA, RSI, MACD, Bollinger Bands, Stochastic, ATR, candlestick patterns
- **💰 Fundamental Analysis**: PER, PBV, DER, ROE, EPS, Cash Flow, financial ratio assessment
- **📰 Sentiment Analysis**: News scraping, sentiment scoring (positive/negative/neutral), sector clustering
- **🤖 Multi-LLM Review**: AI-powered stock review using OpenAI, Gemini, Anthropic, Ollama, or OpenAI-compatible APIs
- **⭐ Stock Recommendations**: Combined scoring (technical + fundamental + sentiment), buy/hold/sell signals
- **🎨 Modern UI**: Dark/Light theme inspired by OpenAI, responsive Bootstrap grid, interactive charts
- **⚙️ Flexible Configuration**: Multiple database backends (SQLite, SQL Server, MySQL), storage options (FileSystem, MinIO, S3, Azure Blob)

### Tech Stack
- **Framework**: Blazor Server .NET 10
- **ORM**: Entity Framework Core
- **Database**: SQLite (default), SQL Server, MySQL
- **UI**: Bootstrap, Custom CSS
- **Charts**: Ready for ChartJs.Blazor.Fork integration
- **LLM**: OpenAI, Gemini, Anthropic, Ollama, OpenAI-compatible

### Quick Start
```bash
# Clone or open the project
cd StockAnalyzer

# Run the application
dotnet run

# Open browser at https://localhost:5001
```

### Configuration
Edit `appsettings.json` to configure:
- Database provider and connection string
- LLM provider API keys and endpoints
- Storage provider
- Analysis weights

---

## 🇮🇩 Bahasa Indonesia

### Fitur
- **📊 Analisa Teknikal**: History harga, MA, RSI, MACD, Bollinger Bands, Stochastic, ATR, pola candlestick
- **💰 Analisa Fundamental**: PER, PBV, DER, ROE, EPS, Cash Flow, penilaian rasio keuangan
- **📰 Analisa Sentimen**: Scraping berita, skor sentimen (positif/negatif/netral), clustering per sektor
- **🤖 Multi-LLM Review**: Ulasan saham berbasis AI menggunakan OpenAI, Gemini, Anthropic, Ollama, atau API OpenAI-compatible
- **⭐ Rekomendasi Saham**: Skor gabungan (teknikal + fundamental + sentimen), sinyal beli/tahan/jual
- **🎨 UI Modern**: Tema Dark/Light ala OpenAI, grid Bootstrap responsif, chart interaktif
- **⚙️ Konfigurasi Fleksibel**: Multiple database backend (SQLite, SQL Server, MySQL), opsi storage (FileSystem, MinIO, S3, Azure Blob)

### Tech Stack
- **Framework**: Blazor Server .NET 10
- **ORM**: Entity Framework Core
- **Database**: SQLite (default), SQL Server, MySQL
- **UI**: Bootstrap, Custom CSS
- **Charts**: Siap untuk ChartJs.Blazor.Fork
- **LLM**: OpenAI, Gemini, Anthropic, Ollama, OpenAI-compatible

### Mulai Cepat
```bash
cd StockAnalyzer
dotnet run
# Buka browser di https://localhost:5001
```

### Halaman Aplikasi
| Halaman | URL | Deskripsi |
|---------|-----|-----------|
| Dashboard | `/` | Overview pasar, top 10 rekomendasi, quick lookup |
| Technical Analysis | `/technical` | Indikator teknikal, price history, volume |
| Fundamental Analysis | `/fundamental` | Rasio keuangan, valuasi, kesehatan emiten |
| Sentiment Analysis | `/sentiment` | Berita, sentimen, clustering sektor |
| Recommendations | `/recommendations` | Rekomendasi saham dengan skor gabungan |
| LLM Review | `/llm-review` | Analisa AI dari berbagai provider LLM |
| Configuration | `/configuration` | Konfigurasi provider LLM, database, storage |

### Konfigurasi LLM
Edit `appsettings.json` bagian `LLM:Providers`:
```json
{
  "LLM": {
    "Providers": {
      "Ollama": {
        "ApiBaseUrl": "http://localhost:11434",
        "ModelName": "llama3.1",
        "IsEnabled": true
      },
      "OpenAI": {
        "ApiKey": "sk-your-openai-key",
        "ModelName": "gpt-4o",
        "IsEnabled": false
      }
    }
  }
}
```

---

## 📂 Project Structure
```
StockAnalyzer/
├── Models/                    # Domain models
├── Data/                      # EF Core DbContext
├── Services/
│   ├── StockData/             # Technical, fundamental, sentiment services
│   ├── LLM/                   # Multi-LLM providers & factory
│   ├── Storage/               # File storage abstraction
│   └── Recommendation/        # Stock recommendation engine
├── Components/
│   ├── Layout/                # Main layout with theme
│   ├── Pages/                 # Blazor pages
│   └── Shared/                # Reusable components
├── wwwroot/                   # Static assets
├── docs/                      # Documentation
└── PLAN.md                    # Development plan
```

## 📄 License
Proprietary - GraviCode Studios

---
Made with ❤️ by **Jacky the Code Bender** @ GraviCode Studios
> Kalau terbantu, traktir pulsa ya~ ☕ https://studios.gravicode.com/products/budax
