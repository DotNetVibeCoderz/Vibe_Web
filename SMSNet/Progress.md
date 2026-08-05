# Progress.md — Catatan Pengembangan SMSNet

> Catatan berjalan tentang apa yang sudah dibangun, sedang dikerjakan, dan tersisa.
> Peta jalan lengkap ada di [PLAN.md](PLAN.md).

**Diperbarui:** 5 Agustus 2026 (termasuk Fase 9 — kolaborasi, lookup & unggahan)
**Status build:** ✅ `dotnet build` bersih — **0 error**
**Status runtime:** ✅ 39 rute dirender, **0 console error**
**Status RBAC:** ✅ **72 dari 72 probe lolos** pada 4 peran × 18 rute
**Status LLM:** ✅ Terverifikasi terhadap **2 API model sungguhan** — DeepSeek dan Azure OpenAI

---

## Ringkasan Status

| Fase | Nama | Status |
| --- | --- | :---: |
| 0 | Basis aplikasi | ✅ Selesai |
| 1 | Fondasi desain | ✅ Selesai |
| 2 | Asisten "Pak Dedi" | ✅ Selesai |
| 3 | Penerapan desain ke seluruh halaman | ✅ Selesai |
| 4 | Metode pembayaran | ✅ Selesai |
| 5 | Pengetatan RBAC & perbaikan bug | ✅ Selesai |
| 6 | Dokumentasi dwibahasa | ✅ Selesai |
| 7 | Absensi QR & kartu ber-QR | ✅ Selesai |
| 8 | Penjadwalan otomatis & uji LLM nyata | ✅ Selesai |
| 9 | Kolaborasi, lookup & unggahan | ✅ Selesai |
| 10 | Setelah itu | ⬜ Belum dijadwalkan |

---

## ✅ Fase 1 — Fondasi Desain

### Yang dibangun

| Berkas | Isi |
| --- | --- |
| `wwwroot/app.css` | Sistem token lengkap: palet, tipografi, elevasi, komponen, motion, `prefers-reduced-motion`, gaya cetak |
| `wwwroot/chat.css` | Permukaan percakapan dan prosa Markdown |
| `Components/App.razor` | Google Fonts, jembatan token ke Tailwind, skrip tema pra-paint, favicon SVG |
| `Components/Layout/MainLayout.razor` | Kerangka responsif, drawer mobile, sakelar tema, salam menurut waktu WIB |
| `Components/Layout/AuthLayout.razor` | Tata letak terpisah untuk halaman masuk/daftar/reset |
| `Components/Layout/NavMenu.razor` | Navigasi yang difilter menurut peran |
| `Services/NavigationRegistry.cs` | Sumber tunggal peta halaman → peran (9 grup, 31 rute) |
| `Components/Shared/*` | `Icon` (48 ikon), `PageHeader`, `StatTile`, `Modal`, `ConfirmDialog`, `EmptyState`, `Toaster`, `Pager`, `RedirectToLogin`, `CrudPageBase` |
| `Services/SchoolClock.cs` | Waktu WIB yang tahan beda OS |
| `Services/ToastService.cs` | Umpan balik sementara per circuit |
| `Services/AppUserClaimsPrincipalFactory.cs` | Menaruh `FullName` di cookie |

### Keputusan desain

Arah **"Buku Induk"** — buku register sekolah. Elemen khasnya **penanda tab kuning**
yang menandai posisi aktif, meniru pembatas plastik pada buku induk fisik. Motif itu
berulang di sidebar, daftar percakapan, dan logo.

Palet sengaja menghindari tiga tampilan default AI. Warnanya berakar pada material
sekolah Indonesia: navy seragam SMP, kuning kunyit, putih kapur.

---

## ✅ Fase 2 — Asisten "Pak Dedi"

### Yang dibangun

`Services/Assistant/` berisi: `AssistantOptions`, `AnthropicChatCompletionService`,
`AssistantKernelFactory`, `AssistantService`, `MarkdownRenderer`, `ChatUploadService`,
`AssistantUserContext`, dan 4 plugin. Antarmukanya di
`Components/Pages/Assistant/Chat.razor`.

### Kernel function (24)

| Plugin | Fungsi |
| --- | --- |
| **Waktu** | `tanggal_hari_ini`, `hitung_selisih_hari`, `tambah_hari`, `info_tahun_ajaran` |
| **Matematika** | `hitung` (parser sendiri, tata bahasa tertutup), `persentase`, `statistik` |
| **Web** | `cari_internet` (Tavily), `baca_halaman`, `baca_file_dari_url` |
| **SekolahData** | 14 fungsi kueri data sekolah |

### Keputusan teknis penting

