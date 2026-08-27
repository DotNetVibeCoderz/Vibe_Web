# Rujukan pengaturan

Semua nilai di bawah ini dapat diubah dari **Pengaturan** tanpa menyunting kode. Halaman ini hanya dapat dibuka oleh peran **Admin**.

## Cara penyimpanannya

Setiap properti pada `Services/PosSettings.cs` disimpan sebagai satu baris pada tabel `Settings`, dengan `Key` berisi nama properti. Menambah pengaturan baru cukup dengan menambah satu properti — skema basis data tidak berubah, dan nilai lama tetap terbaca.

`SettingsService` menyimpan hasil pembacaan di memori dan membuang cache tersebut setiap kali disimpan, sehingga halaman tidak menembak basis data hanya untuk memformat angka.

## Toko

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Nama toko | MyPoS | Tampil di menu samping, judul tab, dan kepala struk |
| Slogan | Kasir Toko Modern | Baris kecil di bawah nama toko |
| Alamat | Jl. Merdeka No. 1, Jakarta Pusat | Dicetak di struk |
| Telepon | 021-1234567 | Dicetak di struk |
| NPWP | *(kosong)* | Dicetak di struk bila diisi |
| Logo | *(kosong)* | Diunggah lewat `IStorageService` yang sedang aktif |

## Mata uang

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Kode mata uang | IDR | Dikirim ke penyedia pembayaran |
| Simbol | Rp | Ditampilkan di seluruh nominal |
| Culture | id-ID | Menentukan pemisah ribuan dan desimal |
| Angka desimal | 0 | Sekaligus presisi pembulatan seluruh perhitungan |
| Posisi simbol | prefix | `prefix` → Rp 15.000, `suffix` → 15.000 Rp |

Bila culture yang diisi tidak tersedia di mesin tempat aplikasi berjalan, format Rupiah tetap dipertahankan melalui pemisah cadangan: titik untuk ribuan, koma untuk desimal.

## Pajak dan biaya

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Kenakan pajak | aktif | Menonaktifkan akan menghilangkan baris pajak dari struk |
| Nama pajak | PPN | Label yang tercetak |
| Tarif (%) | 11 | |
| Harga sudah termasuk pajak | nonaktif | Aktif = pajak diurai dari harga, bukan ditambahkan |
| Hitung pajak setelah diskon | aktif | Menentukan Dasar Pengenaan Pajak |
| Tambahkan biaya layanan | nonaktif | |
| Biaya layanan (%) | 5 | Dihitung dari nilai setelah diskon |
| Biaya layanan kena pajak | aktif | Menentukan apakah layanan masuk DPP |
| Pembulatan total | Tanpa pembulatan | 100 / 500 / 1.000 |

Rumus lengkapnya ada di [pajak.md](pajak.md).

## Struk

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Awalan nomor invoice | INV | Menghasilkan `INV-20260827-0001` |
| Catatan kaki | Terima kasih atas kunjungan Anda | |
| Lebar kertas | 80 mm | 58 mm ≈ 32 karakter, 80 mm ≈ 48 karakter |
| Cetak logo | aktif | |
| Cetak nama kasir | aktif | |

Nomor invoice berurutan per hari, diambil dari nomor tertinggi yang sudah terpakai pada hari tersebut. Indeks unik pada kolom invoice menjadi pengaman bila dua kasir menyimpan pada saat yang sama.

## Stok dan loyalitas

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Ambang stok menipis | 10 | Dipakai bila produk tidak punya ambang sendiri |
| Tolak penjualan bila stok kurang | aktif | Nonaktif memperbolehkan stok minus |
| Aktifkan poin loyalitas | aktif | |
| Nominal belanja per 1 poin | 10.000 | Belanja Rp 10.000 menghasilkan 1 poin |

Poin hanya diberikan bila transaksi berstatus lunas dan ada pelanggan yang dipilih. Membatalkan transaksi akan menarik kembali poin tersebut.

## Pembayaran

| Pengaturan | Bawaan |
|---|---|
| Terima pembayaran tunai | aktif |
| Base URL publik | *(kosong, memakai alamat saat ini)* |
| Xendit: aktif, Secret Key, Callback Token | nonaktif |
| Midtrans: aktif, Server Key, Client Key, mode produksi | nonaktif |
| Stripe: aktif, Secret Key, mata uang | nonaktif |

Rincian pemasangannya ada di [pembayaran.md](pembayaran.md).

## API

Tab ini tidak menyimpan nilai ke `PosSettings`, melainkan mengelola baris pada tabel `ApiKeys`. Setiap kunci punya nama, izin (**baca saja** atau **baca & tulis**), tanggal kedaluwarsa opsional, dan sakelar aktif.

Nilai kunci penuh hanya diperlihatkan satu kali saat dibuat; yang tersimpan hanyalah hash PBKDF2-nya. Waktu pemakaian terakhir dicatat agar kunci yang sudah tidak dipakai mudah ditemukan.

Pengaturan REST API sendiri — aktif atau tidak, Swagger aktif atau tidak, dan awalan rute — ada di `appsettings.json` bagian `Api`, karena keduanya menentukan bagaimana aplikasi dijalankan, bukan bagaimana toko berdagang. Rinciannya di [api.md](api.md).

## Tampilan dan sesi

| Pengaturan | Bawaan | Keterangan |
|---|---|---|
| Mode gelap sebagai bawaan | nonaktif | Pilihan tiap pengguna disimpan di perambannya sendiri dan menimpa nilai ini |
| Warna aksen | #B3382B | Kode heksadesimal; dipakai MudBlazor sebagai warna primer |
| Durasi sesi (jam) | 12 | Kasir diminta masuk ulang setelah rentang ini |

## Catatan keamanan

Kunci rahasia penyedia pembayaran tersimpan sebagai teks biasa di dalam basis data. Untuk pemakaian sungguhan:

- batasi akses basis data di tingkat sistem operasi atau server;
- pastikan hanya peran Admin yang dapat membuka halaman Pengaturan — ini sudah berlaku secara bawaan;
- gunakan kunci sandbox selama pengembangan;
- pertimbangkan memindahkan kunci ke penyimpanan rahasia terpisah bila aplikasi dipakai lebih dari satu toko.

Kunci REST API adalah pengecualian: yang tersimpan hanyalah hash-nya, jadi membaca basis data tidak memberikan kunci yang dapat dipakai.
