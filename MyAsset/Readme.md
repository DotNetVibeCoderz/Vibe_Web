# MyAsset - Asset Management System

MyAsset is a modern, glassmorphism-styled Asset Management System built with Blazor Server and .NET 8. It features comprehensive asset tracking, lifecycle management, maintenance scheduling, and reporting.

## Features

- **Asset Registry**: Track assets with details, images, location, and status.
- **Lifecycle Tracking**: Purchase to disposal tracking.
- **Maintenance**: Schedule and log maintenance activities.
- **Dashboard**: Real-time overview of asset value, health, and distribution.
- **Reporting**: Depreciation and cost analysis.
- **User Management**: Role-based access control (Admin, Manager, Operator).
- **Glassmorphism UI**: Modern and professional interface with Light/Dark mode.

## Technologies

- **Framework**: .NET 8 (Blazor Server)
- **Database**: SQLite (Entity Framework Core)
- **UI Component Library**: MudBlazor
- **Authentication**: ASP.NET Core Identity

## Getting Started

1. **Prerequisites**: Ensure you have .NET 8 SDK installed.
2. **Clone/Download**: Get the project files.
3. **Run the Application**:
   ```bash
   dotnet run
   ```
   The application will automatically create the SQLite database (`myasset.db`) and seed initial data.

4. **Login**:
   - Navigate to `https://localhost:5001` (or the port shown in console).
   - **Username**: `admin`
   - **Password**: `Admin123!`

## Roles & Permissions

- **Admin**: Full access to all modules, including User Management and Categories.
- **Manager/Operator**: Access to Asset Registry, Maintenance, and Reports.

## Project Structure

- `Pages/`: Blazor pages (Assets, Dashboard, Maintenance, etc.)
- `Models/`: Entity models (Asset, Category, MaintenanceLog).
- `Data/`: DB Context and Seeder.
- `Shared/`: Layouts and navigation.
- `wwwroot/css`: Custom CSS for glassmorphism effects.

---

# MyAsset - Sistem Manajemen Aset

MyAsset adalah sistem manajemen aset modern dengan gaya desain glassmorphism yang dibangun menggunakan Blazor Server dan .NET 8.

## Fitur

- **Registrasi Aset**: Lacak aset dengan detail lengkap, gambar, lokasi, dan status.
- **Siklus Hidup**: Pemantauan dari pembelian hingga pembuangan.
- **Pemeliharaan**: Jadwal dan log aktivitas maintenance.
- **Dashboard**: Tinjauan real-time nilai aset, kesehatan, dan distribusi.
- **Laporan**: Analisis depresiasi dan biaya.
- **Manajemen Pengguna**: Kontrol akses berbasis peran.
- **UI Glassmorphism**: Antarmuka modern dengan mode Terang/Gelap.

## Cara Menjalankan

1. Pastikan .NET 8 SDK terinstal.
2. Jalankan perintah:
   ```bash
   dotnet run
   ```
3. Database akan dibuat otomatis.
4. Login dengan akun default:
   - **User**: `admin`
   - **Pass**: `Admin123!`
