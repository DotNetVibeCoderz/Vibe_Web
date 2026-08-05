# PLAN.md — Roadmap Pengembangan SMSNet

> Peta jalan pengembangan aplikasi. Untuk status harian lihat [Progress.md](Progress.md).
> Sumber kebutuhan awal: [requirements.md](requirements.md).

**Bahasa:** dokumen ini ditulis dalam Bahasa Indonesia. Ringkasan bahasa Inggris ada di bagian akhir.

---

## 1. Tujuan Produk

SMSNet adalah Sistem Manajemen Sekolah untuk jenjang SMP/SMA di Indonesia. Sasarannya satu:
**seluruh operasional harian sekolah dapat dijalankan dari satu aplikasi**, mulai dari absensi
pagi hari sampai laporan keuangan akhir bulan, dan orang tua dapat memantau anaknya tanpa
harus datang ke sekolah.

Tiga tolok ukur keberhasilan:

| Tolok ukur | Target |
| --- | --- |
| Waktu tata usaha mencatat absensi satu kelas | < 1 menit |
| Orang tua menemukan tagihan SPP anaknya | < 3 klik dari login |
| Pertanyaan umum terjawab tanpa menghubungi staf | via asisten Pak Dedi |

---

## 2. Prinsip Arsitektur

Keputusan yang **tidak** diubah tanpa alasan kuat, karena seluruh kode bersandar padanya:

1. **Satu proyek Blazor Server.** Tidak ada pemisahan API/SPA. Semua halaman interaktif
   (`InteractiveServer` global di `Components/App.razor`).
2. **Tanpa build step frontend.** Tailwind dari CDN, token desain di `wwwroot/app.css`.
   Tidak ada npm, PostCSS, atau bundler. Ini disengaja agar sekolah dapat men-deploy
   tanpa toolchain Node.
3. **Entity pipih.** Relasi disimpan sebagai string tampilan, bukan foreign key
   (`ScheduleItem.Teacher` berisi nama guru). Pengecualian tunggal: tabel chat, yang
   memang butuh cascade delete.
4. **Akses data lewat `IDbContextFactory`.** Setiap halaman membuat `DbContext`
   berumur pendek per operasi. Satu context yang dibagi lintas await pada circuit
   Blazor menghasilkan galat *"A second operation was started on this context"* —
   bug yang pernah ada pada aplikasi ini. Tetap tanpa lapisan repository.
5. **Peran ditegakkan di server, berlapis.** `NavigationRegistry` adalah sumber tunggal
   peta halaman → peran, sehingga menu dan atribut `[Authorize]` tidak pernah berbeda.
   Penegakan berlanjut ke endpoint API dan ke dalam badan fungsi asisten — model tidak
   pernah menjadi penentu siapa melihat apa.
6. **Waktu selalu lewat `SchoolClock`.** `DateTime.Now` mengembalikan jam server yang
   umumnya UTC, dan itu menggeser catatan absensi ke hari yang salah bagi sekolah di
   Indonesia.

---

## 3. Fase Pengembangan

### Fase 0 — Basis Aplikasi ✅ *(selesai sebelum siklus ini)*

Kerangka awal: autentikasi Identity, 40+ halaman CRUD, seeder data contoh,
REST API + Swagger, penyimpanan berkas yang dapat dikonfigurasi.

---

### Fase 1 — Fondasi Desain ✅ *(selesai)*

**Tujuan:** memberi aplikasi identitas visual yang bukan template default.

Arah desain **"Buku Induk"** — bahasa visualnya adalah buku register sekolah:
baris bergaris, angka tabular, dan penanda tab kuning penunjuk posisi.

| Aspek | Keputusan |
| --- | --- |
| Palet | `tinta` #101A2E · `dongker` #1B3A6B (seragam SMP) · `kunyit` #E8A317 · `kapur` #F4F5F7 · `garis` #D8DCE4 · `daun` #2F7D5C |
| Tipografi | Bricolage Grotesque (display) · Public Sans (teks) · IBM Plex Mono (angka/kode) |
| Elemen khas | Penanda tab kuning pada navigasi aktif dan baris tabel yang disorot |
| Tema | Terang/gelap, diputuskan sebelum paint pertama agar tidak berkedip |