1. **Konektor Anthropic ditulis sendiri.** Microsoft tidak merilis konektor Anthropic;
   paket komunitas masih 1.25-alpha sementara SK yang dipakai 1.78 — selisih 53 versi minor.
2. **Temperature tidak dikirim ke Anthropic** — model Claude terkini menolaknya dengan 400.
   Kedalaman diatur lewat `Effort`.
3. **RBAC ditegakkan di dalam fungsi, bukan di prompt.**
4. **Penjagaan SSRF** pada fungsi web.
5. **Setiap fungsi data membuka scope DI sendiri.**
6. **WebP ditolak saat unggah** karena tidak didukung enum media Anthropic.

---

## ✅ Fase 3 — Penerapan Desain ke Seluruh Halaman

Seluruh **36 halaman** ditulis ulang di atas sistem desain. Tidak tersisa satu pun
kelas warna mentah (`indigo-600`, `slate-300`) di berkas `.razor`.

`Components/Shared/CrudPageBase.cs` kini memuat mekanisme bersama halaman master data —
pencarian, saringan, pengurutan, paging, konfirmasi hapus, dan ekspor CSV. Sebelumnya
setiap halaman menyalin ±120 baris logika yang sama dan perilakunya melenceng.

Halaman yang ditulis ulang: Dashboard · 4 halaman Master · 4 Akademik · 4 Guru & Staff ·
4 Orang Tua & Siswa · 5 Administrasi · 3 Analitik · 5 Laporan · Events · 3 Keamanan &
Integrasi · 4 Autentikasi.

---

## ✅ Fase 4 — Metode Pembayaran

### Yang dibangun

| Berkas | Isi |
| --- | --- |
| `Models/PaymentEntities.cs` | `PaymentGatewayConfig`, `PaymentTransaction` |
| `Services/Payments/IPaymentGateway.cs` | Abstraksi gateway |
| `Services/Payments/Gateways.cs` | Midtrans, Xendit, Stripe, QRIS, Transfer Manual |
| `Services/Payments/PaymentService.cs` | Registry + pembuatan & rekonsiliasi tagihan |
| `Components/Pages/Admin/PaymentGateways.razor` | Pengaturan kanal dari UI |
| `Components/Pages/Parent/EPayment.razor` | Alur checkout |

Konfigurasi ganda: appsettings sebagai nilai bawaan, ditimpa oleh pengaturan yang
disimpan lewat UI — sehingga instalasi baru tetap bisa boot dari konfigurasi saja.

### Terverifikasi

Tagihan dibuat dengan referensi `SMSNET-20260805-0001`, petunjuk pembayaran tampil,
konfirmasi berhasil, dan `PaymentRecord` terkait ikut diperbarui.

### Keterusterangan

Titik integrasi HTTP untuk Midtrans, Xendit, dan Stripe sudah ditulis lengkap dan
ditandai `LIVE CALL`, tetapi **belum pernah diuji terhadap akun sungguhan** karena tidak
ada kredensial di lingkungan pengembangan. Callback/webhook penyedia **belum ditangani** —
konfirmasi masih manual.

---

## ✅ Fase 5 — Pengetatan RBAC & Perbaikan Bug

Delapan temuan audit, seluruhnya ditutup:

| # | Temuan | Perbaikan |
| --- | --- | --- |
| 1 | **`api/*` terbuka tanpa autentikasi** | `[Authorize]` per controller + admin-only pada verb tulis. Diverifikasi: 401. |
| 2 | Peran `siswa`/`orangtua` tanpa halaman | Kini punya 5–7 halaman yang benar-benar dapat dibuka |
| 3 | Hapus tanpa konfirmasi | `ConfirmDialog` pada seluruh penghapusan |
| 4 | `DbContext` lintas await | Seluruh halaman memakai `IDbContextFactory` |
| 5 | Audit trail kosong | `AuditService` mencatat setiap create/update/delete |
| 6 | Paket dengan advisory | Dipin ke versi patched; `dotnet build` kini **0 warning** |
| 7 | Logout tanpa antiforgery | `[ValidateAntiForgeryToken]` + `<AntiforgeryToken />` |
| 8 | `DateTime.Now` mengabaikan zona waktu | Seluruh kode memakai `SchoolClock` (WIB) |

Perbaikan tambahan: proteksi open-redirect pada `ReturnUrl`, lockout pada percobaan
login gagal, pesan galat yang tidak membocorkan keberadaan username, dan halaman galat
yang menampilkan trace identifier.

### Hasil uji RBAC

Empat akun dibuat (`admin`, `bu_sari`/guru, `andi`/siswa, `pak_budi`/orangtua), lalu
15 rute diprobe per peran:

