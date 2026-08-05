# Arsitektur

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/architecture.md)

---

## Gambaran Umum

SMSNet adalah **satu proyek ASP.NET Core Blazor Server**. Tidak ada pemisahan
backend/frontend, tidak ada SPA terpisah, dan tidak ada proses build JavaScript.
Keputusan ini disengaja: sekolah harus bisa men-deploy aplikasi ini tanpa toolchain Node.

```
┌──────────────────────────────────────────────────────────────┐
│ Peramban                                                     │
│   • Tailwind CSS (CDN) + wwwroot/app.css (token desain)      │
│   • Chart.js (CDN)                                           │
│   • Blazor Server circuit (WebSocket)                        │
└───────────────────────────┬──────────────────────────────────┘
                            │ SignalR
┌───────────────────────────▼──────────────────────────────────┐
│ ASP.NET Core (net10.0)                                       │
│                                                              │
│  Components/          Controllers/         Services/         │
│   ├ Layout            ├ AccountController   ├ Assistant/     │
│   ├ Shared            ├ StudentsController  ├ Payments/      │
│   └ Pages             └ TeachersController  └ (umum)         │
│                                                              │
│  Data/ ApplicationDbContext  ──►  SQLite (smsnet.db)         │
└──────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
   Model LLM           Gateway bayar        Tavily
 (OpenAI/Anthropic/   (Midtrans/Xendit/   (pencarian)
  Google/Ollama)       Stripe)
```

---

## Keputusan Arsitektur

### 1. Semua halaman interaktif secara global

`Components/App.razor` memasang `@rendermode="InteractiveServer"` pada `<Routes>`.
Akibatnya **setiap halaman interaktif** dan tidak ada halaman yang perlu mendeklarasikan
mode render sendiri.

### 2. Pemisahan autentikasi: formulir vs komponen

Ini bagian yang paling mudah salah dipahami, jadi perlu dijelaskan.

Identity didaftarkan lewat `AddIdentityCore` + cookie. Sebuah circuit Blazor yang
sudah berjalan **tidak dapat menulis cookie** — header respons sudah lama terkirim.
Karena itu:

| Alur | Cara | Alasan |
| --- | --- | --- |
| **Login & logout** | `<form method="post">` biasa ke `Controllers/AccountController.cs` | Perlu menulis cookie autentikasi |
| **Register, profil, reset kata sandi** | `EditForm` interaktif biasa memanggil `UserManager` | Tidak menandatangani sesi, jadi tidak ada cookie yang ditulis |

Ikuti pemisahan ini untuk alur baru apa pun yang mengubah cookie autentikasi.

### 3. Akses data lewat `IDbContextFactory`

`ApplicationDbContext` didaftarkan lewat `AddDbContextFactory(..., ServiceLifetime.Scoped)`
yang sekaligus menyediakan factory **dan** instance scoped.

Halaman memakai **factory**, bukan context yang disuntikkan:

```csharp
await using var db = await DbFactory.CreateDbContextAsync();
var siswa = await db.Students.AsNoTracking().ToListAsync();
```

Alasannya: sebuah circuit Blazor hidup jauh lebih lama daripada satu permintaan HTTP.
Berbagi satu `DbContext` lintas banyak `await` menghasilkan galat
*"A second operation was started on this context"* — bug yang dulu ada pada aplikasi ini.

### 4. Entitas pipih dan denormalisasi

Entitas pada `Models/Entities.cs` **sengaja tidak memakai foreign key**. Relasi
disimpan sebagai string tampilan:

```csharp
public class ScheduleItem
{
    public string ClassName { get; set; }   // "8A", bukan ClassRoomId
    public string Teacher { get; set; }     // "Guru 01", bukan TeacherId
}
```

Ikuti pola ini saat menambah entitas. Memperkenalkan foreign key sungguhan akan
merusak seeder dan setiap halaman yang menggabungkan data berdasarkan nama.

**Pengecualian:** tabel chat (`ChatSession` → `ChatMessage` → `ChatAttachment`)
memakai relasi sungguhan dengan *cascade delete*, karena menghapus satu percakapan
memang harus ikut menghapus pesan dan lampirannya.

Konsekuensi dari pilihan ini: integritas data tidak dijaga basis data. Halaman
**Laporan Master Data** karena itu memuat pemeriksaan konsistensi — misalnya jadwal
yang menunjuk guru tak terdaftar.

### 5. `NavigationRegistry` sebagai sumber tunggal

`Services/NavigationRegistry.cs` mendefinisikan setiap rute beserta peran yang boleh
membukanya. Tiga hal membacanya:

1. `NavMenu.razor` — menyusun menu samping
2. Halaman **Role Access** — menampilkan matriks hak akses
3. Dokumentasi ini

Karena satu definisi dipakai bersama, sebuah halaman **tidak mungkin** muncul di menu
untuk peran yang kemudian ditolak oleh atribut `[Authorize]` miliknya.

### 6. Waktu selalu lewat `SchoolClock`

`DateTime.Now` mengembalikan waktu server, yang pada mayoritas host adalah UTC.
Untuk sekolah di Indonesia itu menggeser catatan absensi dan pembayaran ke hari
yang salah antara pukul 00:00–07:00 waktu setempat.

