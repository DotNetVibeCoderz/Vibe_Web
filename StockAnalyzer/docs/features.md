# 📋 Fitur Detail StockAnalyzer

## 📊 Analisa Teknikal (Technical Analysis)

### Data yang Ditampilkan
- **Price History**: Open, High, Low, Close, Adjusted Close
- **Volume**: Total volume, buy/sell volume, foreign net volume
- **Moving Averages**: MA5, MA10, MA20, MA50, MA200
- **RSI (Relative Strength Index)**: Indikator momentum 14-hari
- **MACD**: Moving Average Convergence Divergence (12, 26, 9)
- **Bollinger Bands**: Upper, Middle (MA20), Lower bands
- **Stochastic Oscillator**: %K dan %D
- **ATR (Average True Range)**: Indikator volatilitas
- **Bandar Movement**: Akumulasi dan distribusi big player

### Metode Analisa
1. **Trend Analysis**: Menentukan tren (Bullish/Bearish/Sideways) berdasarkan posisi MA20 dan MA50
2. **Candlestick Patterns**: Deteksi Doji, Hammer, Shooting Star, Engulfing, Marubozu
3. **Technical Scoring**: Skor 0-100 berdasarkan kombinasi RSI, MACD, MA, Volume, Bollinger Bands
4. **Signal Generation**: Overbought/Oversold dari RSI dan Stochastic

### Sumber Data
- Yahoo Finance API (default)
- Alpha Vantage API
- IDX API (Indonesian Stock Exchange)
- Dapat diperluas dengan provider lain

---

## 💰 Analisa Fundamental (Fundamental Analysis)

### Data yang Ditampilkan
- **Profitabilitas**: PER, EPS, ROE, ROA, Net Profit Margin, Gross Profit Margin
- **Valuasi**: PBV, PSR, EV/EBITDA, Dividend Yield
- **Solvabilitas**: DER, Current Ratio, Interest Coverage
- **Pertumbuhan**: Revenue Growth, Earnings Growth
- **Cash Flow**: Operating Cash Flow, Free Cash Flow, Cash Flow Per Share
- **Neraca**: Total Assets, Total Liabilities, Total Equity, Revenue, Net Income

### Metode Analisa
1. **Ratio Assessment**: Setiap rasio dinilai dengan kategori (Excellent/Good/Fair/Poor)
2. **Fundamental Scoring**: Skor 0-100 berdasarkan kombinasi PER, PBV, DER, ROE, Growth, Current Ratio, Free Cash Flow
3. **Financial Health**: Klasifikasi kesehatan (Excellent/Good/Fair/Weak)

### Sumber Data
- Laporan keuangan emiten
- API data fundamental
- Input manual / upload CSV

---

## 📰 Analisa Sentimen (Sentiment Analysis)

### Data yang Ditampilkan
- **News Articles**: Judul, konten, publisher, tanggal publikasi
- **Sentiment Score**: -1.0 (sangat negatif) hingga +1.0 (sangat positif)
- **Sentiment Label**: Positive, Negative, Neutral
- **Sector Clustering**: Agregasi sentimen per sektor industri

### Metode Analisa
1. **Keyword-Based Analysis**: Matching kata kunci positif dan negatif dari berita
2. **Aggregate Sentiment**: Rata-rata sentimen dari seluruh berita terkait
3. **Sector Clustering**: Pengelompokan sentimen berdasarkan sektor
4. **LLM-Enhanced**: Opsional analisa sentimen lebih dalam menggunakan LLM

### Sumber Berita
- CNBC Indonesia
- Bisnis.com
- Kontan.co.id
- Investasi Kontan
- Dapat diperluas dengan source lain

---

## 🤖 Multi-LLM Review

### Provider yang Didukung
| Provider | Model Default | Keterangan |
|----------|--------------|------------|
| **OpenAI** | gpt-4o | Cloud API, perlu API key |
| **Gemini** | gemini-2.0-flash | Google AI, perlu API key |
| **Anthropic** | claude-3-sonnet | Cloud API, perlu API key |
| **Ollama** | llama3.1 | Local, gratis, perlu Ollama running |
| **OpenAI Compatible** | local-model | Custom endpoint (LM Studio, vLLM, Groq, dll) |

### Tipe Analisa LLM
1. **Technical Review** → Analisa teknikal oleh AI
2. **Fundamental Review** → Analisa fundamental oleh AI
3. **Sentiment Analysis** → Analisa sentimen berita oleh AI
4. **Stock Recommendation** → Rekomendasi lengkap (buy/sell/hold, target price, stop loss, risk level)

### Konfigurasi
- Setiap tipe analisa bisa menggunakan provider/model berbeda
- Fallback otomatis jika provider utama tidak tersedia
- Konfigurasi via `appsettings.json` dan UI admin

---

## ⭐ Rekomendasi Saham

### Scoring System
- **Technical Score**: 0-100 (weight: 35% default)
- **Fundamental Score**: 0-100 (weight: 35% default)
- **Sentiment Score**: 0-100 (weight: 30% default)
- **Overall Score**: Weighted average ketiga skor

### Level Rekomendasi
| Skor | Rekomendasi | Target Price | Risk Level |
|------|-------------|-------------|------------|
| 80-100 | Strong Buy | +30% | Low |
| 65-80 | Buy | +15% | Low-Medium |
| 45-65 | Hold | +5% | Medium |
| 30-45 | Sell | -10% | Medium-High |
| 0-30 | Strong Sell | -20% | High |

### Fitur
- Top 10 rekomendasi (di-cache 24 jam)
- Filter by sektor
- Analisa per kode saham (input manual)
- LLM review opsional untuk setiap rekomendasi

---

## ⚙️ Konfigurasi

### Database
- **SQLite** (default): File-based, cocok untuk development
- **SQL Server**: Untuk production skala besar
- **MySQL**: Alternatif open-source

### Storage
- **FileSystem** (default): Penyimpanan lokal
- **MinIO**: Self-hosted S3-compatible
- **S3**: AWS S3 cloud storage
- **Azure Blob**: Microsoft Azure storage

### LLM Providers
- Konfigurasi API key, endpoint, model per provider
- Pilihan model per tipe analisa
- Timeout dan temperature yang dapat disesuaikan
