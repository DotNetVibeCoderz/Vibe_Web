# Instalasi & Konfigurasi

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/installation.md)

---

## Kebutuhan Sistem

| Komponen | Versi | Catatan |
| --- | --- | --- |
| .NET SDK | **10.0** atau lebih baru | Wajib. README lama menyebut .NET 8 — itu sudah tidak berlaku. |
| Sistem operasi | Windows, Linux, atau macOS | Diuji pada Windows 11. |
| Basis data | SQLite | Berkas dibuat otomatis; tidak perlu server terpisah. |
| Koneksi internet | Diperlukan saat menjalankan | Tailwind, Google Fonts, dan Chart.js dimuat dari CDN. Tanpa internet aplikasi tetap berjalan tetapi tampil tanpa gaya dan tanpa grafik. |
| Node.js | Tidak diperlukan | Tidak ada proses build frontend. |

---

## Menjalankan Aplikasi

```bash
git clone <repositori-anda>
cd SMSNet

dotnet restore
dotnet run
```

Aplikasi akan berjalan di:

- `http://localhost:5175`
- `https://localhost:7184`

Profil peluncuran diatur pada `Properties/launchSettings.json`.

### Akun Bawaan

| Username | Kata sandi | Peran |
| --- | --- | --- |
| `admin` | `admin123` | admin |

Akun ini dibuat otomatis saat pertama kali dijalankan. **Ganti kata sandinya sebelum dipakai
sungguhan** melalui halaman *Profil Saya*.

### Perintah Lain

```bash
dotnet build          # kompilasi saja
dotnet watch          # jalankan dengan hot reload
dotnet run --launch-profile http    # paksa hanya HTTP
```

Swagger UI tersedia di `/swagger`, **hanya pada environment Development**.

---

## Basis Data

Skema dibuat memakai `EnsureCreated()`, **bukan** migrasi EF Core. Konsekuensinya penting:

> Setelah mengubah apa pun di `Models/*.cs` atau `Data/ApplicationDbContext.cs`,
> berkas SQLite lama **tidak** ikut berubah. Aplikasi akan gagal atau berperilaku aneh
> sampai berkasnya dihapus.

```bash
rm smsnet.db smsnet.db-shm smsnet.db-wal
dotnet run
```

`DbInitializer.SeedAsync` berhenti lebih awal bila tabel `Students` sudah berisi data,
jadi perubahan data contoh juga menuntut penghapusan di atas.

Migrasi EF Core terdaftar sebagai utang teknis di [PLAN.md Fase 8](../../PLAN.md).

---

## Konfigurasi

Seluruh pengaturan ada di `appsettings.json`. Untuk nilai rahasia, **gunakan environment
variable** agar tidak ikut tersimpan di repositori. Pemisah tingkatnya adalah dua garis bawah.

### Basis Data

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=smsnet.db"
}
```

### Penyimpanan Berkas

```json
"FileStorage": {
  "Provider": "FileSystem",
  "BasePath": "wwwroot/uploads"
}
```

Pilihan `Provider`: `FileSystem` (bawaan), `AzureBlob`, `AwsS3`.

> ⚠️ Implementasi `AzureBlob` dan `AwsS3` pada `Services/CloudStoragePlaceholders.cs`
> masih berupa *stub* yang mengembalikan path palsu. Jangan dipakai di produksi
> sebelum diimplementasikan.

### Asisten "Pak Dedi"

Rincian lengkap ada di [dokumen asisten](asisten.md). Ringkasnya:

```bash
# OpenAI (bawaan)
export Assistant__OpenAI__ApiKey="sk-..."

# atau Anthropic
export Assistant__Provider="Anthropic"
export Assistant__Anthropic__ApiKey="sk-ant-..."

# atau Google Gemini
export Assistant__Provider="Google"
export Assistant__Google__ApiKey="..."

# atau Ollama — tanpa kunci API, jalankan Ollama secara lokal
export Assistant__Provider="Ollama"

# pencarian internet (opsional)
export Assistant__Tavily__ApiKey="tvly-..."
```

Pada Windows PowerShell:

```powershell
$env:Assistant__OpenAI__ApiKey = "sk-..."
```

### Metode Pembayaran

Rincian lengkap ada di [dokumen pembayaran](pembayaran.md). Secara bawaan aplikasi
berjalan dalam **mode sandbox** — tidak ada permintaan yang dikirim ke penyedia mana pun,
sehingga seluruh alur dapat diuji tanpa akun merchant.

```json
"Payments": {
  "SandboxMode": true
}
```

---

## Struktur Direktori

```
SMSNet/
├── Components/
│   ├── App.razor              # dokumen HTML, font, konfigurasi Tailwind
│   ├── Routes.razor           # router + penanganan akses ditolak
│   ├── Layout/                # MainLayout, AuthLayout, NavMenu
│   ├── Shared/                # komponen bersama + CrudPageBase
│   └── Pages/                 # halaman, dikelompokkan per area fitur
├── Controllers/               # AccountController (login/logout), REST API
├── Data/                      # ApplicationDbContext, DbInitializer
├── Models/                    # entitas
├── Services/
│   ├── Assistant/             # Semantic Kernel, konektor, plugin
│   ├── Payments/              # gateway pembayaran
│   └── *.cs                   # SchoolClock, AuditService, ToastService, dll.
├── wwwroot/                   # app.css, chat.css, *.js
├── docs/                      # dokumentasi ini
├── PLAN.md                    # peta jalan
└── Progress.md                # catatan kemajuan
```

---

## Masalah Umum

| Gejala | Penyebab | Solusi |
| --- | --- | --- |
| Halaman tampil tanpa gaya | Tidak ada koneksi internet — Tailwind dan font dimuat dari CDN | Sambungkan ke internet, atau host aset secara lokal |
| Grafik tidak muncul | Chart.js dari CDN gagal dimuat | Sama seperti di atas |
| `SQLite Error: no such table` | Skema lama setelah entitas berubah | Hapus `smsnet.db*` lalu jalankan ulang |
| Asisten menjawab "belum dikonfigurasi" | Kunci API belum diisi | Isi environment variable sesuai bagian di atas |
| "A second operation was started on this context" | Seharusnya tidak terjadi lagi — seluruh halaman kini memakai `IDbContextFactory` | Laporkan bila muncul, sertakan nama halamannya |
| Swagger 404 | Environment bukan Development | Setel `ASPNETCORE_ENVIRONMENT=Development` |