```
=== admin    === 15/15 OK
=== guru     === 15/15 OK
=== siswa    === 15/15 OK
=== orangtua === 15/15 OK
==> ALL RBAC PROBES PASSED
```

### Bukti penegakan peran di dalam fungsi asisten

Model diarahkan memanggil fungsi terbatas `rekap_absensi` untuk dua pengguna berbeda:

| Pengguna | Yang dikembalikan fungsi |
| --- | --- |
| **siswa** | "Maaf, rekap absensi sekolah hanya dapat diakses oleh admin atau guru. Akun Anda saat ini memiliki peran: siswa." |
| **admin** | "Rekap absensi 06 Jul 2026 – 05 Aug 2026: Total catatan 18, Hadir 15 (83.3%)…" |

Fungsi yang dipanggil identik; yang membedakan adalah pemeriksaan peran di dalam kode
fungsi — bukan keputusan model.

Isolasi sesi percakapan juga terbukti: admin hanya melihat percakapan miliknya sendiri.

---

## ✅ Fase 6 — Dokumentasi Dwibahasa

```
docs/
├── README.md              indeks dwibahasa + galeri tangkapan layar
├── id/  instalasi · arsitektur · fitur · rbac · pembayaran · asisten · api · deployment
├── en/  installation · architecture · features · rbac · payments · assistant · api · deployment
└── img/ 16 tangkapan layar dari aplikasi yang berjalan
```

16 dokumen, 16 gambar, tanpa rujukan gambar yang rusak. README diperbarui dengan
tangkapan layar dan penunjuk ke seluruh dokumen.

Seluruh tangkapan layar diambil dari aplikasi yang benar-benar berjalan, bukan mockup.

---

## ✅ Fase 7 — Absensi QR & Kartu Ber-QR

### Yang dibangun

| Berkas | Isi |
| --- | --- |
| `Services/Attendance/QrCodeService.cs` | Penerbitan kode, render SVG, normalisasi & resolusi kode |
| `Services/Attendance/CardTemplateService.cs` | Muat/simpan/render template kartu, dengan sanitasi |
| `Services/Attendance/QrAttendanceService.cs` | Pencatatan pindaian, anti-duplikat, daftar hari ini |
| `Components/Pages/Master/StudentCards.razor` | Pilih, pratinjau, cetak kartu + editor template |
| `Components/Pages/Academic/QrAttendance.razor` | Dua mode pindai + daftar kehadiran hari ini |
| `wwwroot/cards.css` | Tata letak kartu ID-1 dan gaya cetak |
| `wwwroot/qrscan.js` | Pemindaian kamera (BarcodeDetector → jsQR) |
| `wwwroot/templates/*.html` | Template bawaan yang dapat disunting |

### Keputusan teknis penting

1. **Kode disimpan, bukan diturunkan dari Id.** Kartu yang hilang dapat diterbitkan
   ulang dan kode lamanya langsung mati. Sufiks acak 4 karakter mencegah seorang siswa
   menebak kode temannya dari nomor induk.
2. **Huruf I/O dan angka 0/1 dihindari** karena paling sering salah dibaca saat
   kode diketik manual.
3. **QR dirender sebagai SVG data URI** — tajam pada ukuran cetak apa pun, dan halaman
   cetak tidak memerlukan permintaan HTTP tambahan.
4. **Pindaian ganda tidak membuat baris baru.** Di gerbang sekolah kartu sering
   tertempel dua kali; menumpuk baris akan merusak seluruh persentase kehadiran.
5. **Satu kolom isian melayani pemindai genggam dan ketik manual**, karena alat pemindai
   memang berperilaku seperti papan ketik. Kolom itu menjaga fokusnya sendiri.
6. **Template disanitasi sebelum dirender** — admin dapat menempelkan HTML, termasuk
   potongan yang tanpa sengaja membawa `script`.

### Perbaikan menyertai: culture id-ID

Aplikasi sebelumnya berjalan pada culture sistem, sehingga tanggal tampil
"Wednesday, 05 August 2026" pada server berbahasa Inggris — yang berarti hampir semua
server. Culture kini disetel ke **id-ID** di `Program.cs`, menghasilkan
"Rabu, 05 Agustus 2026" dan "Rp2.100.000".

Konsekuensi yang ikut ditangani: pemisah desimal id-ID adalah koma, yaitu karakter yang
sama dengan pemisah kolom CSV. Ekspor CSV kini memakai format angka invariant lewat
helper `CsvNumber`, agar nilai seperti 85,5 tidak terpecah menjadi dua kolom.

### Terverifikasi

