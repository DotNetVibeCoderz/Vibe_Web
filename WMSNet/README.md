# WMSNet - Workshop Management System

Aplikasi Manajemen Bengkel modern yang dibangun dengan Blazor Server, C#, SQLite, dan Tailwind CSS.

## Fitur Utama

### Operasional
- **Job Tracking**: Pantau status pekerjaan (Pending, In Progress, Completed).
- **Manajemen Pelanggan**: Database pelanggan dan riwayat kendaraan.
- **Penjadwalan**: (Mock) Kalender reservasi.

### Keuangan
- **Invoicing**: Pembuatan tagihan otomatis.
- **Estimasi Biaya**: Perhitungan biaya sparepart dan jasa.

### Analitik & Inventory
- **Dashboard**: Statistik real-time pendapatan dan pekerjaan.
- **Inventory**: Stok sparepart dengan peringatan stok menipis.
- **Export Excel**: Fitur export data inventory.

## Teknologi
- **Framework**: .NET 6/7/8 (Blazor Server)
- **Database**: SQLite (via Entity Framework Core)
- **UI**: Tailwind CSS (via CDN) + HTML/CSS
- **API**: Minimal API + Swagger

## Cara Menjalankan

1. Pastikan .NET SDK sudah terinstall.
2. Buka terminal di folder project.
3. Jalankan perintah:
   ```bash
   dotnet run
   ```
4. Aplikasi akan berjalan di `https://localhost:7193` (atau port lain yang tersedia).
5. Database `app.db` akan dibuat otomatis dan diisi data contoh saat pertama kali dijalankan.

## User Default
- **Username**: `admin`
- **Password**: `Admin123!`

---

## English Description

WMSNet is a modern Workshop Management System built with Blazor Server, C#, SQLite, and Tailwind CSS.

### Key Features
- **Job Tracking**: Real-time status updates.
- **Customer Management**: Complete history of customers and vehicles.
- **Inventory**: Real-time stock management with low-stock alerts.
- **Financials**: Invoicing and revenue tracking.

### How to Run
1. Install .NET SDK.
2. Run `dotnet run` in the project directory.
3. Access via browser.
4. Default Admin: `admin` / `Admin123!`
