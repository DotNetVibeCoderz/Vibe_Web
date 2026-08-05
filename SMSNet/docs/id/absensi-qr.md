# Absensi QR & Kartu Ber-QR

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/qr-attendance.md)

---

![Kartu ber-QR](../img/qr-cards.png)

Setiap siswa dan guru memperoleh kartu ber-QR. Kartu yang sama dipakai sebagai
identitas **dan** sebagai alat absensi — tidak perlu perangkat tambahan selain
kamera ponsel atau pemindai genggam biasa.

---

## Alur Singkat

```
1. Terbitkan kode QR   →  Master Data → Kartu Ber-QR → "Terbitkan N kode QR"
2. Cetak kartunya      →  pilih siswa → Cetak
3. Pakai untuk absen   →  Akademik → Absensi QR → pindai
```

---

## 1. Menerbitkan Kode

Buka **Master Data → Kartu Ber-QR**. Bila ada siswa atau guru yang belum memiliki
kode, sebuah tombol muncul: **"Terbitkan N kode QR"**. Satu klik memberi kode kepada
semua yang belum punya.

Bentuk kodenya:

```
SIS-000007-K4M9
│    │      └── 4 karakter acak
│    └───────── nomor induk
└────────────── SIS untuk siswa, GUR untuk guru
```

**Mengapa ada bagian acak?** Agar seorang siswa tidak dapat menebak kode temannya
hanya dari nomor induk. Tanpa itu, siapa pun yang melihat satu kartu dapat menyusun
kode seluruh angkatan.

Huruf **I**, **O**, angka **0**, dan **1** sengaja tidak dipakai — keempatnya paling
sering salah dibaca ketika kode diketik manual.

### Menerbitkan Ulang

Tombol ⟳ pada setiap baris menerbitkan kode baru. **Kode lama langsung berhenti
berlaku**, jadi kartu yang hilang tidak dapat dipakai orang lain. Cetak ulang
kartunya setelah menerbitkan ulang.

---

## 2. Mencetak Kartu

Centang nama pada tabel, lalu tekan **Cetak**. Pratinjau di bawah tabel adalah
persis yang akan keluar dari pencetak.

Ukuran kartu mengikuti standar **ID-1 (85,6 × 54 mm)** — sama seperti KTP dan kartu
ATM, sehingga muat di dompet dan di *holder* tanda pengenal biasa. Pada kertas A4
tercetak dua kartu per baris.

Saat mencetak, sidebar dan tombol otomatis disembunyikan; hanya lembar kartu yang
masuk ke pencetak.

> **Catatan:** aktifkan opsi "Background graphics" pada dialog cetak peramban agar
> garis warna di tepi kartu ikut tercetak.

---

## 3. Menyunting Template Kartu

![Template kartu](../img/qr-template.png)

Tata letak kartu adalah **berkas HTML yang dapat disunting**, dengan dua cara:

| Cara | Lokasi | Kapan dipakai |
| --- | --- | --- |
| **Berkas** | `wwwroot/templates/kartu-siswa.html` dan `kartu-guru.html` | Saat punya akses ke server |
| **Antarmuka** | Master Data → Kartu Ber-QR → tab **Template** | Saat tidak punya akses server |

Pengaturan yang disimpan lewat antarmuka **menimpa** berkas. Tombol **Hapus
Penimpaan** mengembalikannya ke berkas bawaan.

### Placeholder

| Token | Isi |
| --- | --- |
| `{{NAMA}}` | Nama lengkap |
| `{{KELAS}}` | Kelas (siswa) atau mata pelajaran (guru) |
| `{{NIS}}` | Nomor induk |
| `{{GENDER}}` | Laki-laki / Perempuan |
| `{{WALI}}` | Nama wali (siswa) atau email (guru) |
| `{{TELEPON}}` | Nomor telepon |
| `{{KODE}}` | Kode QR dalam bentuk teks |
| `{{QR}}` | Gambar QR — pakai di dalam atribut `src` |
| `{{SEKOLAH}}` | Nama sekolah |
| `{{TAHUN_AJARAN}}` | Tahun ajaran berjalan |
| `{{STATUS}}` | Aktif / Tidak aktif |

Pratinjau di sebelah kanan editor diperbarui saat mengetik, memakai data pertama
pada daftar sebagai contoh.

### Batasan keamanan

Template disanitasi sebelum dirender. Tag `script`, `iframe`, dan atribut event
dibuang — termasuk bila tertempel tanpa sengaja dari potongan kode yang disalin.
Gaya inline (`style`) tetap diizinkan karena tata letak kartu memang memerlukannya.

---

## 4. Absensi dengan QR

![Absensi QR](../img/qr-attendance.png)