**Keluaran:** `wwwroot/app.css`, `Components/App.razor`, `MainLayout`, `NavMenu`,
`NavigationRegistry`, komponen bersama (`PageHeader`, `StatTile`, `Modal`,
`ConfirmDialog`, `EmptyState`, `Toaster`, `Icon`), `SchoolClock`, `ToastService`.

---

### Fase 2 — Asisten "Pak Dedi" ✅ *(selesai)*

**Tujuan:** menjawab pertanyaan tentang data sekolah tanpa membuka menu satu per satu.

| Aspek | Keputusan |
| --- | --- |
| Orkestrasi | Semantic Kernel 1.78 |
| Provider | OpenAI · Anthropic · Google Gemini · Ollama, dipilih dari appsettings |
| Anthropic | Konektor `IChatCompletionService` sendiri di atas SDK resmi Anthropic — Microsoft tidak menyediakan konektor Anthropic, dan paket komunitas tertinggal jauh dari versi SK yang dipakai |
| Fungsi | 24 kernel function dalam 4 plugin: Waktu, Matematika, Web, SekolahData |
| Keamanan | Plugin data memeriksa peran pemanggil; plugin web menolak alamat jaringan internal (SSRF) |
| Rendering | Markdig → sanitasi HtmlSanitizer → pengayaan media (video/audio) |

**Keluaran:** `Services/Assistant/**`, `Components/Pages/Assistant/Chat.razor`,
`wwwroot/chat.css`, `wwwroot/chat.js`, tabel `ChatSession`/`ChatMessage`/`ChatAttachment`.

---

### Fase 3 — Penerapan Desain ke Seluruh Halaman ✅ *(selesai)*

**Masalah:** Fase 1 mengganti kerangka aplikasi, tetapi isi ~40 halaman masih memakai
markup lama (kelas Tailwind indigo/slate inline). Halaman tampil rapi karena mewarisi
token, tetapi belum memakai komponen baru.

**Ruang lingkup:**

1. Halaman master data (`Master/*`) — pola CRUD bersama: `PageHeader`, `.sms-table`,
   `ConfirmDialog` untuk hapus, toolbar filter, ekspor CSV.
2. Halaman laporan (`Reports/*`, `Analytics/*`) — `StatTile`, kanvas Chart.js,
   gauge memakai variabel `--pct`.
3. Halaman akademik, guru, orang tua, administrasi.
4. Halaman autentikasi (`Auth/*`) — tata letak terpisah tanpa sidebar.

**Definisi selesai:** tidak ada lagi kelas warna mentah (`indigo-600`, `slate-300`)
di berkas `.razor`; semua tabel memakai `.sms-table`; semua hapus melewati konfirmasi.

---

### Fase 4 — Metode Pembayaran ✅ *(selesai)*

**Tujuan:** memenuhi kebutuhan "mendukung berbagai metode pembayaran yang dapat
dikonfigurasi dari appsetting atau UI".

| Komponen | Rencana |
| --- | --- |
| Abstraksi | `IPaymentGateway` + `IPaymentGatewayRegistry` |
| Penyedia | Midtrans · Xendit · Stripe · QRIS statis · Transfer manual |
| Konfigurasi | Bagian `Payments` di appsettings **dan** halaman admin `/admin/payment-gateways` |
| Entitas | `PaymentGatewayConfig`, `PaymentTransaction` (status, referensi eksternal, kanal) |
| Alur | Halaman E-Payment membuat transaksi → memilih kanal → petunjuk pembayaran → konfirmasi |

**Catatan jujur:** tanpa kredensial sungguhan, implementasi penyedia berjalan dalam
**mode simulasi** yang menghasilkan referensi transaksi lokal. Titik integrasi HTTP
dituliskan lengkap dan ditandai `LIVE CALL`, tetapi belum pernah diuji terhadap akun
penyedia sungguhan.

