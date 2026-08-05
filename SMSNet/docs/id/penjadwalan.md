# Penjadwalan Otomatis

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/scheduling.md)

---

![Hasil penjadwalan](../img/schedule-result.png)

Menyusun jadwal pelajaran satu minggu penuh untuk seluruh kelas sekaligus, tanpa
seorang guru pun terjadwal di dua kelas pada jam yang sama.

Halaman ini **mensimulasikan** jadwal. Hasilnya masih dapat disunting sepenuhnya,
dan tidak menyentuh basis data sampai Anda menekan **Simpan ke Jadwal**.

**Akses:** Admin dan Guru. Masuk lewat **Akademik → Kurikulum & Jadwal → Penjadwalan
Otomatis**, atau langsung ke `/academic/schedule-generator`.

---

## Alur Singkat

```
1. Tentukan jam per minggu  →  tabel "Jam per Minggu"
2. Atur hari & slot          →  panel "Pengaturan"
3. Susun Jadwal              →  simulasi muncul dalam hitungan detik
4. Perbaiki bila perlu       →  klik sel mana pun
5. Simpan ke Jadwal          →  menggantikan jadwal lama
```

---

## 1. Menentukan Jam per Minggu

![Pengaturan penjadwalan](../img/schedule-setup.png)

Tabel di kiri berisi setiap mata pelajaran beserta guru yang mengampunya. Kolom
**Jam / Minggu** menentukan berapa jam pelajaran itu muncul di **setiap** kelas
dalam seminggu.

Nilai awalnya diambil dari kolom **SKS** pada Master Data → Mata Pelajaran, jadi
biasanya tidak perlu diubah sama sekali.

### Mata pelajaran tanpa pengampu

Mata pelajaran yang belum punya guru ditandai **"belum ada pengampu"**, kolom jamnya
dinonaktifkan, dan pelajaran itu **dilewati** — sebuah catatan di bawah tabel
menyebutkan nama-namanya.

Ini disengaja. Pelajaran tanpa guru mustahil ditempatkan; bila tetap diikutkan,
seluruh penyusunan akan gagal karena satu baris yang tidak dapat Anda perbaiki dari
halaman ini. Tetapkan pengampunya di **Master Data → Guru**, lalu muat ulang halaman.

### Meteran kapasitas

Di bawah tabel tertera:

```
Total 14 jam per kelas, dari 40 slot tersedia.
```

Meteran berubah **merah** bila total jam melebihi slot yang tersedia. Bila itu
terjadi, kurangi jamnya atau tambah hari/slot — penyusunan pasti gagal.

---

## 2. Pengaturan

| Pengaturan | Pilihan | Keterangan |
| --- | --- | --- |
| **Hari sekolah** | 5 hari (Senin–Jumat) atau 6 hari (Senin–Sabtu) | |
| **Slot per hari** | 6, 7, atau 8 jam pelajaran | Jamnya mengikuti daftar baku, termasuk jeda istirahat |
| **Maksimal jam mengajar per guru per hari** | angka, bawaan 6 | Mencegah seorang guru mengajar seharian penuh |
| **Hindari mapel sama dua kali sehari** | centang, bawaan aktif | Menyebar pelajaran ke sepanjang minggu |

> Opsi "hindari mapel sama dua kali sehari" **dilonggarkan otomatis** bila jadwal
> tidak dapat tersusun tanpanya. Lebih baik menghasilkan jadwal yang sedikit padat
> daripada tidak menghasilkan apa pun.

---

## 3. Menyusun

Tekan **Susun Jadwal**. Penyusunan berjalan di luar antarmuka, jadi halaman tetap
responsif; untuk sekolah dengan 8 kelas biasanya selesai di bawah 300 ms.

Kartu ringkasan menampilkan jumlah jam yang tersusun, berapa kali percobaan
dilakukan, dan lamanya proses.

### Cara kerjanya

Penyusunan jadwal adalah **masalah pemenuhan batasan** (*constraint satisfaction*),
bukan sekadar pengacakan. Mesinnya memakai teknik baku:

| Teknik | Perannya |
| --- | --- |
| **Backtracking** | Menempatkan satu jam pelajaran, dan mundur bila jalan buntu |
| **MRV** (*minimum remaining values*) | Selalu mengerjakan pelajaran dengan pilihan tersisa paling sedikit — kegagalan ditemukan lebih awal |
| **Forward checking** | Membatalkan cabang begitu ada pelajaran yang kehilangan seluruh pilihannya |
| **Randomized restart** | Mengulang dari titik acak baru bila satu percobaan mentok; hingga 40 kali dalam anggaran 8 detik |

Batasan yang dijaga keras:

