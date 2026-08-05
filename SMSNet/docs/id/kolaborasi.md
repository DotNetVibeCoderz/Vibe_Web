# Editor Teks, Komentar & Unggahan

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/collaboration.md)

---

![Forum dengan komentar](../img/forum-komentar.png)

Tiga kemampuan yang dipakai bersama oleh beberapa halaman: **editor teks berformat**,
**utas komentar**, dan **unggah berkas**.

---

## 1. Editor Teks Berformat

![Editor teks](../img/editor-teks.png)

Dipakai pada **Komunikasi Internal** untuk isi topik.

| Kelompok | Isi |
| --- | --- |
| Gaya teks | Tebal, Miring, Garis bawah, Coret |
| Blok | Judul, Kutipan, Kode |
| Daftar | Berpoin, Bernomor |
| Lainnya | Sisipkan tautan, Hapus format |

### Yang perlu diketahui

**Menempel selalu menjadi teks polos.** Menyalin dari Word atau halaman web biasanya
membawa serta tag font, nama kelas, dan warna yang bertabrakan dengan tampilan
aplikasi. Formatnya dibuang; isinya tetap.

**Tautan divalidasi.** Alamat tanpa skema dianggap `https://`, dan hanya `http`/`https`
yang diterima.

**Editor ini bagian dari aplikasi, bukan pustaka dari CDN.** Aplikasi memang memuat
Tailwind dan Chart.js dari CDN — halaman masih terbaca tanpa keduanya. Editor yang
hilang saat jaringan bermasalah berarti seseorang kehilangan tulisannya, jadi editor
dibuat sendiri (~150 baris) dan ikut terpasang bersama aplikasi.

### Isi lama tetap terbaca

Topik yang ditulis sebelum editor ada tersimpan sebagai teks polos dengan baris baru
sungguhan. Saat ditampilkan, isi seperti itu dikenali dan baris-barisnya diubah menjadi
paragraf — tanpa itu seluruh baris akan menempel jadi satu.

### Keamanan

Editor berjalan di peramban, jadi keluarannya **input pengguna seperti yang lain**:
permintaan yang dibuat manual dapat mengirim HTML apa pun tanpa melewati toolbar.
Karena itu isinya **disanitasi saat disimpan**, bukan saat ditampilkan — sehingga
muatan berbahaya tidak pernah sempat tersimpan.

Yang dibuang: `script`, `iframe`, atribut event, atribut `style` dan `class`, serta
skema selain `http`, `https`, dan `mailto`. Semua tautan keluar otomatis memperoleh
`target="_blank"` dan `rel="noopener noreferrer"`.

---

## 2. Utas Komentar

Tersedia pada:

| Halaman | Yang dikomentari |
| --- | --- |
| **Komunikasi Internal** | Setiap topik diskusi |
| **Evaluasi Kinerja** | Setiap indikator (KPI) |

Setiap komentar dapat memuat teks, **emoji** (20 pilihan cepat), dan **lampiran** —
gambar tampil sebagai pratinjau, berkas lain sebagai tautan unduh beserta ukurannya.

### Siapa yang boleh menghapus

> **Sebuah komentar hanya dapat dihapus oleh penulisnya sendiri atau oleh admin.**

Aturan ini ditegakkan di **`CommentService.DeleteAsync`**, bukan sekadar dengan
menyembunyikan tombolnya. Tombol yang disembunyikan hanya lapisan pertama; halaman
yang lupa memeriksa akan menjadi lubang yang tidak terlihat.

Kepemilikan dicocokkan dengan **id akun**, bukan nama tampilan — nama tidak unik dan
dapat diubah pemiliknya.

### Menghapus induknya

Menghapus topik atau indikator ikut menghapus seluruh komentarnya beserta berkas
lampirannya. Utas ditandai dengan pasangan (jenis, id) dan bukan foreign key, sehingga
basis data tidak dapat melakukan cascade sendiri — tanpa penghapusan eksplisit, catatan
berikutnya yang memakai ulang id tersebut akan mewarisi diskusi milik orang lain.

---

## 3. Unggah Berkas

![Unggah dokumen](../img/dokumen-unggah.png)

Dipakai pada **Dokumen Digital** dan pada lampiran komentar.

Pada Dokumen Digital tersedia dua mode:

| Mode | Kapan dipakai |
| --- | --- |
| **Unggah berkas** | Sekolah menyimpan sendiri berkasnya |
| **Tautan** | Dokumen ada di layanan lain; sekolah hanya mencatat alamatnya |

Daftar dokumen menandai keduanya dengan ikon berbeda, dan menghapus catatan hanya
menghapus berkas fisik bila memang aplikasi ini yang menyimpannya. Dialog konfirmasinya
menyebutkan mana yang berlaku.

### Batasan

| Pengaturan | Bawaan | Kunci appsettings |
| --- | --- | --- |
| Ukuran maksimal | 15 MB | `Uploads:MaxFileSizeBytes` |
| Lampiran per komentar | 5 | `Uploads:MaxFilesPerItem` |
| Tipe yang diizinkan | pdf, doc(x), xls(x), ppt(x), txt, csv, md, rtf, odt, gambar, zip | `Uploads:AllowedExtensions` |

### Nama berkas tidak pernah dipercaya

Nama dari peramban **tidak pernah menyentuh sistem berkas**. Nama itu dapat memuat
path traversal (`../../appsettings.json`), dan dua pengguna yang mengunggah
`rapor.pdf` akan saling menimpa. Nama simpan dibuat sendiri oleh aplikasi; hanya
ekstensi yang lolos daftar izin yang dibawa. Nama asli tetap disimpan terpisah untuk
ditampilkan.

---

## Pemecahan Masalah

| Gejala | Sebab | Solusi |
| --- | --- | --- |
| Toolbar editor tidak berfungsi | `editor.js` gagal dimuat | Periksa konsol peramban; berkas ada di `wwwroot/editor.js` |
| Format hilang setelah disimpan | Tag itu di luar daftar izin sanitasi | Wajar — lihat daftar tag yang diizinkan di atas |
| Tombol hapus komentar tidak muncul | Bukan penulisnya dan bukan admin | Perilaku yang diharapkan |
| Unggahan ditolak | Ekstensi di luar daftar izin | Tambahkan pada `Uploads:AllowedExtensions` bila memang perlu |
| Berkas terunggah tapi 404 saat dibuka | Berkas dihapus manual dari `wwwroot/uploads` | Unggah ulang lewat tombol Ubah |
