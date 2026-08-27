# Perhitungan pajak, diskon, dan pembulatan

Seluruh perhitungan uang berada di satu kelas, `Services/TaxCalculator.cs`. Halaman kasir, simulasi di halaman Pengaturan, dan penyimpanan transaksi memakai kelas yang sama, sehingga angka yang terlihat di layar selalu sama dengan yang tersimpan dan tercetak.

## Urutan perhitungan

```
1.  Kotor           = Σ (harga satuan × jumlah)
2.  Diskon baris    = Σ diskon per baris
3.  Subtotal        = Kotor − Diskon baris
4.  Diskon transaksi= (Subtotal × persen) + nominal      → dibatasi maksimal Subtotal
5.  Penjualan bersih= Subtotal − Diskon transaksi
6.  Biaya layanan   = Penjualan bersih × persen layanan  → bila diaktifkan
7.  DPP             = (Penjualan bersih atau Subtotal) [+ Biaya layanan bila kena pajak]
8.  Pajak           = eksklusif : DPP × tarif
                      inklusif  : DPP − (DPP ÷ (1 + tarif))
9.  Total           = eksklusif : Penjualan bersih + Layanan + Pajak
                      inklusif  : Penjualan bersih + Layanan
10. Pembulatan      = Total dibulatkan ke kelipatan yang dipilih
```

Langkah 7 mengikuti pengaturan **Hitung pajak setelah diskon**. Bila dinonaktifkan, DPP memakai Subtotal, yaitu nilai sebelum diskon transaksi.

Setiap komponen dibulatkan ke presisi mata uang yang berlaku (`CurrencyDecimals`, bawaan 0 untuk Rupiah) memakai pembulatan setengah menjauh dari nol. Pembulatan per komponen ini penting: tanpanya, penjumlahan angka yang tercetak di struk bisa meleset dari total yang ditagih.

## Eksklusif dan inklusif

**Eksklusif** — harga yang tertera belum termasuk pajak, pajak ditambahkan di atasnya.

```
Harga tertera   100.000
PPN 11%          11.000
Total           111.000
```

**Inklusif** — harga yang tertera sudah mengandung pajak, pajak diurai dari dalamnya. Total tetap sama dengan harga tertera.

```
Harga tertera   100.000
PPN 11%           9.910   ← 100.000 − (100.000 ÷ 1,11)
Total           100.000
```

Pilih inklusif bila label harga di rak sudah harga akhir yang dibayar pelanggan.

## Contoh dengan diskon

Pengaturan bawaan: PPN 11%, eksklusif, dihitung setelah diskon, tanpa biaya layanan, tanpa pembulatan.

| | |
|---|---:|
| 2 × Rp 50.000 | 100.000 |
| 1 × Rp 30.000 | 30.000 |
| **Subtotal** | **130.000** |
| Diskon 10% | −13.000 |
| **Dasar pengenaan pajak** | **117.000** |
| PPN 11% | 12.870 |
| **Total** | **129.870** |

Angka yang sama ditampilkan pada panel Simulasi di **Pengaturan → Pajak & Biaya**, sehingga setiap perubahan pengaturan dapat langsung diperiksa akibatnya sebelum disimpan.

## Yang diperbaiki dari versi sebelumnya

Versi awal menghitung pajak dengan satu baris:

```csharp
decimal TaxAmount => SubTotal * 0.11m;
```

Baris itu bermasalah dalam empat hal, dan keempatnya sudah ditangani:

1. **Tarif tertanam di kode.** Tarif 11% tidak dapat diubah tanpa menyunting dan membangun ulang aplikasi. Sekarang tarif, nama pajak, dan status aktifnya diatur dari halaman Pengaturan.
2. **Diskon tidak pernah mengurangi dasar pengenaan pajak.** Kolom `DiscountAmount` selalu diisi nol dan tidak pernah dipakai, sehingga pajak selalu dihitung dari nilai sebelum potongan. Sekarang DPP mengikuti nilai setelah diskon.
3. **Harga yang sudah termasuk pajak dikenai pajak dua kali.** Tidak ada penanganan harga inklusif sama sekali. Sekarang tersedia mode inklusif yang mengurai pajak dari harga.
4. **Tarif tidak diabadikan.** Struk lama akan ikut berubah ketika tarif diganti. Sekarang setiap transaksi menyimpan `TaxRate` dan `TaxInclusive` yang berlaku saat itu, jadi riwayat tidak pernah berubah surut.

Satu masalah lain di luar pajak juga ikut diperbaiki: halaman kasir lama membuang diam-diam baris yang stoknya kurang dari jumlah yang diminta, tetapi tetap menagih total penuh. Sekarang seluruh transaksi ditolak dengan pesan yang menyebutkan produk dan sisa stoknya.

## Pembulatan total

Berguna untuk toko yang tidak menyimpan pecahan kecil.

| Mode | Total 129.870 menjadi |
|---|---|
| Tanpa pembulatan | 129.870 |
| Ke kelipatan 100 | 129.900 |
| Ke kelipatan 500 | 130.000 |
| Ke kelipatan 1.000 | 130.000 |

Selisih pembulatan disimpan tersendiri pada kolom `RoundingAmount` dan dicetak sebagai baris terpisah di struk, sehingga tetap dapat ditelusuri.
