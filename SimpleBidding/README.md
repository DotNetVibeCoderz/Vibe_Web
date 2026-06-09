# SimpleBidding - Blazor Auction Platform

Aplikasi lelang real-time berbasis web yang dibangun dengan .NET Blazor Server, SQLite, dan Entity Framework Core.

## ✨ Fitur Utama (Features)
- **Authentication**: Login/Register menggunakan ASP.NET Core Identity.
- **Role Management**: Admin, Seller, dan Bidder.
- **Real-time Auction**: Sistem penawaran langsung dengan countdown timer.
- **Modern UI**: Desain responsif menggunakan Bootstrap dengan dukungan Light & Dark mode.
- **Data Persistence**: Menggunakan SQLite untuk database yang ringan.
- **Audit Logs**: Mencatat aktivitas penawaran user.

## 🚀 Cara Menjalankan (How to Run)
1. Pastikan .NET SDK sudah terinstall.
2. Jalankan perintah:
   ```bash
   dotnet run
   ```
3. Buka browser ke alamat `https://localhost:5001` atau `http://localhost:5000`.

## 👥 Akun Demo (Demo Accounts)
| Role | Email | Password |
|------|-------|----------|
| Admin | admin@bidding.com | Password123! |
| Seller | seller@bidding.com | Password123! |
| Bidder | bidder@bidding.com | Password123! |

---

Developed by **Jacky the Code Bender** from **Gravicode Studios** (Lead by Kang Fadhil).
Want to support? Traktir pulsa di: [https://studios.gravicode.com/products/budax](https://studios.gravicode.com/products/budax)

---

# SimpleBidding - Platform Lelang Blazor

## 🇮🇩 Deskripsi (Bahasa Indonesia)
Ini adalah platform lelang sederhana namun fungsional. User dapat melihat daftar barang, masuk ke detail barang, dan melakukan penawaran harga. Sistem akan memvalidasi agar harga penawaran baru selalu lebih tinggi dari harga sebelumnya.

## 🛠️ Tech Stack
- **Framework**: .NET 8.0 / 9.0 Blazor Server
- **Database**: SQLite
- **ORM**: Entity Framework Core
- **Identity**: Microsoft Identity UI (Customized)
- **Frontend**: Bootstrap 5 + Custom CSS (Dark/Light Mode)