Seluruh kode memakai `SchoolClock.Today`, `SchoolClock.LocalNow`, dan `SchoolClock.Now`
yang selalu mengembalikan waktu WIB (UTC+7) dan tahan terhadap perbedaan basis data
zona waktu antara Windows dan Linux.

### 7. `CrudPageBase<T>` untuk halaman master data

Sebelumnya setiap halaman CRUD menyalin ±120 baris logika pencarian, pengurutan,
paging, dan ekspor. Akibatnya perilakunya melenceng: sebagian memakai paging,
sebagian tidak, dan tidak satu pun mengonfirmasi penghapusan.

`Components/Shared/CrudPageBase.cs` kini memuat mekanisme bersamanya. Halaman hanya
menyediakan yang khas bagi entitasnya:

```csharp
protected override string EntityLabel => "siswa";
protected override DbSet<Student> Table(ApplicationDbContext db) => db.Students;
protected override IEnumerable<string?> SearchableText(Student s) => new[] { s.FullName, s.ClassName };
protected override string Describe(Student s) => s.FullName;
protected override int IdOf(Student s) => s.Id;
```

---

## Sistem Desain

Tidak ada proses build CSS. Tailwind dimuat dari CDN, dan token desain hidup sebagai
CSS custom property di `wwwroot/app.css`. Konfigurasi Tailwind inline di `App.razor`
menjembatani keduanya sehingga kelas utilitas dan lapisan komponen tidak bisa melenceng.

### Palet

| Token | Nilai | Peran |
| --- | --- | --- |
| `--tinta` | `#101A2E` | Teks dan permukaan gelap |
| `--dongker` | `#1B3A6B` | Warna utama (navy seragam SMP) |
| `--kunyit` | `#E8A317` | Aksen, status aktif, capaian |
| `--kapur` | `#F4F5F7` | Latar halaman |
| `--garis` | `#D8DCE4` | Garis pemisah |
| `--daun` | `#2F7D5C` | Sukses, hadir, lunas |
| `--bata` | `#B3452F` | Peringatan, tidak hadir, menunggak |

Tema gelap menimpa token yang sama di bawah selektor `html.dark`. Karena semua
komponen membaca token, satu definisi cukup untuk kedua tema.

### Tipografi

| Peran | Typeface |
| --- | --- |
| Judul | Bricolage Grotesque |
| Teks | Public Sans |
| Angka & kode | IBM Plex Mono |

Seluruh angka memakai *tabular figures* agar kolom pada tabel sejajar — aplikasi ini
pada dasarnya adalah buku register.

### Kelas Komponen

`.sms-card` · `.sms-table` · `.sms-btn` (varian `--primary`, `--accent`, `--ghost`,
`--quiet`, `--danger`) · `.sms-badge` · `.sms-stat` · `.sms-modal` · `.sms-toast` ·
`.sms-gauge` · `.sms-meter` · `.sms-input` / `.sms-select` / `.sms-textarea`

### Tema

`wwwroot/theme.js` mengelola pergantian tema. Penentuan awal dilakukan oleh skrip
*blocking* di dalam `<head>` sehingga halaman tidak pernah berkedip menampilkan tema
yang salah. Grafik ikut digambar ulang saat tema berganti melalui event
`smsnet:themechange`.

---

## Alur Permintaan

### Halaman biasa

```
Peramban → Blazor circuit → Komponen halaman
                              → IDbContextFactory → DbContext baru → SQLite
                              → AuditService (bila mengubah data)
                              → ToastService (umpan balik)
```

### Giliran percakapan asisten

```
Pengguna kirim pesan
  → AssistantService.SendAsync
    → AssistantKernelFactory.Build(peran pengguna)
      → Kernel + 24 fungsi (Waktu, Matematika, Web, SekolahData)
    → IChatCompletionService (OpenAI / Anthropic / Google / Ollama)
      → model meminta pemanggilan fungsi
      → Semantic Kernel menjalankan fungsi
        → plugin membuka scope DI sendiri → DbContext baru
      → hasil dikembalikan ke model
    → MarkdownRenderer: Markdig → sanitasi → pengayaan media
  → disimpan ke ChatMessage → dirender ke utas percakapan
```

### Pembuatan tagihan

```
Halaman E-Payment
  → PaymentService.CreateChargeAsync
    → PaymentGatewayRegistry: appsettings ditimpa PaymentGatewayConfig
    → IPaymentGateway.CreateChargeAsync
        mode sandbox → disimulasikan lokal
        mode produksi → HTTP ke penyedia
  → PaymentTransaction disimpan
  → AuditService mencatat
```

---

## Berkas Penting

| Berkas | Peran |
| --- | --- |
| `Program.cs` | Seluruh perakitan aplikasi; tidak ada lapisan startup tambahan |
| `Components/App.razor` | Dokumen HTML, font, konfigurasi Tailwind, skrip tema |
| `Components/Routes.razor` | Router dan penanganan akses ditolak |
| `Services/NavigationRegistry.cs` | Peta rute → peran |
| `Components/Shared/CrudPageBase.cs` | Mekanisme bersama halaman master data |
| `Services/SchoolClock.cs` | Waktu WIB |
| `Services/Assistant/AnthropicChatCompletionService.cs` | Konektor Claude untuk Semantic Kernel |
| `Services/Payments/PaymentService.cs` | Pembuatan dan rekonsiliasi tagihan |
| `wwwroot/app.css` | Seluruh token dan lapisan komponen |