**Yang belum ada:** callback/webhook dari penyedia belum ditangani, sehingga status
transaksi Midtrans/Xendit/Stripe tidak berubah otomatis saat pembayaran selesai di sisi
penyedia. Konfirmasi masih manual. Ini masuk Fase 8.

---

### Fase 5 — Pengetatan RBAC & Perbaikan Bug ✅ *(selesai)*

Temuan audit yang harus ditangani:

| # | Temuan | Dampak |
| --- | --- | --- |
| 1 | `api/*` controller tanpa `[Authorize]` | Seluruh data siswa/guru terbuka tanpa login |
| 2 | Peran `siswa` dan `orangtua` tidak punya halaman yang benar-benar dapat dibuka | Login berhasil tetapi aplikasi kosong |
| 3 | Tombol Hapus langsung menghapus tanpa konfirmasi | Kehilangan data karena salah klik |
| 4 | `DbContext` dipakai lintas await pada circuit Blazor | "A second operation was started on this context" |
| 5 | Audit trail tidak pernah ditulis dari aksi nyata | Fitur ada tetapi kosong |
| 6 | `Microsoft.OpenApi` 2.4.1 & `SQLitePCLRaw` 2.1.11 punya advisory | Kerentanan diketahui |
| 7 | Logout tidak memvalidasi antiforgery | CSRF logout |
| 8 | `DateTime.Now` dipakai langsung di banyak halaman | Salah hari bila server UTC |

---

### Fase 6 — Dokumentasi ✅ *(selesai)*

Folder `docs/` dwibahasa:

```
docs/
  id/  arsitektur · instalasi · fitur · rbac · pembayaran · asisten · api · deployment
  en/  (padanan bahasa Inggris)
  img/ tangkapan layar
```

README diperbarui dengan tangkapan layar dan penunjuk ke `docs/`.

---

### Fase 7 — Absensi QR & Kartu Ber-QR ✅ *(selesai)*

**Tujuan:** menghapus antrean absensi manual di gerbang sekolah.

| Aspek | Keputusan |
| --- | --- |
| Kode | Disimpan pada entitas (`Student.QrCode`, `Teacher.QrCode`), bukan diturunkan dari Id — supaya kartu hilang dapat diterbitkan ulang dan kode lama langsung mati |
| Bentuk | `SIS-000007-K4M9` — sufiks acak mencegah kode teman ditebak dari nomor induk; huruf I/O dan angka 0/1 dihindari karena sering salah baca |
| Pembangkitan QR | QRCoder, keluaran **SVG** sebagai data URI — tajam di ukuran cetak apa pun dan tanpa permintaan HTTP tambahan |
| Template kartu | Berkas HTML di `wwwroot/templates/`, dapat ditimpa lewat UI admin; disanitasi sebelum dirender |
| Ukuran kartu | ID-1 (85,6 × 54 mm), dua kartu per baris pada A4 |
| Mode pindai | Kamera (BarcodeDetector → jsQR) **dan** pemindai genggam / ketik manual |
| Anti-duplikat | Pindaian kedua pada hari yang sama tidak membuat baris baru — melaporkan jam kehadiran pertama |

**Keluaran:** `Services/Attendance/**`, `Components/Pages/Master/StudentCards.razor`,
`Components/Pages/Academic/QrAttendance.razor`, `wwwroot/cards.css`, `wwwroot/qrscan.js`,
`wwwroot/templates/*.html`, entitas `CardTemplate` + kolom `QrCode`.

**Perbaikan menyertai:** seluruh aplikasi kini berjalan pada culture **id-ID**, sehingga
tanggal tampil "Rabu, 05 Agustus 2026" dan nominal "Rp2.100.000". Ekspor CSV memakai
format angka invariant agar koma desimal tidak memecah kolom.

---

### Fase 8 — Penjadwalan Otomatis & Uji LLM Nyata ✅ *(selesai)*

