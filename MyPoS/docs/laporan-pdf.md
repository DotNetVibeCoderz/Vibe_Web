# Ekspor laporan ke PDF

Halaman **Laporan Penjualan** dan **Transaksi** dapat mengekspor isinya ke PDF berformat siap cetak, di samping ekspor Excel dan CSV yang sudah ada.

![Contoh laporan PDF](screenshots/13-laporan-pdf.png)

## Isi berkas

Setiap halaman PDF berisi:

- **Kop** — nama toko dengan warna aksen, alamat, telepon, dan NPWP, semuanya diambil dari Pengaturan
- **Judul laporan** dan waktu cetak di sisi kanan
- **Baris penyaring** — rentang tanggal, kategori, kasir, dan kata pencarian yang sedang aktif
- **Kotak ringkasan** — omzet, harga pokok, laba kotor, dan margin; atau total lunas, jumlah transaksi, pajak, dan diskon
- **Tabel** dengan kepala yang diulang di setiap halaman
- **Kaki halaman** dengan nama toko dan nomor halaman

Nominal diset dengan huruf monospace dan dirata-kanan, sehingga kolom rupiah berbaris lurus dan mudah dibandingkan — sama seperti di layar.

## Yang dicetak adalah yang terlihat

PDF memakai daftar baris, kolom, ringkasan, dan penyaring **yang sama persis** dengan yang sedang ditampilkan di layar. Kalau pengguna sudah menyaring rentang tanggal dan kategori tertentu, itulah yang masuk ke berkas.

Ini disengaja: laporan yang dikirim ke pihak lain tidak boleh berbeda isinya dengan yang dilihat orang yang mencetaknya.

## Laporan yang tersedia

| Halaman | Nama berkas | Orientasi |
|---|---|---|
| Laporan Penjualan | `LaporanPenjualan_yyyyMMdd.pdf` | Lanskap |
| Transaksi | `Transaksi_yyyyMMdd.pdf` | Lanskap |

Keduanya lanskap karena tabelnya lebar; potret akan memaksa kolom nominal terpotong.

## Struk

Struk transaksi punya jalur cetaknya sendiri dan tidak melewati layanan PDF. Ia dicetak lewat dialog cetak peramban dari halaman Kasir atau Transaksi, dengan `@media print` yang menyembunyikan seluruh halaman kecuali elemen struk. Untuk printer termal, tombol **Salin teks** menghasilkan struk dalam teks monospace selebar 32 atau 48 karakter mengikuti pengaturan lebar kertas.

## Menambah laporan PDF baru

`PdfReportService` bersifat umum terhadap jenis baris. Cukup jelaskan kolomnya:

```csharp
var bytes = Pdf.Build(
    title: "Kartu Stok",
    subtitle: "Pergerakan persediaan",
    rows: rows,
    columns: new List<PdfColumn<StockRow>>
    {
        new("Tanggal", 1.4f, false, r => r.Date.ToString("dd/MM/yyyy")),
        new("Produk",  2.5f, false, r => r.ProductName),
        new("Masuk",   1.0f, true,  r => r.In.ToString("N0")),
        new("Keluar",  1.0f, true,  r => r.Out.ToString("N0")),
        new("Saldo",   1.2f, true,  r => Money.FormatNumber(r.Balance))
    },
    summaries: new List<PdfSummary> { new("Total masuk", Money.Format(totalIn)) },
    filters: new List<string> { $"Periode {from:dd/MM/yyyy} - {to:dd/MM/yyyy}" },
    landscape: true);
```

Parameter `PdfColumn` berturut-turut adalah judul, bobot lebar relatif, apakah dirata-kanan, dan cara mengambil nilai sel. Kolom yang dirata-kanan otomatis memakai huruf monospace.

Kirim hasilnya ke pengguna dengan pola unduh yang sama seperti Excel dan CSV:

```csharp
using var stream = new MemoryStream(bytes);
await JS.InvokeVoidAsync("downloadFileFromStream", "KartuStok.pdf",
    new DotNetStreamReference(stream));
```

## Lisensi QuestPDF

Pembuatan PDF memakai [QuestPDF](https://www.questpdf.com). Aplikasi ini menyetel **Community License**, yang bebas dipakai organisasi dengan pendapatan tahunan di bawah 1 juta USD. Di atas ambang tersebut diperlukan lisensi berbayar — lihat halaman lisensi mereka. Penyetelannya ada di konstruktor statis `Services/PdfReportService.cs`.
