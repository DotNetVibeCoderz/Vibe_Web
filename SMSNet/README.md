# SMSNet — Sistem Manajemen Sekolah

Aplikasi manajemen sekolah untuk jenjang SMP/SMA di Indonesia. Dibangun sebagai satu
proyek **ASP.NET Core Blazor Server** dengan SQLite, tanpa proses build frontend.

A school management system for Indonesian secondary schools, built as a single
**ASP.NET Core Blazor Server** project with SQLite and no frontend build step.

> Dibuat oleh **jacky the code bender** dari Gravicode Studios (dipimpin Kang Fadhil).

---

![Dashboard](docs/img/dashboard.png)

---

## 🇮🇩 Bahasa Indonesia

### Mulai Cepat

```bash
dotnet restore
dotnet run
```

Buka `http://localhost:5175`, lalu masuk dengan `admin` / `admin123`.

> **Ganti kata sandi bawaan sebelum dipakai sungguhan.** Daftar periksa produksi
> selengkapnya ada di [dokumen deployment](docs/id/deployment.md).

**Kebutuhan:** .NET SDK 10.0. Tidak perlu Node.js. Koneksi internet diperlukan saat
berjalan karena Tailwind, Google Fonts, dan Chart.js dimuat dari CDN.

### Fitur Utama

| Modul | Isi |
| --- | --- |
| **Akademik** | Kurikulum, jadwal, **penjadwalan otomatis**, **absensi QR** + manual, penilaian & rapor, e-learning |
| **Guru & Staff** | Dashboard guru, tugas & ujian, **forum internal dengan editor teks & komentar**, evaluasi kinerja (KPI) |
| **Orang Tua & Siswa** | Portal pemantauan anak, notifikasi, e-payment, **dokumen digital dengan unggah berkas** |
| **Administrasi** | Manajemen keuangan, metode pembayaran, inventaris, payroll, laporan periode |
| **Analitik** | Dashboard analitik, data analytics, custom reports, dan 5 laporan tematik |
| **Master Data** | Siswa, guru, mata pelajaran, kelas — dengan pencarian, saringan, pengurutan, paging, dan ekspor CSV |
| **Keamanan** | RBAC 4 peran, audit trail, REST API terautentikasi |
| **Asisten** | "Pak Dedi" — asisten AI yang menjawab dari data sekolah sungguhan |

### Penjadwalan Otomatis

![Penjadwalan Otomatis](docs/img/schedule-result.png)

Menyusun jadwal satu minggu penuh untuk seluruh kelas dalam hitungan ratusan milidetik,
tanpa seorang guru pun terjadwal di dua kelas pada jam yang sama.

- **Constraint solver** — backtracking dengan MRV, forward checking, dan randomized restart
- **Hasilnya simulasi** — dapat disunting per sel, diperiksa ulang setiap kali diubah
- **Bentrok menghalangi penyimpanan**, dan selnya ditandai merah pada grid
- **Permintaan mustahil ditolak lebih dulu** dengan alasan yang dapat ditindaklanjuti

Selengkapnya: [dokumen penjadwalan](docs/id/penjadwalan.md).

### Asisten "Pak Dedi"

![Pak Dedi](docs/img/assistant-thread.png)

Asisten yang menjawab pertanyaan dengan **membaca data sekolah**, bukan mengarang.

- **Lima penyedia model:** OpenAI (termasuk endpoint kompatibel seperti DeepSeek), Azure OpenAI, Anthropic, Google Gemini, Ollama — dipilih dari appsettings
- **24 kernel function** dalam 4 plugin: waktu, matematika, web (pencarian & scraping), dan data sekolah
- **Multi-sesi** dengan lampiran gambar dan dokumen
- **19 contoh pertanyaan** dalam 6 kelompok pada layar sambutan — sekali klik langsung terkirim
- **Menghormati peran** — fungsi data memeriksa peran pemanggil, bukan sekadar mempercayai prompt
- **Markdown lengkap** — tabel, blok kode, media

Selengkapnya: [dokumen asisten](docs/id/asisten.md).

### Metode Pembayaran

![Metode Pembayaran](docs/img/payment-gateways.png)

Lima kanal: **Transfer Manual**, **QRIS**, **Midtrans**, **Xendit**, dan **Stripe** —
dapat diatur dari appsettings maupun dari antarmuka admin.

Bawaannya berjalan dalam **mode sandbox**, sehingga seluruh alur pembayaran dapat diuji
tanpa akun merchant. Selengkapnya: [dokumen pembayaran](docs/id/pembayaran.md).

### Hak Akses

Empat peran: `admin`, `guru`, `siswa`, `orangtua`. Ditegakkan di **empat lapis** —
navigasi, atribut halaman, endpoint API, dan di dalam fungsi asisten.

![Matriks RBAC](docs/img/rbac-matrix.png)

Selengkapnya: [dokumen RBAC](docs/id/rbac.md).

### Dokumentasi

Dokumentasi lengkap ada di [`docs/`](docs/README.md) dalam dua bahasa:

[Instalasi](docs/id/instalasi.md) ·
[Arsitektur](docs/id/arsitektur.md) ·
[Fitur](docs/id/fitur.md) ·
[RBAC](docs/id/rbac.md) ·
[Penjadwalan](docs/id/penjadwalan.md) ·
[Kolaborasi](docs/id/kolaborasi.md) ·
[Absensi QR](docs/id/absensi-qr.md) ·
[Pembayaran](docs/id/pembayaran.md) ·
[Asisten](docs/id/asisten.md) ·
[API](docs/id/api.md) ·
[Deployment](docs/id/deployment.md)

Perencanaan: [PLAN.md](PLAN.md) (peta jalan) · [Progress.md](Progress.md) (catatan kemajuan)

