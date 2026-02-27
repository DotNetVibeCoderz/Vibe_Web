# AISTracker - Vessel Monitoring System

AISTracker adalah sistem pemantauan kapal berbasis web yang dibuat dengan Blazor Server dan Radzen Components.

## Fitur Utama

- **Real-Time Tracking**: Pemantauan posisi kapal secara langsung (Simulasi).
- **Vessel Management**: CRUD data kapal.
- **Port Management**: CRUD data pelabuhan.
- **History Playback**: Melihat riwayat pergerakan kapal.
- **Reports**: Analitik dan visualisasi data.
- **API**: REST API untuk integrasi data AIS.
- **Authentication**: Login/Logout (Admin Default: `admin@aistracker.com` / `Admin123!`).

## Teknologi

- **Framework**: .NET 8 (Blazor Server / Interactive Server)
- **UI Library**: Radzen.Blazor
- **Database**: SQLite (Entity Framework Core)
- **Auth**: ASP.NET Core Identity

## Cara Menjalankan

1. Pastikan .NET 8 SDK terinstal.
2. Jalankan perintah berikut di terminal:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Buka browser di `https://localhost:7000` (atau port yang ditampilkan).
4. Login dengan user default:
   - Email: `admin@aistracker.com`
   - Password: `Admin123!`

## API Endpoints

Aplikasi menyediakan Minimal API untuk integrasi data. Swagger UI tersedia di `/swagger`.

- `GET /api/vessels`: Mendapatkan daftar kapal.
- `GET /api/vessels/{mmsi}`: Mendapatkan detail kapal berdasarkan MMSI.
- `POST /api/position`: Mengirim data posisi kapal (Lat, Lon, Speed, dll).

## Catatan

- Peta pada halaman "Live Tracking" menggunakan komponen Google Maps yang memerlukan API Key. Tanpa API Key, peta mungkin menampilkan watermark atau error, namun Grid View tetap berfungsi.
- Data pergerakan kapal disimulasikan oleh `AISTrackingService` yang berjalan di background.
