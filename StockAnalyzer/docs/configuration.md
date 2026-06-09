# ⚙️ Panduan Konfigurasi StockAnalyzer

## Daftar Isi
1. [Konfigurasi Database](#database)
2. [Konfigurasi Storage](#storage)
3. [Konfigurasi LLM Provider](#llm-provider)
4. [Konfigurasi Stock API](#stock-api)
5. [Konfigurasi Scoring Weights](#weights)
6. [Konfigurasi News Scraping](#news-scraping)

---

## Database

Konfigurasi database di `appsettings.json` → `Database`:

```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=Data/stockanalyzer.db"
  }
}
```

### SQLite (Default)
```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=Data/stockanalyzer.db"
  }
}
```

### SQL Server
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=StockAnalyzer;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### MySQL
```json
{
  "Database": {
    "Provider": "MySQL",
    "ConnectionString": "Server=localhost;Database=StockAnalyzer;User=root;Password=yourpassword;"
  }
}
```
> ⚠️ MySQL memerlukan package `Pomelo.EntityFrameworkCore.MySql`. Install dengan:
> ```bash
> dotnet add package Pomelo.EntityFrameworkCore.MySql
> ```

---

## Storage

Konfigurasi storage di `appsettings.json` → `Storage`:

### FileSystem (Default)
```json
{
  "Storage": {
    "Provider": "FileSystem",
    "BasePath": "Data/Storage"
  }
}
```

### MinIO / S3
```json
{
  "Storage": {
    "Provider": "S3",
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "stockanalyzer"
  }
}
```

### Azure Blob
```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "Endpoint": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    "BucketName": "stockanalyzer"
  }
}
```

---

## LLM Provider

Konfigurasi di `appsettings.json` → `LLM`:

### Struktur Konfigurasi
```json
{
  "LLM": {
    "DefaultProvider": "Ollama",
    "EnableLLMAnalysis": true,
    "TechnicalWeight": 0.35,
    "FundamentalWeight": 0.35,
    "SentimentWeight": 0.30,
    "Providers": { ... },
    "AnalysisMappings": { ... }
  }
}
```

### OpenAI
```json
{
  "OpenAI": {
    "ApiKey": "sk-your-openai-api-key",
    "ApiBaseUrl": "https://api.openai.com/v1",
    "ModelName": "gpt-4o",
    "FallbackModelName": "gpt-3.5-turbo",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 60,
    "IsEnabled": false
  }
}
```

### Google Gemini
```json
{
  "Gemini": {
    "ApiKey": "your-gemini-api-key",
    "ApiBaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "ModelName": "gemini-2.0-flash",
    "FallbackModelName": "gemini-1.5-flash",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 60,
    "IsEnabled": false
  }
}
```

### Anthropic Claude
```json
{
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key",
    "ApiBaseUrl": "https://api.anthropic.com/v1",
    "ModelName": "claude-3-sonnet-20240229",
    "FallbackModelName": "claude-3-haiku-20240307",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 60,
    "IsEnabled": false
  }
}
```

### Ollama (Local)
```json
{
  "Ollama": {
    "ApiKey": "",
    "ApiBaseUrl": "http://localhost:11434",
    "ModelName": "llama3.1",
    "FallbackModelName": "llama3",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 120,
    "IsEnabled": true
  }
}
```
> 💡 Pastikan Ollama sudah terinstall dan running. Download model dengan: `ollama pull llama3.1`

### OpenAI Compatible (LM Studio, vLLM, Groq, dll)
```json
{
  "OpenAICompatible": {
    "ApiKey": "not-needed",
    "ApiBaseUrl": "http://localhost:1234/v1",
    "ModelName": "local-model",
    "FallbackModelName": "",
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "TimeoutSeconds": 120,
    "IsEnabled": false
  }
}
```

### Analysis Mappings
Tentukan provider mana yang digunakan untuk setiap tipe analisa:
```json
{
  "AnalysisMappings": {
    "TechnicalReview": "Ollama",
    "FundamentalReview": "Ollama",
    "SentimentAnalysis": "Gemini",
    "StockRecommendation": "Ollama"
  }
}
```

---

## Stock API

```json
{
  "StockApi": {
    "Provider": "YahooFinance",
    "ApiKey": "",
    "BaseUrl": "",
    "RefreshIntervalMinutes": 15,
    "AutoRefresh": false
  }
}
```

---

## Scoring Weights

Menentukan bobot setiap komponen dalam overall score:
```json
{
  "LLM": {
    "TechnicalWeight": 0.35,
    "FundamentalWeight": 0.35,
    "SentimentWeight": 0.30
  }
}
```
> 💡 Total ketiga weight harus = 1.0

---

## News Scraping

```json
{
  "NewsScraping": {
    "Enabled": true,
    "Sources": [
      "https://www.cnbcindonesia.com/market",
      "https://market.bisnis.com/",
      "https://investasi.kontan.co.id/"
    ],
    "MaxArticlesPerSource": 10,
    "ScrapingIntervalHours": 1
  }
}
```

---

## Konfigurasi via UI

Semua konfigurasi di atas juga bisa dilihat melalui halaman **Configuration** (`/configuration`) di aplikasi. Halaman ini menampilkan status provider LLM, konfigurasi database, storage, dan scoring weights yang sedang aktif.
