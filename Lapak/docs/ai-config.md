# 🤖 Konfigurasi AI - Lapak

## Daftar Provider yang Didukung

| Provider | Model Default | Endpoint | Keterangan |
|----------|--------------|----------|------------|
| OpenAI | gpt-4o-mini | api.openai.com | GPT-4o series |
| Gemini | gemini-1.5-flash | generativelanguage.googleapis.com | Google AI |
| Anthropic | claude-3-haiku-20240307 | api.anthropic.com | Claude series |
| Ollama | llama3 | localhost:11434 | Local LLM |

## Konfigurasi via appsettings.json

```json
{
  "AI": {
    "DefaultProvider": "OpenAI",
    "FallbackEnabled": true,
    "Providers": {
      "OpenAI": {
        "ApiKey": "sk-your-api-key",
        "Model": "gpt-4o-mini",
        "BaseUrl": "https://api.openai.com/v1",
        "MaxTokens": 2000,
        "Temperature": 0.7,
        "TimeoutSeconds": 60
      },
      "Gemini": {
        "ApiKey": "your-gemini-api-key",
        "Model": "gemini-1.5-flash",
        "BaseUrl": "https://generativelanguage.googleapis.com/v1beta"
      },
      "Anthropic": {
        "ApiKey": "your-anthropic-api-key",
        "Model": "claude-3-haiku-20240307",
        "BaseUrl": "https://api.anthropic.com"
      },
      "Ollama": {
        "ApiKey": "",
        "Model": "llama3",
        "BaseUrl": "http://localhost:11434",
        "TimeoutSeconds": 120
      }
    },
    "ChatBots": {
      "TonyKurus": {
        "Name": "Tony Kurus",
        "SystemPrompt": "Kamu adalah Tony Kurus, asisten belanja...",
        "Temperature": 0.8,
        "MaxTokens": 2000
      },
      "SitiBohay": {
        "Name": "Siti Bohay",
        "SystemPrompt": "Kamu adalah Siti Bohay, customer support...",
        "Temperature": 0.6,
        "MaxTokens": 2000
      }
    }
  }
}
```

## Fallback Mechanism

Sistem secara otomatis akan mencoba provider dalam urutan:
1. Provider yang dipilih / default
2. OpenAI → Gemini → Anthropic → Ollama

Jika `FallbackEnabled: false`, sistem hanya akan mencoba provider default.

## ChatBot Configuration

Setiap chatbot memiliki konfigurasi terpisah:
- **Name**: Nama tampilan chatbot
- **SystemPrompt**: Prompt sistem yang mendefinisikan persona dan perilaku
- **Temperature**: Tingkat kreativitas (0.0 = deterministik, 1.0 = kreatif)
- **MaxTokens**: Batas maksimum token output

## Tools untuk Chatbots

Chatbots dilengkapi tools:
- **Cek Waktu**: Mengambil current UTC time
- **Kalkulasi Math**: Menyelesaikan perhitungan matematika
- **Search Internet**: Mencari informasi terkini
- **Upload File**: Analisis gambar dan dokumen (coming soon)