| Yang diuji | Hasil |
| --- | :---: |
| Terbitkan kode untuk 40 siswa + 15 guru | ✅ |
| 40 kartu dirender dengan 40 gambar QR | ✅ |
| QR benar-benar berupa SVG data URI | ✅ |
| Editor template + pratinjau langsung | ✅ |
| Pindai pertama → tercatat dengan jam | ✅ |
| Pindai kedua → melaporkan jam pertama, tanpa baris baru | ✅ |
| Input berantakan `sis 000001 mym3` dikenali | ✅ |
| Kode asing → ditolak dengan pesan jelas | ✅ |
| Pencarian nama pada daftar hari ini | ✅ |
| Kamera gagal di headless → pesan menunjuk mode manual | ✅ |
| RBAC dua rute baru, 4 peran | ✅ 8/8 |
| Sidebar & matriks RBAC ikut otomatis | ✅ |
| Nol console error | ✅ |

**Belum diverifikasi:** pemindaian kamera sungguhan dan pemindai genggam fisik —
keduanya memerlukan perangkat keras yang tidak tersedia di lingkungan ini. Jalur
kodenya menangani kegagalan kamera dengan anggun dan selalu menunjuk mode alternatif.

---

## ✅ Fase 8 — Penjadwalan Otomatis & Uji LLM Nyata

### Yang dibangun

- **`Services/Scheduling/`** — mesin penjadwalan sebagai layanan tersendiri:
  `TimetableModels.cs` (permintaan, hasil, jenis bentrok), `TimetableGenerator.cs`
  (solver), `TimetableValidator.cs` (pemeriksa papan).
- **`Components/Pages/Academic/ScheduleGenerator.razor`** — halaman dua langkah
  (Pengaturan → Hasil) dengan grid seminggu yang dapat disunting per sel.
- Tombol masuk **"Penjadwalan Otomatis"** pada `Curriculum.razor`, plus entri sidebar.
- **19 contoh prompt** dalam 6 kelompok pada layar sambutan `Chat.razor`.
- **Penyedia Azure OpenAI** pada `AssistantOptions` + `AssistantKernelFactory`.

### Keputusan teknis penting

**Solver, bukan pengacakan.** Penjadwalan adalah masalah pemenuhan batasan, dan
menyelesaikannya dengan mengacak lalu memeriksa akan gagal pada kasus yang sedikit saja
ketat. Yang dipakai adalah backtracking dengan **MRV** (kerjakan dulu pelajaran dengan
pilihan tersisa paling sedikit) dan **forward checking** (batalkan cabang begitu ada
pelajaran kehilangan seluruh pilihannya), ditambah **randomized restart** — maksimal 40
percobaan dalam anggaran 8 detik.

**Gagal dengan cara yang berguna.** Permintaan mustahil ditolak *sebelum* penyusunan
dimulai, dengan alasan yang menyebut angkanya. Bila hanya sebagian yang muat, papan
terbaik tetap ditampilkan beserta daftar pelajaran yang gagal ditempatkan. Halaman
kosong tidak memberi tahu operator apa pun.

**Mapel tanpa guru dilewati, bukan menggagalkan seluruh permintaan.** Kolom jam untuk
mapel semacam itu dinonaktifkan di antarmuka — jadi bila jamnya tetap ikut terkirim,
operator akan melihat kegagalan yang **tidak dapat ia perbaiki dari halaman itu**.
Sekarang mapel tersebut dikeluarkan dari permintaan dan namanya disebut, lengkap dengan
tautan ke Master Data → Guru.

**Menyimpan berarti mengganti.** Jadwal seminggu adalah satu kesatuan; menggabungkan
minggu baru ke atas yang lama meninggalkan pelajaran yatim. Karena itu jumlah entri lama
selalu disebutkan pada dialog konfirmasi.

**Ringkasan tidak boleh basi.** Kalimat ringkasan dari solver menggambarkan papan yang
*ia* hasilkan. Begitu operator menyunting satu sel, kalimat itu bisa menjadi salah —
karena itu papan yang kini bentrok melaporkan keadaannya sekarang, bukan keadaan saat
lahir.

### Dua bug produksi yang hanya muncul saat diuji dengan model sungguhan

**1. Proyeksi EF Core (fatal, senyap).** Delapan fungsi `SekolahDataPlugin` memformat
angka dan tanggal di dalam `Select()`:

```csharp
.Select(p => new[] { p.Amount.ToString("C0", Rupiah), ... })   // ✗ gagal saat dijalankan
```

EF Core tidak dapat menerjemahkan `ToString(format)` menjadi SQL. Perbaikannya adalah
memateralisasi dulu, baru memformat:

```csharp
var rows = (await query.Take(MaxRows).ToListAsync(ct))       // ✓ ambil dulu
    .Select(p => new[] { p.Amount.ToString("C0", Rupiah) })  //   format di memori
    .ToList();
```

