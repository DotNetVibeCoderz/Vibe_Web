# QueueKiosk (Neo-Brutalist Queue Management System)

A robust, full-stack queue management kiosk application developed in C# with Blazor Server. It incorporates a striking Neo-Brutalist UI design that supports both Light and Dark themes.

## Features Included
1. **Self-Service Check-In**: Kiosk home page for customers to take queue tickets.
2. **Ticket Issuance**: Printable HTML tickets with QR code. 
3. **Real-Time Queue Display**: Large TV-display mode for "Now Serving" counter with SignalR integration.
4. **Staff Dashboard**: Staff members can view waiting lists, call the next customer, complete, or skip.
5. **Priority Queuing**: Special button for VIP/disability priority routing.
6. **Data & Analytics**: Admin dashboard for total tickets, average waiting times, and service metrics.
7. **Master Data Management**: CRUD functionality for system Services.
8. **Minimal API + Swagger**: External integrations using API keys.

## Default Credentials
- **Username**: `admin`
- **Password**: `admin123`

## Tech Stack
- C# .NET 8 (Blazor Server)
- Entity Framework Core (SQLite)
- Minimal APIs with API Key Authentication
- QRCoder for QR Codes

## Instructions to Run
1. Ensure `.NET 8 SDK` is installed.
2. Build the project: `dotnet build`
3. Run the project: `dotnet run`
4. The SQLite database `queue.db` will be auto-created with seeded data.

---

# Kios Antrian (Sistem Antrian Neo-Brutalis)

Sistem antrian kios yang dikembangkan menggunakan C# dan Blazor Server dengan desain UI Neo-Brutalis yang mencolok. 

## Fitur
1. **Pendaftaran Mandiri**: Layar utama kios untuk pelanggan mengambil tiket antrian.
2. **Pencetakan Tiket**: Cetak tiket HTML dengan QR Code (didukung oleh QRCoder).
3. **Layar Antrian Real-Time**: Mode TV Display untuk "Sedang Melayani" di loket dengan SignalR (bawaan Blazor Server).
4. **Dashboard Petugas**: Staf dapat melihat daftar tunggu, memanggil pelanggan berikutnya, menyelesaikan, atau melewatinya.
5. **Jalur Prioritas**: Tombol khusus untuk rute prioritas VIP/disabilitas.
6. **Data & Analisis**: Dashboard admin untuk melihat total tiket, waktu tunggu rata-rata, dan metrik layanan.
7. **Manajemen Master Data**: Fungsi CRUD untuk Layanan sistem.
8. **Minimal API + Swagger**: Integrasi eksternal menggunakan API key.

## Kredensial Default
- **Username**: `admin`
- **Password**: `admin123`

## Cara Menjalankan
1. Pastikan `.NET 8 SDK` sudah terinstall.
2. Build project: `dotnet build`
3. Jalankan project: `dotnet run`
4. Database SQLite `queue.db` akan dibuat secara otomatis beserta datanya.