**Tujuan:** menghapus pekerjaan menyusun jadwal dengan tangan, dan membuktikan asisten
bekerja terhadap API model yang sungguhan — bukan hanya terhadap tiruan.

| Aspek | Keputusan |
| --- | --- |
| Algoritma | **Constraint satisfaction**, bukan pengacakan: backtracking + MRV + forward checking, dengan randomized restart (maks. 40 percobaan dalam anggaran 8 detik) |
| Letak | `Services/Scheduling/` sebagai layanan tersendiri — satu-satunya logika berat di aplikasi ini, dan satu-satunya yang layak diuji tanpa antarmuka |
| Penolakan dini | Permintaan mustahil (jam melebihi slot, mapel tanpa guru, guru tidak cukup) ditolak **sebelum** penyusunan, dengan alasan yang dapat ditindaklanjuti |
| Kegagalan sebagian | Tetap menampilkan papan terbaik + daftar pelajaran yang gagal ditempatkan — lebih berguna daripada halaman kosong |
| Hasil = simulasi | Tidak menyentuh basis data sampai dikonfirmasi; penyuntingan per sel dengan validasi ulang menyeluruh setiap perubahan |
| Bentrok | Menghalangi penyimpanan dan ditandai merah pada grid; "catatan" (jumlah jam meleset) tidak menghalangi |
| Penyimpanan | **Mengganti, bukan menggabungkan** — jadwal seminggu adalah satu kesatuan; jumlah entri lama selalu disebut sebelum dikonfirmasi |
| Mapel tanpa guru | Dilewati dan disebutkan namanya, bukan digagalkan — kolom jamnya nonaktif, jadi pengguna tidak akan bisa memperbaikinya dari halaman itu |

**Keluaran:** `Services/Scheduling/TimetableModels.cs`, `TimetableGenerator.cs`,
`TimetableValidator.cs`, `Components/Pages/Academic/ScheduleGenerator.razor`, tombol
masuk pada `Curriculum.razor`, gaya `.jadwal-sel` pada `wwwroot/app.css`.

**Menyertai:**

- **19 contoh prompt** dalam 6 kelompok pada layar sambutan asisten. Kelompok terakhir
  sengaja memaksa lebih dari satu fungsi dipanggil sekaligus — kemampuan yang paling
  sulit ditebak pengguna baru dari kolom isian kosong.
- **Penyedia Azure OpenAI** ditambahkan sebagai penyedia kelima. Azure bukan sekadar
  OpenAI dengan endpoint lain: URL-nya memuat nama deployment dan `api-version`.

**Dua bug produksi ditemukan hanya karena diuji terhadap model sungguhan:**

1. **Proyeksi EF Core.** Delapan fungsi `SekolahDataPlugin` memformat angka dan tanggal
   di dalam `Select()`. EF Core tidak dapat menerjemahkan `ToString(format)` ke SQL,
   sehingga setiap pemanggilan gagal saat dijalankan. Model melaporkannya kepada
   pengguna sebagai "sistem sedang mengalami kendala teknis" — persis jenis kegagalan
   yang tidak akan pernah terlihat dari tiruan yang mengembalikan JSON siap pakai.
2. **Model penalaran menolak parameter sampling.** Keluarga gpt-5 dan o1/o3/o4 menolak
   `max_tokens` serta hanya menerima nilai bawaan `temperature`/`top_p`, sedangkan
   Semantic Kernel mengirim nama parameter yang lama → HTTP 400.

---

### Fase 9 — Kolaborasi, Lookup & Unggahan ✅ *(selesai)*

**Tujuan:** menutup tujuh keluhan konkret pada halaman yang sudah ada — bukan fitur
baru, melainkan yang sudah ada tapi belum layak dipakai.