- satu kelas hanya punya satu pelajaran per slot;
- satu guru hanya mengajar satu kelas per slot;
- guru hanya diberi mata pelajaran yang memang diampunya;
- beban harian seorang guru tidak melewati batas yang Anda tetapkan.

### Bila tidak berhasil

Permintaan yang mustahil **ditolak sebelum penyusunan dimulai**, dengan alasan yang
langsung dapat ditindaklanjuti — misalnya total jam melebihi slot, atau jumlah guru
tidak cukup untuk suatu mata pelajaran.

Bila jadwal hanya tersusun sebagian, hasil terbaik tetap ditampilkan bersama kartu
**"Tidak Dapat Ditempatkan"** yang menyebut satu per satu pelajaran yang gagal.
Jauh lebih berguna daripada halaman kosong.

---

## 4. Menyunting Hasil

Grid menampilkan satu kelas dalam satu waktu; pemilih di kanan atas berpindah kelas.

**Klik sel mana pun** untuk membukanya:

![Penyunting sel](../img/schedule-editor.png)

- Daftar guru hanya berisi yang benar-benar mengampu mata pelajaran terpilih.
- Guru yang sedang mengajar di kelas lain pada jam itu diberi keterangan
  **"(sedang mengajar kelas lain)"**.
- Memilih guru yang sedang sibuk memunculkan peringatan **sebelum** Anda menerapkannya.
- Pilih **"— kosongkan —"** untuk mengosongkan sel.

Anda tetap **boleh** membuat bentrok — kadang memang perlu, misalnya untuk menyusun
ulang beberapa sel berturut-turut. Yang dijaga aplikasi adalah jadwal bentrok tidak
bisa ikut tersimpan.

---

## 5. Bentrok

![Bentrok](../img/schedule-conflict.png)

Setiap perubahan memicu pemeriksaan ulang seluruh papan. Temuan dibagi dua:

| Jenis | Lencana | Akibat |
| --- | --- | --- |
| **Bentrok** | merah | Menghalangi penyimpanan |
| **Catatan** | kuning | Hanya pemberitahuan |

Yang menghalangi penyimpanan:

- satu guru di dua kelas pada jam yang sama;
- satu kelas dengan dua pelajaran pada jam yang sama;
- guru diberi mata pelajaran yang tidak diampunya;
- pelajaran tanpa guru;
- beban harian guru melewati batas.

Sekadar catatan (tidak menghalangi):

- jumlah jam suatu mata pelajaran tidak sama dengan yang diminta di halaman awal.

Sel yang bermasalah **diberi garis merah** pada grid, jadi tidak perlu mencocokkan
daftar temuan dengan tabel secara manual. Tombol **Simpan ke Jadwal** dinonaktifkan
selama masih ada bentrok.

---

## 6. Menyimpan

**Simpan ke Jadwal** meminta konfirmasi lebih dulu, dan menyebut angkanya:

> Seluruh 12 entri jadwal yang tersimpan akan diganti dengan 112 entri hasil
> simulasi ini.

**Penyimpanan bersifat menggantikan, bukan menggabungkan.** Jadwal satu minggu adalah
satu kesatuan; menggabungkan minggu baru ke atas yang lama akan meninggalkan
pelajaran-pelajaran yatim yang tidak diminta siapa pun. Karena itu jumlah entri lama
selalu disebutkan sebelum Anda menekan tombolnya.

Setelah tersimpan, jadwal langsung tampil di **Akademik → Kurikulum & Jadwal → tab
Jadwal Pelajaran**, dan ikut terbaca oleh Dashboard Guru, Portal Orang Tua, serta
asisten Pak Dedi. Tindakan ini tercatat di **Audit Trail**.

---

## Pemecahan Masalah

| Gejala | Sebab | Solusi |
| --- | --- | --- |
| Tombol "Susun Jadwal" mati | Belum ada kelas | Tambahkan kelas di Master Data → Kelas |
| Sebuah mapel tidak pernah muncul | Belum ada guru pengampu | Tetapkan pengampu di Master Data → Guru |
| Meteran kapasitas merah | Total jam melebihi slot | Kurangi jam, atau tambah hari/slot |
| "Tidak Dapat Ditempatkan" berisi banyak nama | Guru untuk suatu mapel terlalu sedikit | Tambah pengampu, atau naikkan batas jam per guru per hari |
| Banyak sel kosong | Total jam memang lebih kecil dari slot tersedia | Wajar — tambahkan jam bila ingin lebih padat |
| Tombol Simpan mati | Masih ada bentrok | Perbaiki sel bergaris merah, atau tekan Susun Ulang |
| Hasil berbeda tiap kali disusun | Penyusunan memang diacak | Tekan Susun Ulang sampai memperoleh bentuk yang disukai |
