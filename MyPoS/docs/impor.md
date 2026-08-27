# Impor data master dari Excel

Halaman **Produk**, **Kategori**, dan **Pelanggan** masing-masing punya tombol **Impor Excel**. Alurnya sama untuk ketiganya: unduh template, isi, unggah, periksa pratinjau, lalu simpan.

## Alur

**1. Unduh template.** Tombol pertama di dalam dialog menghasilkan berkas `.xlsx` yang sudah berisi judul kolom yang benar, satu baris contoh, dan lembar petunjuk.

![Dialog impor, langkah pertama](screenshots/14-impor-template.png)

**2. Isi berkas.** Hapus baris contoh yang bertanda `CONTOH`, atau biarkan — baris tersebut memang dilewati saat impor.

**3. Unggah.** Berkas dibaca dan diperiksa, **tanpa menulis apa pun** ke basis data.

**4. Periksa pratinjau.** Setiap baris ditampilkan beserta tindakan yang akan diambil — Tambah, Perbarui, Dilewati, atau Bermasalah — lengkap dengan alasannya.

![Pratinjau impor](screenshots/15-impor-pratinjau.png)

**5. Simpan.** Hanya baris yang lolos pemeriksaan yang ditulis. Bila terjadi kegagalan di tengah jalan, seluruh berkas dibatalkan; impor separuh jadi jauh lebih sulit dibereskan daripada impor yang gagal utuh.

Pemisahan langkah 3 dan 5 disengaja. Impor yang langsung menulis membuat satu kesalahan ketik pada berkas berubah menjadi ratusan baris yang harus dibereskan satu per satu.

## Isi template

Setiap template berisi tiga lembar:

| Lembar | Isi |
|---|---|
| **Data** | Tempat mengisi. Baris pertama adalah judul kolom, jangan diubah. |
| **Petunjuk** | Penjelasan setiap kolom, mana yang wajib, dan contoh nilainya. |
| **Referensi** | Tersembunyi. Berisi daftar kategori yang menjadi sumber dropdown pada template produk. |

Judul kolom yang wajib diberi komentar sel berisi keterangan "WAJIB DIISI". Urutan kolom boleh diubah — yang dicocokkan adalah judulnya, bukan posisinya.

## Kolom per jenis data

### Produk

| Kolom | Wajib | Keterangan |
|---|:-:|---|
| Nama Produk | ✓ | Maksimal 200 karakter |
| Barcode | | Harus unik. Dipakai untuk mencocokkan produk yang sudah ada |
| Kategori | ✓ | Nama kategori; tersedia sebagai dropdown |
| Harga Jual | ✓ | Tanpa simbol mata uang |
| Harga Modal | | Kosong dianggap nol |
| Stok | | Kosong dianggap nol |
| Stok Minimum | | Kosong atau 0 berarti memakai ambang bawaan dari Pengaturan |
| Keterangan | | Bebas |
| Aktif | | Ya / Tidak. Kosong dianggap Ya |

### Kategori

| Kolom | Wajib | Keterangan |
|---|:-:|---|
| Nama Kategori | ✓ | Harus unik. Nama yang sudah ada akan dilewati |

### Pelanggan

| Kolom | Wajib | Keterangan |
|---|:-:|---|
| Nama Pelanggan | ✓ | Maksimal 150 karakter |
| Telepon | | Dipakai untuk mencocokkan pelanggan yang sudah ada |
| Email | | Harus mengandung `@` bila diisi |
| Poin Loyalitas | | Kosong dianggap nol |

## Cara data lama dikenali

Impor perlu tahu mana baris yang berarti "data baru" dan mana yang berarti "perbarui yang sudah ada". Kunci pencocokannya:

| Jenis | Kunci pencocokan |
|---|---|
| Produk | Barcode bila diisi; bila kosong, nama produk |
| Kategori | Nama kategori |
| Pelanggan | Telepon bila diisi; bila kosong, email |

Telepon didahulukan daripada email untuk pelanggan toko karena lebih jarang berubah dan lebih jarang dipakai bersama beberapa orang.

## Pilihan saat impor

**Perbarui data yang sudah ada** — aktif secara bawaan. Bila dimatikan, baris yang cocok dengan data lama dilewati, bukan ditimpa. Berguna ketika berkas berisi campuran data lama dan baru, tetapi Anda hanya ingin menambah yang baru.

**Buat kategori yang belum ada** — khusus impor produk, nonaktif secara bawaan. Bila dimatikan, baris dengan kategori tak dikenal ditolak; bila diaktifkan, kategorinya dibuat lebih dulu. Sengaja dibuat nonaktif karena satu salah ketik nama kategori akan diam-diam melahirkan kategori baru.

## Pemeriksaan yang dilakukan

Sebelum apa pun tersimpan, setiap baris diperiksa terhadap:

- kolom wajib yang kosong;
- panjang teks melebihi batas kolom basis data;
- angka negatif pada harga, stok, atau poin;
- alamat email tanpa `@`;
- kategori yang tidak dikenal;
- **duplikat di dalam berkas itu sendiri** — dua baris dengan barcode atau telepon yang sama saling bentrok, dan pesannya menyebutkan nomor baris pasangannya.

Selain galat, ada juga peringatan yang tidak menggagalkan baris — misalnya harga modal yang lebih besar daripada harga jual, atau kategori yang akan dibuat otomatis.

## Format angka

Angka boleh ditulis `15000` maupun `15.000`. Keduanya dibaca sebagai lima belas ribu. Simbol `Rp` di depan angka juga dibuang bila ada, sehingga menyalin nilai dari tempat lain tidak langsung menggagalkan barisnya.

Barcode dan nomor telepon dibaca sebagai teks apa adanya, jadi angka nol di depan tidak hilang dan nomor panjang tidak berubah menjadi notasi ilmiah.

## Batas

Ukuran berkas maksimal 10 MB. Pratinjau menampilkan 200 baris pertama, tetapi seluruh baris tetap ikut diproses saat disimpan.

## Menambah jenis data baru

Buat kelas yang mengimplementasikan `IMasterDataImporter` di `Services/Import/`, daftarkan di `Program.cs`, lalu pasang komponen `ImportDialog` di halamannya:

```razor
@inject MyPoS.Services.Import.PemasokImporter Importer

<MudButton StartIcon="@Icons.Material.Filled.UploadFile" OnClick="() => _importDialog?.Open()">
    Impor Excel
</MudButton>

<ImportDialog @ref="_importDialog" Importer="Importer" OnImported="LoadAsync" />
```

Pembuatan template dan pembacaan berkas ditangani `ExcelImportHelper`, jadi importer baru hanya perlu mendeskripsikan kolomnya serta aturan pemeriksaan dan penyimpanannya.
