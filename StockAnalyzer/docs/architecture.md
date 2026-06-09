# 🏗️ Arsitektur Sistem StockAnalyzer

## Diagram Arsitektur

```
┌─────────────────────────────────────────────────────────────────┐
│                         BLAZOR UI LAYER                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐   │
│  │Dashboard │ │Technical │ │Fundamental│ │Recommendations  │   │
│  │   Page   │ │   Page   │ │   Page    │ │     Page        │   │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └───────┬──────────┘   │
│       │            │            │               │               │
│  ┌────┴────────────┴────────────┴───────────────┴──────────┐   │
│  │                   SHARED COMPONENTS                      │   │
│  │  NavItem │ FilterBar │ ScoreBar │ RecBadge │ ThemeToggle │   │
│  └────────────────────────┬─────────────────────────────────┘   │
└───────────────────────────┼─────────────────────────────────────┘
                            │
┌───────────────────────────┼─────────────────────────────────────┐
│                     SERVICE LAYER                                │
│  ┌────────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │ StockData      │  │ LLM          │  │ Recommendation     │  │
│  │ Service        │  │ Service      │  │ Service            │  │
│  │                │  │              │  │                    │  │
│  │ - Technical    │  │ - OpenAI     │  │ - Scoring Engine   │  │
│  │ - Fundamental  │  │ - Gemini     │  │ - Top 10           │  │
│  │ - Sentiment    │  │ - Anthropic  │  │ - Sector Filter    │  │
│  │ - News Scraping│  │ - Ollama     │  │ - LLM Integration  │  │
│  └───────┬────────┘  │ - Compatible │  └─────────┬──────────┘  │
│          │           └──────┬───────┘            │              │
│  ┌───────┴────────┐  ┌──────┴───────┐  ┌────────┴──────────┐  │
│  │ Storage        │  │ LLM Config   │  │ LLM Provider      │  │
│  │ Service        │  │ Service      │  │ Factory           │  │
│  │                │  │              │  │                   │  │
│  │ - FileSystem   │  │ - Sync from  │  │ - Provider Router │  │
│  │ - MinIO/S3     │  │   appsettings│  │ - Fallback Logic  │  │
│  │ - Azure Blob   │  │ - CRUD Ops   │  │ - Availability    │  │
│  └───────┬────────┘  └──────────────┘  └───────────────────┘  │
└──────────┼─────────────────────────────────────────────────────┘
           │
┌──────────┼─────────────────────────────────────────────────────┐
│                   DATA ACCESS LAYER                             │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    EF Core DbContext                      │  │
│  │                                                          │  │
│  │  StockEmitens  │  TechnicalIndicators  │  FundamentalData │  │
│  │  SentimentData │  Recommendations      │  LLM Configs     │  │
│  │  SectorSentiments │  AppConfigurations │  TopRecommendations│
│  └────────────────────────┬─────────────────────────────────┘  │
└───────────────────────────┼─────────────────────────────────────┘
                            │
┌───────────────────────────┼─────────────────────────────────────┐
│                     DATABASE LAYER                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────┐   │
│  │ SQLite   │  │ SQL      │  │ MySQL    │  │ (extensible) │   │
│  │ (default)│  │ Server   │  │          │  │              │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     EXTERNAL SERVICES                            │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐   │
│  │ Yahoo    │ │ News     │ │ OpenAI   │ │ Ollama (Local)   │   │
│  │ Finance  │ │ Sites    │ │ API      │ │                  │   │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Layer Detail

### 1. Blazor UI Layer (`Components/`)
- **Pages**: Halaman utama yang dipetakan ke route Blazor
- **Layout**: MainLayout dengan sidebar dan dark/light theme
- **Shared**: Komponen reusable (NavItem, filter, badge)
- **Dashboard/Charts/Filters**: Siap untuk ekstensi chart dan filter lanjutan

### 2. Service Layer (`Services/`)
- **StockData Services**: Logika bisnis untuk data saham
  - `StockDataService`: CRUD data emiten
  - `TechnicalAnalysisService`: Kalkulasi indikator teknikal
  - `FundamentalAnalysisService`: Kalkulasi dan penilaian rasio
  - `SentimentAnalysisService`: Analisa sentimen keyword-based
  - `NewsScrapingService`: Scraping berita dengan fallback simulasi
- **LLM Services**: Integrasi multi-LLM
  - `LLMProviderFactory`: Factory pattern untuk routing provider
  - Individual providers: OpenAI, Gemini, Anthropic, Ollama, OpenAICompatible
  - `LLMService`: Orchestrator untuk request LLM
  - `LLMConfigService`: Manajemen konfigurasi LLM
- **Storage Services**: Abstraksi penyimpanan file
- **Recommendation Services**: Engine rekomendasi saham

### 3. Data Access Layer (`Data/`)
- `AppDbContext`: EF Core context dengan konfigurasi relasi dan seed data
- Mendukung migrasi database untuk production

### 4. Models Layer (`Models/`)
- Domain models dengan Data Annotations
- Relasi antar entity terdefinisi dengan Foreign Key
- Indexes untuk performa query

## Design Patterns
- **Repository Pattern** (via EF Core)
- **Factory Pattern** (LLM Providers, Storage)
- **Strategy Pattern** (Provider implementations)
- **Dependency Injection** (All services)

## Data Flow
```
User Input → Blazor Page → Service Layer → EF Core DbContext → Database
                                ↓
                          LLM Provider → External API
                                ↓
                          Response → Service Layer → Blazor Page → UI Update
```