Yang membuat bug ini berbahaya: model **tidak** menampilkan pesan galat teknis. Ia
melaporkannya dengan sopan sebagai *"sistem rekap pembayaran sedang mengalami kendala
teknis"* — terdengar seperti gangguan sesaat, bukan seperti fungsi yang tidak pernah
bisa bekerja. Mock yang mengembalikan JSON siap pakai tidak akan pernah menyentuh jalur
kode ini.

**2. Model penalaran menolak parameter sampling.** `gpt-5-mini` membalas HTTP 400:
`Unsupported parameter: 'max_tokens' is not supported with this model. Use
'max_completion_tokens' instead.` Keluarga gpt-5 dan o1/o3/o4 juga hanya menerima nilai
bawaan `temperature`/`top_p`, sedangkan Semantic Kernel mengirim nama parameter lama.
`AssistantKernelFactory` kini mengenali keluarga model tersebut dan tidak mengirimkan
ketiganya sama sekali.

### Terverifikasi

| Yang diuji | Hasil |
| --- | :---: |
| Tombol masuk pada Kurikulum → halaman penjadwalan | ✅ |
| Susun jadwal: 8 kelas, 112 jam, tanpa bentrok | ✅ 110 ms, 1 percobaan |
| Mapel tanpa pengampu dilewati + diberi catatan | ✅ |
| Sunting sel → kosongkan | ✅ |
| Guru yang sibuk ditandai di daftar pilihan | ✅ |
| Peringatan tampil **sebelum** bentrok diterapkan | ✅ |
| Bentrok terdeteksi, sel ditandai merah | ✅ 1 sel |
| Tombol Simpan nonaktif selama ada bentrok | ✅ |
| Susun Ulang mengembalikan papan bersih | ✅ |
| Dialog konfirmasi menyebut jumlah entri lama & baru | ✅ "12 → 112" |
| Tersimpan ke basis data | ✅ 112 entri |
| Muncul di tab Jadwal Pelajaran | ✅ 112 baris, 5 hari |
| **DeepSeek** menjawab 4 pertanyaan dari data DB | ✅ |
| **Azure OpenAI** (gpt-5-mini) menjawab 4 pertanyaan yang sama | ✅ |
| Contoh prompt: 6 kelompok, 19 prompt, klik langsung kirim | ✅ |
| RBAC rute `/academic/schedule-generator`, 4 peran | ✅ 4/4 (admin & guru boleh) |
| Sapuan seluruh rute setelah perubahan | ✅ 39 rute, 0 masalah |
| Nol console error | ✅ |

Contoh jawaban yang benar-benar dikembalikan model sungguhan, seluruhnya dari basis
data lewat pemanggilan fungsi:

| Pertanyaan | Jawaban | Fungsi |
| --- | --- | --- |
| "Ada berapa siswa aktif saat ini?" | 35 siswa aktif dari total 40 | `ringkasan_sekolah` |
| "Berapa total tunggakan SPP saat ini?" | Rp2.100.000 dari 3 siswa (Siswa 04/08/12) | `rekap_pembayaran` |
| "Hitung 15% dari 2.500.000" | Rp375.000 | `Matematika-hitung` |
| "Sekarang tahun ajaran dan semester berapa?" | 2026/2027, Semester 1 (Ganjil) | `info_tahun_ajaran` |

---

## ✅ Fase 9 — Kolaborasi, Lookup & Unggahan

Tujuh permintaan konkret pada halaman yang sudah ada. Bukan fitur baru — yang sudah ada
tapi belum layak dipakai.

### Yang dibangun

**Infrastruktur bersama**

- `Models/CollaborationEntities.cs` — `Comment` + `CommentAttachment`
- `Services/CommentService.cs` — baca/tulis utas, dan **penegakan hak hapus**
- `Services/UploadService.cs` — simpan berkas dengan nama yang dibuat sendiri
- `Services/HtmlContentSanitizer.cs` — bersihkan HTML editor saat menyimpan
- `Components/Shared/CommentThread.razor` — utas + lampiran + emoji
- `Components/Shared/RichTextEditor.razor` + `wwwroot/editor.js`
- `Components/Shared/LookupInput.razor`, `LinkButton.razor`
- `CrudPageBase`: hook `OnFormOpened`, `ConfirmDeleteAsync` menjadi virtual

**Per halaman**