| Aspek | Keputusan |
| --- | --- |
| Editor teks | Ditulis sendiri (~150 baris) alih-alih pustaka CDN. Tailwind dan Chart.js boleh hilang — halaman tetap terbaca; editor yang hilang berarti tulisan seseorang lenyap |
| Sanitasi | Dilakukan **saat menyimpan**, bukan saat menampilkan — muatan berbahaya tidak pernah sempat tersimpan |
| Utas komentar | Dialamatkan (jenis, id), bukan FK per tabel induk. Halaman ikut serta dengan mengirim dua nilai, tanpa migrasi baru tiap kali |
| Hak hapus komentar | Ditegakkan di `CommentService.DeleteAsync`, bukan sekadar menyembunyikan tombol. Kepemilikan dicocokkan dengan **id akun**, bukan nama |
| Lookup | `<datalist>`, bukan `<select>` — nama bisa mendahului master data, dan select tidak bisa menyatakan "salah satu ini, atau yang lain" |
| Kelas pada nilai | **Didenormalisasi ke baris nilai.** Nilai adalah catatan sebuah momen; membacanya dari data siswa akan mengubah hasil tahun lalu saat siswa naik kelas |
| Multi-kelas pada tugas | Daftar dipisah koma, kosong = semua kelas — mengikuti pola relasi-sebagai-string di seluruh skema ini |
| Nama berkas | Tidak pernah menyentuh sistem berkas: dapat memuat path traversal, dan dua `rapor.pdf` akan saling menimpa |
| Satuan capaian KPI | Kolom eksplisit, bukan tebakan — lihat bug di bawah |

**Keluaran:** `Models/CollaborationEntities.cs`, `Services/UploadService.cs`,
`Services/CommentService.cs`, `Services/HtmlContentSanitizer.cs`,
`Components/Shared/{CommentThread,RichTextEditor,LookupInput,LinkButton}.razor`,
`wwwroot/editor.js`, ditambah perubahan pada tujuh halaman dan `CrudPageBase`
(hook `OnFormOpened`, `ConfirmDeleteAsync` menjadi virtual).

**Tiga bug ditemukan dan diperbaiki:**

1. **Razor membaca `Rp@bill.Amount` sebagai alamat email.** Pola *huruf, `@`, huruf*
   adalah literal email bagi Razor, sehingga baris itu **dikirim apa adanya sebagai
   teks** — orang tua melihat tulisan `Rp@bill.Amount.ToString("N0")` di halaman
   tagihannya, bukan angka. Diperbaiki dengan helper `Money()`, dan seluruh markup
   disapu untuk pola yang sama (hanya satu kejadian).
2. **Bilah kemajuan KPI salah membaca skala.** Capaian "4.2" pada skala 0–5 digambar
   sebagai 4%, bukan 84%. Sebabnya nilai dibaca sebagai persen tanpa tahu satuannya —
   karena itu satuan kini disimpan, bukan ditebak.
3. **Caret hilang setelah menekan tombol toolbar.** Mengklik tombol memindahkan fokus
   dari editor, dan mengembalikan fokus saja menempatkan caret di posisi nol — teks
   yang diketik sesudahnya mendarat di awal dokumen atau hilang. Diperbaiki dengan
   `preventDefault` pada `mousedown` (agar fokus tidak pernah lepas) ditambah
   penyimpanan/pemulihan *range* untuk dialog tautan, yang memang merebut fokus.

---

### Fase 10 — Setelah Itu (belum dijadwalkan)

- **Migrasi EF.** Saat ini skema dibuat dengan `EnsureCreated()` sehingga setiap
  perubahan entitas menuntut penghapusan basis data. Tidak layak untuk produksi.
- **Webhook penyedia pembayaran** beserta verifikasi tanda tangannya, agar status
  transaksi mutakhir otomatis tanpa konfirmasi manual.
- **Membatasi halaman pendaftaran.** Saat ini `/auth/register` terbuka untuk umum dan
  memungkinkan siapa pun mendaftar sebagai `admin`.
- **Reset kata sandi lewat email** menggantikan alur langsung yang ada sekarang.
- **Normalisasi entitas.** Mengganti relasi berbasis nama dengan foreign key sungguhan.
  Perubahan besar: menyentuh seluruh halaman dan seluruh seeder.
