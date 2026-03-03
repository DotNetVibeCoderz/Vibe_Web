# SportTracker 🏃‍♂️🚴‍♀️

![SportTracker](https://images.unsplash.com/photo-1552674605-db6ffd4facb5?w=1000&q=80)

## Overview (English)
**SportTracker** is a Strava-clone fitness & activity tracking application built with **Blazor Server** (.NET 8).
It features a bold, sporty brutalism design and supports both light and dark themes. 
The application allows users to record activities (Run, Ride, Swim, etc.), see activity feeds, view leaderboards, manage clubs, and provides administrative features with export to CSV capabilities.

### Key Features:
- **Activity Tracking**: Record distance, duration, elevation, and pace.
- **Social Feed**: View activities from people you follow, add likes (kudos) and comments.
- **Global Leaderboard**: Compete with other users based on total distance covered.
- **Clubs**: Discover and join running and cycling clubs.
- **Master Data (Admin)**: Full CRUD pages for Users and Activities with search, sort, pagination, and CSV export.
- **Minimal APIs + Swagger**: REST API endpoints available at `/swagger`.
- **Brutalism Design**: High contrast, bold typography (`Space Grotesk` font) using MudBlazor.
- **Storage Configuration**: Configurable file storage interface (FileSystem by default).

### Default Users (Seeded):
- **Admin**: `admin` / `admin123`
- **User**: `jane_doe` / `user123`

---

## Ringkasan (Bahasa Indonesia)
**SportTracker** adalah aplikasi pelacakan kebugaran & aktivitas (mirip Strava) yang dibangun menggunakan **Blazor Server** (.NET 8).
Aplikasi ini memiliki desain brutalisme yang *bold* dan *sporty*, serta mendukung tema terang dan gelap (Light/Dark mode).
Pengguna dapat merekam aktivitas (Lari, Sepeda, Renang, dll.), melihat *feed* aktivitas teman, melihat peringkat global (*leaderboard*), mengelola klub, dan memiliki panel admin yang dilengkapi dengan fitur ekspor ke CSV.

### Fitur Utama:
- **Pelacakan Aktivitas**: Rekam jarak, durasi, elevasi, dan *pace*.
- **Feed Sosial**: Lihat aktivitas dari orang yang diikuti, tambahkan *like* (Kudos) dan komentar.
- **Papan Peringkat Global**: Bersaing dengan pengguna lain berdasarkan total jarak tempuh.
- **Klub Komunitas**: Temukan dan bergabung dengan klub lari dan sepeda.
- **Master Data (Admin)**: Halaman CRUD lengkap untuk *Users* dan *Activities* dengan pencarian, pengurutan, paginasi, dan ekspor CSV.
- **Minimal API + Swagger**: Tersedia *endpoint* REST API yang dapat diakses melalui `/swagger`.
- **Desain Brutalisme**: Kontras tinggi, tipografi tebal (font `Space Grotesk`) menggunakan library MudBlazor.
- **Konfigurasi Penyimpanan**: Interface penyimpanan file yang dapat dikonfigurasi (Default: Sistem File Lokal).

### Pengguna Default (Awal):
- **Admin**: `admin` / `admin123`
- **User**: `jane_doe` / `user123`

---

## How to Run / Cara Menjalankan

1. **Install Prerequisites**: Ensure you have [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
2. **Restore Packages**: Open terminal in the project folder and run `dotnet restore`.
3. **Run Application**: Run `dotnet run` to start the app. The SQLite database will be created and seeded automatically.
4. **Access Swagger**: Go to `https://localhost:<port>/swagger` to view the Minimal API documentation.