| Halaman | Perubahan |
| --- | --- |
| `Portal.razor` | Nominal tagihan diperbaiki (lihat bug 1) |
| `Communication.razor` | Editor teks berformat + komentar per topik |
| `Grades.razor` | Lookup siswa & mapel, kelas terisi otomatis dan **ikut tersimpan**, validasi 0–100, saringan kelas |
| `ELearning.razor` | `LinkUrl` + tombol buka yang menyesuaikan jenis materi |
| `Tasks.razor` | `LinkUrl` opsional + pemilihan **beberapa kelas atau semua** |
| `Performance.razor` | Lookup guru, templat indikator, satuan capaian + validasi, komentar per indikator |
| `Documents.razor` | Unggah berkas sungguhan, berdampingan dengan mode tautan |

### Keputusan teknis penting

**Editor ditulis sendiri.** Tailwind dan Chart.js memang dari CDN — halaman tetap
terbaca tanpa keduanya. Editor yang lenyap saat jaringan bermasalah berarti seseorang
kehilangan tulisannya, jadi ~150 baris sendiri lebih murah daripada ketergantungan
ratusan kilobyte yang bisa hilang.

**Sanitasi saat menyimpan, bukan saat menampilkan.** Editor berjalan di peramban, jadi
keluarannya input pengguna seperti yang lain — permintaan yang dibuat manual bisa
mengirim HTML apa pun tanpa menyentuh toolbar. Membersihkan saat menyimpan berarti
muatan berbahaya tidak pernah sempat tersimpan.

**Hak hapus komentar ditegakkan di layanan.** Menyembunyikan tombol hanyalah lapisan
pertama; `CommentService.DeleteAsync` yang benar-benar menolak. Kepemilikan dicocokkan
dengan **id akun**, bukan nama tampilan — nama tidak unik dan dapat diubah.

**Utas dialamatkan (jenis, id), bukan foreign key per tabel.** FK sungguhan berarti satu
kolom baru dan satu migrasi setiap kali halaman lain ingin berkomentar. Konsekuensinya
basis data tidak bisa cascade, jadi induk menghapus utasnya sendiri secara eksplisit.

**Kelas didenormalisasi ke baris nilai.** Sebuah nilai adalah catatan sebuah momen.
Membaca kelas dari data siswa saat menampilkan akan diam-diam mengubah hasil tahun lalu
begitu siswa naik kelas.

**Lookup memakai `<datalist>`, bukan `<select>`.** Nama bisa mendahului master data, dan
`<select>` tidak dapat menyatakan "salah satu dari ini, atau yang lain". Keterangan di
bawah kolom menyebutkan apakah nilainya cocok — jadi salah ketik terlihat, tanpa ditolak.

### Tiga bug yang ditemukan dan diperbaiki

**1. Razor membaca `Rp@bill.Amount` sebagai alamat email (terlihat pengguna).**

```razor
Rp@bill.Amount.ToString("N0")   ✗ dikirim apa adanya sebagai teks
@Money(bill.Amount)             ✓
```

Pola *huruf, `@`, huruf* adalah literal email bagi Razor. Orang tua melihat tulisan
`Rp@bill.Amount.ToString("N0")` pada halaman tagihannya, bukan angka — dan halaman itu
tetap merender tanpa galat apa pun, sehingga tidak ada yang memicu peringatan. Seluruh
markup disapu untuk pola yang sama; hanya satu kejadian.

**2. Bilah kemajuan KPI salah membaca skala.** Capaian "4.2" pada skala 0–5 digambar
sebagai 4%, bukan 84%, karena angkanya dibaca sebagai persen tanpa tahu satuannya.
Satuan kini disimpan sebagai kolom tersendiri, bukan ditebak dari bentuk nilainya.

**3. Caret hilang setelah menekan tombol toolbar.** Mengklik tombol memindahkan fokus
dari editor; mengembalikan fokus saja menempatkan caret di posisi nol, sehingga teks
yang diketik setelah menekan "daftar berpoin" mendarat di awal dokumen. Diperbaiki
dengan `preventDefault` pada `mousedown` — fokus tidak pernah lepas — ditambah
penyimpanan dan pemulihan *range* untuk dialog tautan, yang memang merebut fokus.

### Terverifikasi

