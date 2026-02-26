# MyLibrary LMS (Blazor Server)

## English
A responsive Library Management System built with **Blazor Server**, **SQLite**, and **EF Core** using a glassmorphism UI (light/dark theme). The project includes cataloging, members, circulation, inventory, acquisition, reporting, security, and community pages.

### Features
- Cataloging with metadata (MARC / Dublin Core)
- Smart search by title, author, ISBN, category
- Digital resource management and barcode/RFID section
- Member management and self-service portal
- Notifications & alerts
- Circulation (check-in/check-out)
- Inventory control & audit
- Acquisition & procurement
- Reporting & analytics
- Role-based access control & audit trail
- Backup & recovery module
- Community forum & events
- Master data CRUD page with export CSV, search, filter, sort, paging
- Authentication: Login, Register, Reset Password, Edit Profile

### Default Users
- **Admin**: `admin / admin123`
- Roles: `admin`, `petugas`, `anggota`

### Configuration
`appsettings.json`
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=library.db"
},
"FileStorage": {
  "Provider": "FileSystem",
  "BasePath": "storage"
}
```

### Run
```bash
dotnet restore
dotnet run
```

---

## Bahasa Indonesia
Sistem Manajemen Perpustakaan berbasis **Blazor Server**, **SQLite**, dan **EF Core** dengan desain glassmorphism (light/dark). Fitur sudah dibuat sesuai kebutuhan LMS.

### Fitur Utama
- Cataloging dengan metadata (MARC / Dublin Core)
- Smart search (judul, penulis, ISBN, kategori)
- Digital resource management & barcode/RFID
- Manajemen anggota & self-service
- Notifikasi & alert
- Sirkulasi (check-in/check-out)
- Inventory & audit
- Pengadaan buku
- Laporan & analitik
- Role-based access & audit trail
- Backup & recovery
- Komunitas (forum, event, rekomendasi)
- Master data CRUD dengan export CSV, search, filter, sort, paging
- Autentikasi: login, register, reset password, edit profil

### User Default
- **Admin**: `admin / admin123`
- Role: `admin`, `petugas`, `anggota`

### Menjalankan
```bash
dotnet restore
dotnet run
```

Selamat memakai! Jika mau traktir pulsa 😄 bisa lewat: https://studios.gravicode.com/products/budax
