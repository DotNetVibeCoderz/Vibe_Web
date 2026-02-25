# MyResto - Restaurant Management System (RMS)

## Description
MyResto is a comprehensive Restaurant Management System built with **Blazor Server (.NET 8)** and **MudBlazor** for a modern, responsive UI. It is designed to handle various aspects of restaurant operations including POS, Menu Management, Table Reservation, Inventory, Staff, and Reporting.

## Features
- **Dashboard**: Real-time overview of sales, orders, and table status.
- **Master Data**: Management of Categories, Products (Menu), and Tables.
- **Modern UI**: Dark/Light mode support using MudBlazor.
- **Database**: SQLite with Entity Framework Core (easy to migrate to SQL Server/PostgreSQL).
- **Authentication**: Built-in Identity support (Admin, Cashier, Waiter, Manager, Chef roles).
- **Scalable Architecture**: Service-based architecture with Repository pattern ready.

## Tech Stack
- **Framework**: .NET 8 (Blazor Server)
- **UI Component Library**: MudBlazor
- **Database**: SQLite (EF Core)
- **Identity**: ASP.NET Core Identity

## Getting Started

### Prerequisites
- .NET 8 SDK installed.

### Installation
1. Clone the repository.
2. Navigate to the project folder.
3. Run `dotnet restore`.
4. Run `dotnet run`.

The database (`MyResto.db`) will be automatically created and seeded with default data upon the first run.

### Default Credentials
- **Username/Email**: admin@myresto.com (or just 'admin')
- **Password**: admin123

## Project Structure
- `Components/Pages`: Application pages (Dashboard, Products, POS, etc.).
- `Components/Dialogs`: Modal dialogs for CRUD operations.
- `Data`: Database context and seeding logic.
- `Models`: Entity definitions.

---

# MyResto - Sistem Manajemen Restoran

## Deskripsi
MyResto adalah Sistem Manajemen Restoran (RMS) yang lengkap dibangun dengan **Blazor Server (.NET 8)** dan **MudBlazor** untuk antarmuka pengguna yang modern dan responsif. Aplikasi ini dirancang untuk menangani berbagai aspek operasional restoran termasuk POS, Manajemen Menu, Reservasi Meja, Inventaris, Staf, dan Pelaporan.

## Fitur
- **Dashboard**: Tinjauan real-time penjualan, pesanan, dan status meja.
- **Master Data**: Pengelolaan Kategori, Produk (Menu), dan Meja.
- **UI Modern**: Dukungan mode Gelap/Terang menggunakan MudBlazor.
- **Database**: SQLite dengan Entity Framework Core (mudah dimigrasi ke SQL Server/PostgreSQL).
- **Otentikasi**: Dukungan Identity bawaan (Peran Admin, Kasir, Waiter, Manajer, Chef).
- **Arsitektur Skalabel**: Arsitektur berbasis layanan yang siap dikembangkan.

## Teknologi
- **Framework**: .NET 8 (Blazor Server)
- **UI Library**: MudBlazor
- **Database**: SQLite (EF Core)
- **Identity**: ASP.NET Core Identity

## Cara Memulai

### Prasyarat
- .NET 8 SDK terinstal.

### Instalasi
1. Clone repositori ini.
2. Masuk ke folder proyek.
3. Jalankan `dotnet restore`.
4. Jalankan `dotnet run`.

Database (`MyResto.db`) akan otomatis dibuat dan diisi dengan data awal pada saat dijalankan pertama kali.

### Login Default
- **Username/Email**: admin@myresto.com (atau 'admin')
- **Password**: admin123