- **Notifikasi real-time** (SignalR) untuk pengumuman dan tagihan.
- **Impor/ekspor massal** data siswa dari Excel.
- **Uji otomatis.** Belum ada proyek uji sama sekali.
- **Penyimpanan awan.** `AzureBlobStorage` dan `AwsS3Storage` masih stub yang
  mengembalikan path palsu.

---

## 4. Status Pengerjaan

Fase 1–9 seluruhnya selesai dan terverifikasi dengan menjalankan aplikasi.
Rincian bukti per fase ada di [Progress.md](Progress.md).

```
Fase 1 ✅ desain  →  Fase 2 ✅ asisten  →  Fase 3 ✅ 36 halaman
                                              ↓
Fase 6 ✅ dokumentasi  ←  Fase 5 ✅ RBAC & bug  ←  Fase 4 ✅ pembayaran
       ↓
Fase 7 ✅ absensi QR & kartu ber-QR  →  Fase 8 ✅ penjadwalan otomatis & uji LLM nyata
                                              ↓
                                        Fase 9 ✅ kolaborasi, lookup & unggahan
```

Dokumentasi sengaja dikerjakan terakhir agar tangkapan layarnya tidak basi.

Yang tersisa seluruhnya ada di Fase 10 dan belum dijadwalkan. Yang paling mendesak
di antaranya adalah **migrasi EF Core**: selama skema masih dibuat dengan
`EnsureCreated()`, setiap perubahan entitas menuntut penghapusan basis data — tidak
dapat diterima begitu sekolah mulai memasukkan data sungguhan. Fase 9 saja sudah
menuntut satu penghapusan lagi, untuk enam kolom baru.

---

## 5. English Summary

SMSNet is a school management system for Indonesian secondary schools, built as a
single Blazor Server project with no frontend build step.

**Phases 1 through 9 are complete**: a "school register" design system (navy/turmeric
palette, Bricolage Grotesque + Public Sans, light/dark, responsive shell, role-aware
navigation); the "Pak Dedi" assistant (Semantic Kernel with OpenAI/Azure OpenAI/
Anthropic/Google/Ollama providers, a hand-written Anthropic connector, 24 kernel
functions, multi-session chat with attachments, sanitised Markdown); the design system
applied across all 36 pages on a shared `CrudPageBase`; five configurable payment
channels; the RBAC and bug hardening; bilingual documentation; QR attendance with
printable QR cards whose layout is an editable HTML template; automatic timetabling
built on a constraint solver; and a collaboration layer — a hand-written rich-text
editor, comment threads with attachments and emoji, master-data lookups, and real file
uploads.

Everything was verified by running the application and driving it with Playwright —
a clean build, all 39 routes rendering without console errors, 72/72 RBAC probes across
four roles, and plugin-level role enforcement demonstrated by having one restricted
function refuse a student and answer an admin. Evidence per phase is in
[Progress.md](Progress.md).

Phase 8 additionally verified the assistant against **two real model APIs** (DeepSeek
via the OpenAI-compatible endpoint, and Azure OpenAI on gpt-5-mini). That exercise paid
for itself immediately: it uncovered two production bugs that no mock could have
surfaced — EF Core failing to translate `ToString(format)` inside `Select()` projections
across eight data functions, and reasoning models rejecting the sampling parameters
Semantic Kernel emits.

Phase 9 closed seven concrete complaints about existing pages and turned up three more
bugs on the way. The sharpest was in the parent portal: Razor reads the pattern
*letter, `@`, letter* as an email literal, so `Rp@bill.Amount.ToString("N0")` was
**emitted verbatim as text** — parents saw that string where the amount should have
been. The other two were a KPI progress bar that drew "4.2 out of 5" as 4%, and a
rich-text caret that jumped to position zero whenever a toolbar button was pressed.

**Deliberately deferred to Phase 10:** EF migrations, payment-provider webhooks, entity
normalisation, automated tests, and real cloud-storage implementations. The first of
these is the most pressing — while the schema is created with `EnsureCreated()`, an
entity change means deleting the database, which is unacceptable once a school holds
real records. Phase 9 alone forced another such deletion, for six new columns.
