# ✨ Fitur Detail VirtualDoctor

## 1. Konsultasi Dokter Online

### Chat Konsultasi
- Pilih dokter dari daftar (filter: spesialisasi, online/offline)
- Chat real-time dengan dokter
- Kirim pesan teks, gambar, dan file
- Riwayat konsultasi lengkap per user

### Video Call (Ready)
- Integrasi SignalR untuk signaling
- WebRTC peer connection ready

## 2. Pembelian Obat & Vitamin

### Katalog Obat
- Kategori: Obat Bebas, Obat Keras, Vitamin, Suplemen
- Informasi lengkap: deskripsi, dosis, efek samping, harga
- Indikator resep dokter

### Order Flow
1. Browse katalog → Tambah ke keranjang
2. Review keranjang → Update quantity
3. Pilih apotek terdekat
4. Isi alamat pengiriman
5. Opsional: verifikasi asuransi
6. Checkout → Order confirmed

## 3. Homecare Services

- Tes Lab: cek darah lengkap, kolesterol, gula darah
- Vaksinasi: influenza, COVID-19
- Vitamin Booster
- Panggil Dokter ke rumah
- Kunjungan Perawat

## 4. Booking RS/Klinik

1. Pilih dokter
2. Pilih tipe (tatap muka/online)
3. Pilih RS/klinik (untuk tatap muka)
4. Pilih tanggal & jam
5. Lihat estimasi biaya
6. Konfirmasi booking

## 5. AI Chat Multi-LLM

### Provider
| Provider | Model | Konfigurasi |
|----------|-------|-------------|
| OpenAI | gpt-4o | API Key + Endpoint |
| Gemini | gemini-2.0-flash | API Key |
| Claude | claude-3-5-sonnet | API Key |
| Ollama | llama3.1 | Endpoint lokal |
| Custom | any | OpenAI-compatible endpoint |

### Kernel Functions (11 tools)
1. **searchInternet** - Cari informasi via Tavily/Perplexity
2. **checkDate** - Cek tanggal & waktu saat ini
3. **mathCalc** - Kalkulasi matematika
4. **readFileFromUrl** - Baca file dari URL
5. **describeImage** - Deskripsi gambar via LLM vision
6. **scrapWebPage** - Scraping konten halaman web
7. **askDoctor** - Rujukan konsultasi dokter
8. **orderMedicine** - Cari & pesan obat
9. **scheduleDoctor** - Cek jadwal dokter
10. **findHospital** - Cari RS/klinik terdekat
11. **queryHealthDocs** - Tanya artikel via RAG

## 6. RAG (Retrieval Augmented Generation)

- PDF → Ekstrak teks → Chunk → Embed → Index
- Query → Search vector → Retrieve context → LLM generate answer
- Worker auto-index setiap 30 menit
- Status indexing per artikel

## 7. Dashboard & Reporting

- Statistik dokter, obat, appointment, order
- Tabel data interaktif
- Filter & search advance
- Data real-time dari database

## 8. Integrasi Asuransi

- Provider: BPJS, Prudential, Allianz, Manulife, AIA
- Verifikasi polis
- Kalkulasi coverage
- Payment flow dengan asuransi

## 9. Bot Personality

Default: **dokter Markonah Al-senyumwati**
- Cerdas, ramah, informatif, sopan, humoris
- SystemPrompt & Temperature bisa di-konfigurasi via UI Settings