Buka **Akademik → Absensi QR**. Tersedia dua mode.

### Mode Kamera

Menyalakan kamera perangkat dan memindai otomatis. Cocok untuk ponsel atau tablet
yang dipegang petugas di gerbang.

- Memakai **BarcodeDetector** bawaan peramban bila tersedia (Chrome, Edge, Android),
  dan jatuh ke **jsQR** pada peramban lain (Safari, Firefox). Mesin yang sedang
  dipakai ditampilkan di bawah tombol.
- Berbunyi dan bergetar saat berhasil membaca, sehingga petugas tidak perlu menatap
  layar terus-menerus.
- Satu kartu yang tertahan di depan kamera **tidak** menghasilkan puluhan pemindaian —
  kode yang sama diabaikan selama 2,5 detik.
- Kamera dilepas otomatis saat meninggalkan halaman, sehingga lampu indikatornya
  tidak menyala terus.

> Akses kamera memerlukan **HTTPS**, kecuali saat diakses lewat `localhost`.
> Bila kamera tidak dapat dibuka, pesannya selalu menunjuk ke mode kedua.

### Mode Scanner / Ketik Manual

Satu kolom isian yang melayani dua hal sekaligus:

- **Pemindai genggam** (USB/Bluetooth) bekerja seperti papan ketik — arahkan ke
  kartu, kodenya terisi sendiri lalu tercatat otomatis karena alat mengirim Enter.
  Kolom isian ini menjaga fokusnya sendiri, jadi petugas tidak perlu mengkliknya
  setiap kali.
- **Ketik manual** untuk kartu yang rusak atau tertinggal. Huruf besar-kecil, spasi,
  dan tanda hubung tidak berpengaruh — `sis 000007 k4m9` dikenali sama dengan
  `SIS-000007-K4M9`.

### Tiga Kemungkinan Hasil

| Hasil | Warna | Arti |
| --- | --- | --- |
| **Tercatat** | Hijau | Kehadiran berhasil disimpan, lengkap dengan jamnya |
| **Sudah tercatat** | Kuning | Kartu dipindai dua kali — jam kehadiran pertama ditampilkan |
| **Tidak dikenali** | Merah | Kode tidak cocok dengan siapa pun |

**Pemindaian ganda tidak membuat catatan baru.** Di gerbang sekolah kartu akan sering
tertempel dua kali; bila setiap pemindaian membuat baris baru, seluruh persentase
kehadiran di aplikasi menjadi salah.

---

## 5. Daftar Kehadiran Hari Ini

Panel kanan menampilkan semua yang sudah hadir hari ini, terbaru di atas, lengkap
dengan **jam kehadiran**. Baris yang baru masuk disorot sesaat agar petugas melihat
catatannya benar-benar tersimpan.

Tersedia:

- **Pencarian nama** — mengetik langsung menyaring daftar
- **Saringan peran** — Siswa, Guru, atau semua
- **Pembatalan** — tombol hapus pada tiap baris, untuk kartu yang dipindai orang
  yang salah. Setelah dibatalkan, kartu yang sama dapat dipindai ulang.

Setiap pemindaian dan pembatalan tercatat di **Audit Trail**.

---

## Integrasi dengan Absensi Manual

Absensi QR menulis ke tabel yang sama dengan **Absensi Manual**, dengan kolom
Metode berisi `QR`. Artinya:

- Rekap kehadiran, laporan akademik, dan Portal Orang Tua langsung ikut memperhitungkannya
- Asisten Pak Dedi dapat menjawab pertanyaan tentangnya lewat fungsi `rekap_absensi`
- Halaman Absensi Manual tetap dapat dipakai untuk mencatat sakit dan izin, yang
  memang tidak melibatkan pemindaian kartu

---

## Pemecahan Masalah

| Gejala | Sebab | Solusi |
| --- | --- | --- |
| Tombol "Terbitkan kode" tidak muncul | Semua sudah punya kode | Wajar — tidak perlu tindakan |
| Kartu tercetak tanpa warna | Peramban membuang latar saat mencetak | Aktifkan "Background graphics" di dialog cetak |
| "Kode tidak dikenali" padahal kartu asli | Kode sudah diterbitkan ulang | Cetak ulang kartunya |
| Kamera tidak mau menyala | Bukan HTTPS, izin ditolak, atau dipakai aplikasi lain | Pesan galat menyebut sebabnya; gunakan mode Scanner / Ketik |
| Pemindai genggam mengetik ke tempat lain | Fokus berpindah | Kolom kode menjaga fokusnya sendiri — klik sekali pada kolom itu |
| QR tidak terbaca kamera | Kartu tercetak terlalu kecil atau buram | Cetak pada 100% (jangan "fit to page") |