| Yang diuji | Hasil |
| --- | :---: |
| Portal: nominal tagihan tampil sebagai angka | ✅ Rp525.000 |
| Portal: tidak ada sisa markup Razor | ✅ |
| Editor: 11 tombol toolbar | ✅ |
| Editor: tebal + daftar berpoin tersimpan sebagai `<b>` dan `<ul><li>` | ✅ |
| Editor: teks setelah klik toolbar mendarat di tempat yang benar | ✅ |
| Editor: placeholder muncul saat kosong, paragraf terbungkus `<p>` | ✅ |
| Editor: isi lama (teks polos) tetap terbaca | ✅ |
| Komentar: emoji (20), lampiran, tampil setelah kirim | ✅ |
| **Komentar: penulis boleh hapus miliknya** | ✅ |
| **Komentar: penulis TIDAK ditawari hapus milik orang lain** | ✅ |
| **Komentar: admin boleh hapus milik siapa pun** | ✅ 2/2 |
| Nilai: lookup 40 siswa, 8 mapel, 8 kelas | ✅ |
| Nilai: memilih siswa mengisi kelas otomatis | ✅ "Siswa 03" → 7B |
| Nilai: 150 ditolak | ✅ |
| Nilai: kelas tersimpan dan muncul kembali saat diubah | ✅ 2/2 baris |
| E-Learning: tombol buka, label sesuai jenis | ✅ |
| E-Learning: `javascript:` ditolak | ✅ |
| E-Learning: `drive.google.com/…` dinormalkan ke `https://` | ✅ |
| Tugas: 8 kelas dapat dipilih, kosong ditolak | ✅ |
| Tugas: 3 kelas tersimpan dan muncul kembali saat diubah | ✅ 10A, 7A, 8A |
| Tugas: "Semua kelas" muncul saat disaring per kelas | ✅ |
| Kinerja: lookup 15 guru, 6 templat indikator | ✅ |
| Kinerja: 150% dan skala 7 ditolak | ✅ |
| Kinerja: skala 4.2/5 digambar 84% | ✅ |
| Kinerja: komentar per indikator | ✅ |
| Dokumen: unggah, tersimpan, berkas benar-benar terlayani | ✅ HTTP 200 |
| Dokumen: mode unggah/tautan bertahan saat diubah | ✅ |
| Sapuan seluruh rute setelah perubahan | ✅ 39 rute, 0 masalah |
| RBAC setelah perubahan | ✅ 72/72 |
| Nol console error | ✅ |

**Catatan:** basis data dihapus sekali lagi pada fase ini — enam kolom baru
(`GradeRecord.ClassName`, `ELearningContent.LinkUrl`, `TaskExam.LinkUrl`,
`TaskExam.Classes`, `PerformanceReview.Unit`, dan empat kolom pada `DocumentItem`)
ditambah dua tabel komentar.

---

## Ringkasan Verifikasi

| Yang diuji | Cara | Hasil |
| --- | --- | :---: |
| Kompilasi | `dotnet build` | ✅ 0 error, 0 warning |
| Seluruh rute dirender | Playwright, 39 rute sebagai admin | ✅ semua 200 |
| Console bersih | Playwright console + pageerror listener | ✅ 0 error |
| RBAC | 4 peran × 18 rute | ✅ 72/72 |
| Peran di fungsi asisten | Fungsi terbatas dipanggil 2 peran | ✅ ditolak / diizinkan sesuai peran |
| Isolasi sesi chat | Dua pengguna, periksa daftar percakapan | ✅ tidak bocor |
| Alur pembayaran | Buat tagihan → petunjuk → konfirmasi | ✅ referensi `SMSNET-20260805-0001` |
| Konfirmasi hapus | Klik Hapus, batalkan | ✅ dialog muncul, data utuh (8/8 baris) |
| Audit trail | Periksa setelah aksi | ✅ terisi entri nyata |
| API tanpa autentikasi | `curl` tanpa cookie | ✅ 401 |
| Pemanggilan fungsi asisten | Mock OpenAI-compatible | ✅ 24 fungsi, data DB sungguhan |
| Persistensi chat | Reload halaman | ✅ pesan bertahan |
| **Asisten dengan model sungguhan** | DeepSeek + Azure OpenAI (gpt-5-mini) | ✅ jawaban benar dari data DB |
| Penjadwalan otomatis ujung ke ujung | Playwright: susun → sunting → bentrok → simpan | ✅ 112 entri tersimpan |

### Yang belum diverifikasi

Dicatat terus terang:

- **Anthropic, Gemini, dan Ollama lokal** — belum diuji terhadap layanan sungguhan;
  tidak ada kredensial atau instalasinya di lingkungan ini. OpenAI (lewat DeepSeek) dan
  Azure OpenAI **sudah** diuji sungguhan, dan justru dari situlah dua bug produksi
  ditemukan — jadi "jalur kodenya identik" bukan alasan yang dapat diandalkan untuk
  ketiganya.
- **Panggilan sungguhan ke Midtrans, Xendit, dan Stripe** — butuh akun merchant.
- **Pemindaian kamera dan pemindai genggam fisik** — butuh perangkat keras.
- **Tidak ada uji otomatis.** Seluruh verifikasi di atas dilakukan dengan menjalankan
  aplikasi dan mengendalikannya lewat Playwright. Proyek uji tetap belum ada.

---

## ⬜ Fase 10 — Setelah Itu

