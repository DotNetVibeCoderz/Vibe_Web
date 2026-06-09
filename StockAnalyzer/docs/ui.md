# 🎨 Panduan UI/UX StockAnalyzer

## Tema

StockAnalyzer memiliki **dua tema** yang bisa diganti dengan toggle di sidebar:
- **🌙 Dark Theme** (default) - Tema gelap ala OpenAI dengan aksen ungu
- **☀️ Light Theme** - Tema terang yang bersih dan profesional

Tema disimpan di `localStorage` browser sehingga tetap persisten setelah refresh.

---

## Navigasi

### Sidebar (Kiri)
Sidebar tetap (fixed) di sisi kiri layar berisi:
| Menu | Icon | Halaman |
|------|------|---------|
| Dashboard | 🏠 | Overview pasar & top picks |
| Technical Analysis | 📊 | Indikator & price history |
| Fundamental Analysis | 💰 | Rasio keuangan & valuasi |
| Sentiment Analysis | 📰 | Berita & sentimen |
| Recommendations | ⭐ | Rekomendasi saham |
| LLM Review | 🤖 | Analisa AI |
| Configuration | ⚙️ | Pengaturan sistem |

### Top Bar
- Judul halaman saat ini
- Status market (Market Open/Close)
- Jam realtime

---

## Halaman

### 1. Dashboard (`/`)
**Stat Cards**:
- Total Stocks: Jumlah emiten yang ditracking
- Market Sentiment: Rata-rata sentimen 7 hari terakhir
- LLM Status: Provider LLM yang aktif
- Top Picks: Jumlah saham dengan rating Buy

**Top 10 Table**:
- Ranking 1-10
- Kode saham, nama perusahaan, sektor
- Score bar dengan warna
- Badge rekomendasi (StrongBuy/Buy/Hold/Sell/StrongSell)

**Sector Distribution**:
- Progress bar per sektor
- Jumlah saham per sektor

**Quick Lookup**:
- Input kode saham
- Hasil pencarian instan dengan info dasar

### 2. Technical Analysis (`/technical`)
- **Filter Bar**: Input kode saham + pilihan periode (30/90/180/365 hari)
- **Price Summary**: Current price, change, MA20, MA50, RSI, MACD signal
- **Indicators Table**: RSI, MACD, Bollinger Bands, Stochastic, ATR
- **Volume Analysis**: Buy/Sell volume, foreign net, avg volume
- **Price History Table**: 30 hari terakhir dengan semua data OHLCV

### 3. Fundamental Analysis (`/fundamental`)
- **Filter Bar**: Input kode saham
- **Financial Health Score**: 0-100 dengan label (Excellent/Good/Fair/Weak)
- **Profitability**: PER, EPS, ROE, ROA, margin
- **Valuation**: PBV, PSR, EV/EBITDA, Dividend Yield
- **Solvency**: DER, Current Ratio, Interest Coverage
- **Growth**: Revenue/Earnings growth dengan assessment
- **Balance Sheet**: Assets, Liabilities, Equity, Revenue, Net Income

### 4. Sentiment Analysis (`/sentiment`)
- **Filter Bar**: Input kode saham + tombol scraping berita
- **Sentiment Overview**: Aggregate score, positive/negative/neutral count
- **News List**: Artikel berita dengan:
  - Sentiment badge (🟢/🔴/🟡)
  - Judul, publisher, tanggal
  - Link ke sumber
- **Sector Clustering**: Tabel sentimen per sektor

### 5. Recommendations (`/recommendations`)
- **Filter Bar**: Input kode + filter sektor + refresh button
- **Recommendation Card** (setelah analisa):
  - Badge rekomendasi besar
  - Overall score dengan progress bar
  - Target price, stop loss, risk level
  - Score breakdown (technical, fundamental, sentiment)
  - AI review box (jika LLM enabled)
- **Top 10 Table**: Rekomendasi teratas dengan aksi "Detail"

### 6. LLM Review (`/llm-review`)
- **Filter Bar**: Kode saham + tipe analisa + pilihan provider
- **Provider Status**: Indikator availability per provider
- **Analysis Result**: Response dari LLM provider
- **Analysis History**: Log analisa sebelumnya

### 7. Configuration (`/configuration`)
- **Tab Navigation**: LLM Providers | Database | Storage | Stock API | Weights
- **LLM Providers**: List semua provider dengan detail konfigurasi
- **Database**: Provider dan connection string aktif
- **Storage**: Provider dan setting
- **Weights**: Bobot skoring

---

## Komponen UI

### Badges
- **Recommendation Badges**: StrongBuy (hijau), Buy (hijau muda), Hold (kuning), Sell (orange), StrongSell (merah)
- **Sentiment Badges**: Bullish (hijau), Bearish (merah), Neutral (kuning)
- **Health Badges**: Excellent (hijau), Good (ungu), Fair (kuning), Weak (merah)

### Score Bars
- Progress bar animasi yang menunjukkan skor 0-100
- Warna berbeda: Technical (indigo), Fundamental (ungu), Sentiment (pink)

### Cards
- Container dengan border-radius 12px
- Shadow halus
- Header dan body terpisah

### Tables
- Header uppercase dengan warna muted
- Hover effect pada row
- Striped rows untuk readability
- Scrollable untuk data banyak

### Responsive
- Grid 4 kolom → 2 kolom → 1 kolom (mobile)
- Sidebar hide di layar kecil
- Filter bar wrapping

---

## Tips Penggunaan

1. **Mulai dari Dashboard**: Lihat overview market dan top picks
2. **Analisa Saham Spesifik**: Masukkan kode di Quick Lookup
3. **Deep Dive**: Buka halaman Technical/Fundamental/Sentiment
4. **Generate Rekomendasi**: Buka halaman Recommendations
5. **AI Review**: Gunakan LLM Review dengan provider aktif
6. **Cek Konfigurasi**: Pastikan LLM provider aktif di Configuration

### Tips LLM
- Untuk development, gunakan **Ollama** (gratis, local)
- Untuk analisa sentimen, **Gemini** unggul dalam NLP
- Untuk rekomendasi, **OpenAI GPT-4o** memberikan hasil terbaik
- Gunakan **OpenAI Compatible** untuk endpoint custom seperti Groq atau LM Studio
