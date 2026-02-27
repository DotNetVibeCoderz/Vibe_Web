# EasyParking Management System

Welcome to **EasyParking** - a Neo-Brutalist, high-performance, and feature-rich parking management application built with **Blazor Server** and **.NET 8**.

## Features

- **Neo-Brutalism Design**: Modern, bold, contrasting colors with solid shadows.
- **Dark/Light Theme**: Accessible and beautiful UI switching seamlessly.
- **Authentication & Authorization**: Integrated role-based access control (Admin, Operator, Pelanggan).
- **Master Data Management**: CRUD functionality for users, parking slots, etc.
- **Real-Time Monitoring**: Live visualization of available, occupied, reserved, and EV slots.
- **Transactions & Billing**: Simplified check-in and automated check-out billing based on time.
- **Export Data**: CSV export functionality built-in for master records.
- **SQLite + Entity Framework Core**: Fast, lightweight, and easy to run right out of the box.

## Getting Started

1. **Prerequisites**: Ensure you have .NET 8 SDK installed.
2. **Build**: Run `dotnet build`.
3. **Run**: Run `dotnet run`. The application will automatically create the `easyparking.db` and seed the initial data.
4. **Login Accounts**:
   - **Admin**: `admin` / `admin123`
   - **Operator**: `operator1` / `password123`
   - **Customer**: `user1` / `password123`

## Project Structure

- `Data`: EF Core DbContext and Database Seeder.
- `Models`: Database Entities (`User`, `ParkingSlot`, `ParkingTransaction`).
- `Pages`: Blazor components for each route (Dashboard, Monitoring, Transactions, Master Data).
- `Services`: Business logic services, Auth State Provider, File Storage Abstraction.
- `wwwroot/css`: Custom CSS for Neo-Brutalism styling (`site.css`).

## Note from the Developer (Jacky)
Hello! I'm **Jacky The Code Bender**, created by Gravicode Studios. I hope you enjoy this application! If you like my work, maybe treat me to some mobile data top-up? 😉 (https://studios.gravicode.com/products/budax)

---

# Sistem Manajemen EasyParking (Bahasa Indonesia)

Selamat datang di **EasyParking** - aplikasi manajemen parkir berdesain Neo-Brutalist yang tangguh, dibangun menggunakan **Blazor Server** dan **.NET 8**.

## Fitur Utama

- **Desain Neo-Brutalism**: Modern, berani, dengan warna kontras dan bayangan tebal.
- **Tema Gelap / Terang**: Dukungan light/dark mode untuk kenyamanan pengguna.
- **Autentikasi & Otorisasi**: Akses berbasis peran (Admin, Operator, Pelanggan).
- **Master Data**: Fitur CRUD untuk data slot parkir, pengguna, dengan fitur export ke CSV.
- **Monitoring Real-Time**: Visualisasi langsung ketersediaan slot (Kosong, Terisi, Dipesan, dan Charging EV).
- **Transaksi**: Proses Check-in dan Check-out dengan perhitungan tarif otomatis.
- **Database**: Menggunakan SQLite dengan Entity Framework Core agar mudah dijalankan tanpa setup database rumit.

## Cara Menjalankan

1. **Syarat**: Pastikan .NET 8 SDK sudah terinstal.
2. **Build**: Jalankan perintah `dotnet build`.
3. **Run**: Jalankan perintah `dotnet run`. Sistem akan otomatis membuat database `easyparking.db` dan mengisi data awal (seeding).
4. **Akun Login Default**:
   - **Admin**: `admin` / `admin123`
   - **Operator**: `operator1` / `password123`
   - **Pelanggan**: `user1` / `password123`

## Pesan dari Jacky
Halo! Saya **Jacky The Code Bender**, dibuat oleh tim Gravicode Studios yang dipimpin oleh Kang Fadhil. Semoga aplikasi ini bermanfaat! Kalau kamu terbantu, boleh dong traktir saya pulsa sedikit di sini: https://studios.gravicode.com/products/budax 😁
