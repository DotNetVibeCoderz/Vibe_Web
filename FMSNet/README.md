# FMSNet - Fleet Management System

## Overview
FMSNet is a robust Fleet Management System built with C# Blazor Server, featuring a brutalist design and real-time capabilities. It provides comprehensive tools for monitoring vehicle locations, managing drivers, scheduling maintenance, and tracking fuel consumption.

## Features
- **GPS Vehicle Tracking**: Real-time tracking using Leaflet.js and simulated updates.
- **Route Optimization**: Basic route display and simulated movement.
- **Driver Behavior Monitoring**: Safety scores and status management.
- **Fuel Management**: Log fuel consumption and costs.
- **Maintenance Scheduling**: Track and schedule vehicle maintenance.
- **Compliance & Safety**: Manage driver licenses and vehicle inspections.
- **API Integration**: REST API endpoints for external integration with API Key authentication.
- **Brutalist Design**: Unique, high-contrast, professional UI.
- **Light/Dark Mode**: Adaptive theme support.

## Technologies
- **Backend**: .NET 8, Entity Framework Core, SQLite.
- **Frontend**: Blazor Server, Bootstrap, Custom CSS (Brutalist).
- **Maps**: Leaflet.js (via JS Interop).
- **Authentication**: ASP.NET Core Identity.
- **API**: Minimal API with Swagger.

## Getting Started

### Prerequisites
- .NET 8 SDK installed.

### Installation
1. Clone the repository.
2. Navigate to the project folder.
3. Run `dotnet restore`.
4. Run `dotnet run`.

### Default Credentials
- **Username**: `admin@fmsnet.com` (Example, create one via Register if needed)
- **Password**: `Admin123!` (Example)
- **API Key**: `FMS-API-KEY-12345` (Header: `X-API-KEY`)

## API Usage
Access Swagger UI at `/swagger` to explore endpoints.
Example:
- `GET /api/vehicles` - List all vehicles.
- `POST /api/vehicles/location` - Update vehicle location.

## License
MIT License.

---

# FMSNet - Sistem Manajemen Armada

## Ringkasan
FMSNet adalah Sistem Manajemen Armada yang kuat yang dibangun dengan C# Blazor Server, menampilkan desain brutalis dan kemampuan real-time. Ini menyediakan alat komprehensif untuk memantau lokasi kendaraan, mengelola pengemudi, menjadwalkan perawatan, dan melacak konsumsi bahan bakar.

## Fitur
- **Pelacakan Kendaraan GPS**: Pelacakan real-time menggunakan Leaflet.js dan pembaruan simulasi.
- **Optimasi Rute**: Tampilan rute dasar dan simulasi pergerakan.
- **Pemantauan Perilaku Pengemudi**: Skor keselamatan dan manajemen status.
- **Manajemen Bahan Bakar**: Catat konsumsi bahan bakar dan biaya.
- **Penjadwalan Perawatan**: Lacak dan jadwalkan perawatan kendaraan.
- **Kepatuhan & Keselamatan**: Kelola lisensi pengemudi dan inspeksi kendaraan.
- **Integrasi API**: Endpoint REST API untuk integrasi eksternal dengan otentikasi API Key.
- **Desain Brutalis**: UI yang unik, kontras tinggi, dan profesional.
- **Mode Terang/Gelap**: Dukungan tema adaptif.

## Teknologi
- **Backend**: .NET 8, Entity Framework Core, SQLite.
- **Frontend**: Blazor Server, Bootstrap, Custom CSS (Brutalis).
- **Peta**: Leaflet.js (via JS Interop).
- **Otentikasi**: ASP.NET Core Identity.
- **API**: Minimal API dengan Swagger.

## Memulai

### Prasyarat
- .NET 8 SDK terinstal.

### Instalasi
1. Clone repositori.
2. Navigasi ke folder proyek.
3. Jalankan `dotnet restore`.
4. Jalankan `dotnet run`.

### Kredensial Default
- **Username**: `admin@fmsnet.com` (Contoh, buat satu melalui Daftar jika diperlukan)
- **Password**: `Admin123!` (Contoh)
- **API Key**: `FMS-API-KEY-12345` (Header: `X-API-KEY`)

## Penggunaan API
Akses Swagger UI di `/swagger` untuk menjelajahi endpoint.
Contoh:
- `GET /api/vehicles` - Daftar semua kendaraan.
- `POST /api/vehicles/location` - Perbarui lokasi kendaraan.

## Lisensi
Lisensi MIT.