---

## 🇬🇧 English

### Quick start

```bash
dotnet restore
dotnet run
```

Open `http://localhost:5175` and sign in with `admin` / `admin123`.

> **Change the default password before real use.** The full production checklist is in
> the [deployment guide](docs/en/deployment.md).

**Requires:** .NET SDK 10.0. No Node.js. An internet connection is needed at runtime
because Tailwind, Google Fonts, and Chart.js load from CDNs.

### Features

| Module | Contents |
| --- | --- |
| **Academic** | Curriculum, schedules, **automatic timetabling**, **QR attendance** + manual, grading & reports, e-learning |
| **Teachers & staff** | Teacher dashboard, tasks & exams, **internal forum with rich text & comments**, KPI reviews |
| **Parents & students** | Monitoring portal, notifications, e-payment, **digital documents with file upload** |
| **Administration** | Financial management, payment gateways, inventory, payroll, period reports |
| **Analytics** | Analytics dashboard, data analytics, custom reports, and 5 thematic reports |
| **Master data** | Students, teachers, subjects, classes — with search, filters, sorting, paging, CSV export |
| **Security** | Four-role RBAC, audit trail, authenticated REST API |
| **Assistant** | "Pak Dedi" — an AI assistant that answers from the school's real records |

### Automatic timetabling

Builds a full week for every class in a few hundred milliseconds, with no teacher booked
into two classrooms at the same hour.

- **Constraint solver** — backtracking with MRV, forward checking, and randomized restarts
- **The result is a simulation** — editable cell by cell, re-checked on every change
- **Conflicts block saving**, and their cells are outlined in red on the grid
- **Impossible requests are rejected up front** with a reason you can act on

More: [timetabling guide](docs/en/scheduling.md).

### The assistant

Answers questions by **reading school data**, not inventing it.

- **Five providers:** OpenAI (including compatible endpoints such as DeepSeek), Azure OpenAI, Anthropic, Google Gemini, Ollama — chosen in appsettings
- **24 kernel functions** across 4 plugins: time, maths, web (search & scraping), school data
- **Multi-session** with image and document attachments
- **19 example prompts** in 6 groups on the welcome screen — one click sends them
- **Role-aware** — data functions check the caller's role rather than trusting the prompt
- **Full Markdown** — tables, code blocks, media

More: [assistant guide](docs/en/assistant.md).

### Payments

Five channels: **manual transfer**, **QRIS**, **Midtrans**, **Xendit**, and **Stripe** —
configurable from appsettings and from the admin interface.

Ships in **sandbox mode**, so the entire payment flow can be exercised without a
merchant account. More: [payments guide](docs/en/payments.md).

### Access control

Four roles: `admin`, `guru`, `siswa`, `orangtua`. Enforced in **four layers** —
navigation, page attributes, API endpoints, and inside the assistant's functions.

More: [RBAC guide](docs/en/rbac.md).

### Documentation

Full documentation lives in [`docs/`](docs/README.md) in both languages:

[Installation](docs/en/installation.md) ·
[Architecture](docs/en/architecture.md) ·
[Features](docs/en/features.md) ·
[RBAC](docs/en/rbac.md) ·
[Timetabling](docs/en/scheduling.md) ·
[Collaboration](docs/en/collaboration.md) ·
[QR Attendance](docs/en/qr-attendance.md) ·
[Payments](docs/en/payments.md) ·
[Assistant](docs/en/assistant.md) ·
[API](docs/en/api.md) ·
[Deployment](docs/en/deployment.md)

Planning: [PLAN.md](PLAN.md) (roadmap) · [Progress.md](Progress.md) (progress log)

---

## Tangkapan Layar / Screenshots

| | |
| --- | --- |
| ![Master data](docs/img/master-students.png) **Master Data** | ![Laporan](docs/img/report-academic.png) **Laporan Akademik** |
| ![Kartu QR](docs/img/qr-cards.png) **Kartu Ber-QR** | ![Absensi QR](docs/img/qr-attendance.png) **Absensi QR** |
| ![Penjadwalan](docs/img/schedule-result.png) **Penjadwalan Otomatis / Timetabling** | ![Contoh prompt](docs/img/assistant-prompts.png) **Contoh Prompt / Example Prompts** |
| ![Tema gelap](docs/img/dashboard-dark.png) **Tema Gelap / Dark Theme** | ![Ponsel](docs/img/dashboard-mobile.png) **Ponsel / Mobile** |

---

## Tech Stack

| Lapis | Teknologi |
| --- | --- |
| Framework | ASP.NET Core Blazor Server (net10.0) |
| Basis data | SQLite + Entity Framework Core |
| Autentikasi | ASP.NET Core Identity (cookie) |
| Gaya | Tailwind CSS (CDN) + token desain kustom |
| Grafik | Chart.js |
| AI | Semantic Kernel 1.78 + SDK resmi Anthropic |
| Markdown | Markdig + HtmlSanitizer |

### Catatan Teknis

- **Target framework adalah net10.0.** README lama menyebut .NET 8 — itu sudah tidak berlaku.
- **Tidak ada migrasi EF.** Skema dibuat dengan `EnsureCreated()`; mengubah entitas
  menuntut penghapusan berkas basis data. Ini utang teknis yang tercatat di
  [PLAN.md Fase 8](PLAN.md).
- **Konektor Anthropic ditulis sendiri** karena Microsoft tidak merilis konektor
  Anthropic untuk Semantic Kernel, dan paket komunitas tertinggal 53 versi minor.

---

## Lisensi & Kredit

Dibuat oleh **jacky the code bender** — Gravicode Studios.

Kalau berkenan, traktir pulsa ya 😄👉 https://studios.gravicode.com/products/budax
