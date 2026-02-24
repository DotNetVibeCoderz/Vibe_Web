# MyClinic - Aplikasi Manajemen Puskesmas & Klinik

A full-stack, comprehensive Clinic Management Application (Sistem Manajemen Puskesmas & Klinik) built with Blazor Server, Entity Framework Core (SQLite), Identity, and MudBlazor for an elegant UI experience.

## Fitur Utama / Key Features
- **Authentikasi & Otorisasi**: Login, Registrasi, Reset Password, Role (Admin, Doctor, dsb).
- **Registrasi Pasien**: Pendaftaran dan pencatatan riwayat dasar dengan Nomor Rekam Medis (RM).
- **Rekam Medis Elektronik (EMR)**: Pencatatan diagnosis, catatan dokter, dan tindakan medis secara lengkap.
- **Jadwal & Antrian**: Sistem antrian pintar dengan dukungan fitur telemedicine.
- **Apotek & Resep Digital**: Manajemen stok obat dan e-Prescription.
- **Laboratorium**: Pencatatan hasil test darah, urine, rontgen, dsb.
- **Billing & BPJS**: Tagihan pembayaran (Cash, Transfer) dan simulasi status klaim BPJS.
- **Manajemen SDM**: Data dokter, tenaga medis, dan perawat.
- **Desain Modern (MudBlazor)**: UI responsif dan mendukung fitur **Dark/Light Theme**.
- **Data Tables Terpadu**: Pencarian full-text per kolom dan dukungan paging untuk mengelola data ribuan baris dengan mudah.
- **Simulasi Integrasi Dinkes**: Sinkronisasi data epidemiologi dan antrian online yang siap dihubungkan melalui API.

## Cara Menjalankan (How to Run)

### Prasyarat / Prerequisites
- .NET 8 SDK atau versi terbaru.
- Visual Studio / VS Code / JetBrains Rider.

### Langkah Eksekusi / Execution Steps
1. Buka folder/project `MyClinic`.
2. Lakukan build project:
   ```bash
   dotnet build
   ```
3. Jalankan aplikasi (Database SQLite dan seed data akan di-generate otomatis saat pertama kali run):
   ```bash
   dotnet run
   ```
4. Buka browser dan arahkan ke: `https://localhost:7000` (atau port yang tertera pada console).

### Akun Default / Default Accounts
Sistem akan otomatis menginjeksi akun `Admin` pada saat pertama kali berjalan:
- **Email/Username**: `admin@clinic.com` / `admin`
- **Password**: `admin123`

## Teknologi yang Digunakan
- **Blazor Server** (C#)
- **MudBlazor** (Component Library UI)
- **Entity Framework Core 8** + SQLite
- **ASP.NET Core Identity**
- **Bogus** (Data Faker Generator untuk Dummy Data simulasi)

## Pengembangan Selanjutnya
Database default saat ini menggunakan **SQLite** untuk memudahkan demo dan portabilitas, tapi sistem siap dihubungkan ke **SQL Server, PostgreSQL, maupun MySQL** dengan cukup mengubah connection string dan provider pada `Program.cs`.

---

Dibuat dengan ❤️ oleh Jacky The Code Bender (Tim Gravicode Studios).