Belum dijadwalkan, terdaftar di [PLAN.md](PLAN.md):

- **Migrasi EF Core.** Skema masih dibuat `EnsureCreated()`; perubahan entitas menuntut
  penghapusan basis data. Tidak layak produksi.
- **Webhook penyedia pembayaran** agar status transaksi mutakhir otomatis.
- **Normalisasi entitas** — mengganti relasi berbasis nama dengan foreign key.
- **Notifikasi real-time** (SignalR).
- **Impor/ekspor massal** dari Excel.
- **Uji otomatis.**
- **Penyimpanan awan** — `AzureBlob` dan `AwsS3` masih stub.
- **Paginasi & autentikasi token pada REST API.**

---

## Catatan Operasional

### Basis data harus dihapus setelah ubah entitas

```bash
rm smsnet.db smsnet.db-shm smsnet.db-wal
dotnet run
```

Dilakukan dua kali pada siklus ini: untuk tabel chat, lalu untuk tabel pembayaran.

### Mengaktifkan asisten

```bash
export Assistant__OpenAI__ApiKey="sk-..."
# atau
export Assistant__Provider="Anthropic"
export Assistant__Anthropic__ApiKey="sk-ant-..."
```

Untuk endpoint yang kompatibel dengan OpenAI (mis. DeepSeek) cukup menambah `Endpoint`;
Azure memakai bagian pengaturannya sendiri karena URL-nya memuat nama deployment:

```bash
export Assistant__Provider="AzureOpenAI"
export Assistant__AzureOpenAI__ApiKey="..."
export Assistant__AzureOpenAI__Endpoint="https://namaresource.openai.azure.com/"
export Assistant__AzureOpenAI__Deployment="gpt-5-mini"
```

> **Kredensial tidak pernah ditulis ke `appsettings.json`.** Seluruh uji terhadap model
> sungguhan memakai environment variable, dan `testkey.txt` sudah masuk `.gitignore`.

### Sebelum produksi

Daftar periksa lengkap ada di [docs/id/deployment.md](docs/id/deployment.md). Tiga yang
paling mendesak:

1. Ganti kata sandi `admin123`.
2. **Batasi halaman pendaftaran** — saat ini siapa pun dapat mendaftar sebagai `admin`.
3. Beralih ke migrasi EF Core sebelum data sungguhan masuk.

---

## English Summary

All nine planned phases are complete: the design system, the Pak Dedi assistant,
applying the design across all 36 pages, configurable payment gateways, the RBAC and
bug hardening, bilingual documentation, QR attendance with printable cards, automatic
timetabling built on a constraint solver, and a collaboration layer — a hand-written
rich-text editor, comment threads with attachments and emoji, master-data lookups, and
real file uploads.

**Verified by running the application**, not by inspection: a clean build, all 39 routes
rendering with zero console errors, 72/72 RBAC probes passing across four roles,
plugin-level role enforcement proven by having the same restricted function refuse a
student and answer an admin, chat session isolation, a full payment round trip, the QR
scan flow including duplicate protection, and the timetabler driven end to end —
generate, edit a cell, force a teacher clash, confirm it blocks saving, then save 112
entries and read them back from the database.

**The assistant was tested against two real model APIs** — DeepSeek through the
OpenAI-compatible endpoint, and Azure OpenAI on `gpt-5-mini`. That exercise found two
production bugs a mock could not have: EF Core cannot translate `ToString(format)`
inside a `Select()` projection (eight data functions were failing at query time, which
the model politely reported to users as a temporary technical problem), and reasoning
models reject the sampling parameters Semantic Kernel emits.

Phase 9 addressed seven specific complaints about existing pages, and found three more
bugs doing so. The one that mattered most was user-visible and silent: Razor reads the
pattern *letter, `@`, letter* as an email literal, so `Rp@bill.Amount.ToString("N0")`
was emitted verbatim — parents saw that string on their billing page where the amount
belonged, and the page rendered without a single error to hint at it. The others were a
KPI progress bar drawing "4.2 out of 5" as 4%, and a rich-text caret that jumped to
position zero on every toolbar press. The comment deletion rule — author or admin, and
nobody else — was verified across three accounts rather than assumed.

**Not verified:** Anthropic, Gemini, and local Ollama against their real services;
real merchant accounts for Midtrans, Xendit, and Stripe; and physical camera/handheld
scanner hardware. There is still no automated test project — all verification was done
by driving the running application with Playwright.

**Most urgent remaining item:** the schema is still created with `EnsureCreated()`
rather than EF migrations, so an entity change requires deleting the database. That is
unacceptable for a real school and is the first item in Phase 10. Phase 9 alone forced
another deletion, for six new columns and two new tables.
