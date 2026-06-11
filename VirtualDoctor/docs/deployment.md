# 🚀 Deployment Guide - VirtualDoctor

## Setup Environment

### 1. Prerequisites
```bash
# Install .NET 10 SDK
# https://dotnet.microsoft.com/download/dotnet/10.0

# Clone / download project
cd VirtualDoctor
```

### 2. Database Configuration

#### SQLite (Default)
```json
{
  "Database": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=VirtualDoctor.db"
  }
}
```

#### SQL Server
```json
{
  "Database": {
    "Provider": "SqlServer",
    "ConnectionString": "Server=localhost;Database=VirtualDoctor;User Id=sa;Password=YourPassword;TrustServerCertificate=true"
  }
}
```

#### PostgreSQL
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "ConnectionString": "Host=localhost;Database=VirtualDoctor;Username=postgres;Password=YourPassword"
  }
}
```

#### MySQL
```json
{
  "Database": {
    "Provider": "MySql",
    "ConnectionString": "Server=localhost;Database=VirtualDoctor;User=root;Password=YourPassword"
  }
}
```

### 3. LLM Configuration

Edit `appsettings.json` bagian `Llm`:
- Masukkan API key untuk OpenAI, Gemini, atau Anthropic
- Atau gunakan Ollama lokal (install Ollama terlebih dahulu)

### 4. Storage Configuration

#### File System (Default)
```json
{
  "Storage": {
    "Provider": "FileSystem",
    "BasePath": "uploads"
  }
}
```

#### MinIO / S3
```json
{
  "Storage": {
    "Provider": "MinIO",
    "Endpoint": "http://localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "virtualdoctor"
  }
}
```

#### Azure Blob
```json
{
  "Storage": {
    "Provider": "AzureBlob",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;",
    "BucketName": "virtualdoctor"
  }
}
```

### 5. Running the Application

```bash
# Development
dotnet run

# Production
dotnet publish -c Release -o publish
cd publish
dotnet VirtualDoctor.dll

# With specific URLs
dotnet run --urls "https://0.0.0.0:5001"
```

### 6. UI Configuration

- **Light/Dark Theme**: Toggle di top bar (ikon ☀️/🌙)
- **Bot Settings**: Halaman `/settings` untuk ubah nama, system prompt, temperature
- **LLM Provider**: Pilih provider di UI AI Chat page

### 7. Demo Accounts

| Email | Password | Role |
|-------|----------|------|
| budi@email.com | Password123! | User |
| siti@email.com | Password123! | User |
| admin@virtualdoctor.com | Password123! | Admin |

### 8. PDF Indexing

1. Buat folder `wwwroot/HealthPdfs/`
2. Letakkan file PDF di folder tersebut
3. Worker akan auto-index setiap 30 menit
4. Atau trigger manual via `DocumentIndexingService.ReindexAllAsync()`

### 9. Google Maps API

```json
{
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  }
}
```

### 10. Search API (Tavily/Perplexity)

```json
{
  "Search": {
    "Provider": "Tavily",
    "ApiKey": "YOUR_TAVILY_API_KEY"
  }
}
```
