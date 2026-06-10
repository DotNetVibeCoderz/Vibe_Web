# 📊 Dashboard & Pelaporan - Lapak

## Fitur Dashboard

### Statistik Utama
- Total Pendapatan
- Total Pesanan
- Jumlah Pelanggan
- Rata-rata Rating Produk

### Grafik & Visualisasi
- Ikhtisar Penjualan (30 hari)
- Distribusi Tier Pelanggan
- Status Pesanan (Pie chart)
- Produk Terlaris

### Tabel Data
- Daftar Pesanan Terbaru
- Filter berdasarkan status, tanggal, tier

## Konfigurasi Chart

Dashboard menggunakan **ChartJs.Blazor.Fork** untuk rendering chart. Konfigurasi:

```csharp
// Di Program.cs
builder.Services.AddChartJs(); // Setup ChartJs

// Di komponen
<Chart Config="_barChartConfig" />
```

## Filter Lanjutan

Dashboard mendukung filter:
- **Rentang Tanggal**: Filter data berdasarkan periode
- **Kategori**: Filter berdasarkan kategori produk
- **Toko**: Filter berdasarkan toko tertentu
- **Nilai Transaksi**: Filter berdasarkan range nilai
- **Tier Pelanggan**: Filter berdasarkan segmentasi

## Real-time Updates

Dashboard menggunakan SignalR untuk update real-time:
- Status pesanan berubah → Dashboard terupdate otomatis
- Transaksi baru → Statistik terupdate
- Perubahan data → Chart ter-refresh

## Export Laporan

Fitur export mendukung:
- PDF (coming soon)
- Excel/CSV (coming soon)

## Customer Scoring Dashboard

Segmentasi pelanggan ditampilkan dengan visualisasi:
- Bronze (0-99 poin)
- Silver (100-499 poin)
- Gold (500-999 poin)
- Platinum (1000+ poin)
