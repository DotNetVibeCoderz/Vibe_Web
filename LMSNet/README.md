# LMSNet
Online Course / Learning Management System built with Blazor Server, Tailwind CSS, Entity Framework Core, and SQLite.

## Features / Fitur

- **Peserta Didik (Students):** 
  - Course catalog & enrollment
  - Video lectures & multimedia content (Rich text preview)
  - Interactive quizzes & assignments
  - Progress tracking
  
- **Instruktur (Instructors):**
  - Course creation & content management
  - Student analytics & performance tracking
  - Live classes / webinar integration
  - Gradebook & feedback system
  
- **Administrasi (Admin):**
  - User management (roles: admin, mentor, student, creator)
  - Reporting & analytics dashboard
  - Master Data CRUD features with Export to Excel, sorting, and paging

- **Pendukung:**
  - Secure Authentication & role-based access
  - Dark/Light Theme support 
  - API support with Swagger
  - Accessible design

## Tech Stack
- C# & .NET 10 (Blazor Server)
- Tailwind CSS (via CDN)
- Entity Framework Core with SQLite
- Identity Authentication
- ClosedXML for Excel Export
- Swagger for API

## Setup & Run / Cara Menjalankan

### English
1. Ensure you have [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed.
2. Clone or open the project folder.
3. Run the application: `dotnet run` or open in Visual Studio and click Run.
4. Database will be automatically created (`lmsnet.db`) with rich sample data (Users and Courses) when the application starts.
5. **Default Credentials:**
   - **Admin:** `admin@lmsnet.com` / `Admin123!`
   - **Mentor:** `jacky@lmsnet.com` / `Mentor123!`
   - **Student:** `student@lmsnet.com` / `Student123!`
6. Access Swagger API documentation at `/swagger`.

### Bahasa Indonesia
1. Pastikan Anda telah menginstal [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Buka folder project ini.
3. Jalankan aplikasi menggunakan command: `dotnet run` atau buka di Visual Studio dan jalankan.
4. Database (`lmsnet.db`) dan data awal contoh yang lengkap (Pengguna dan Kursus) akan terbuat secara otomatis ketika aplikasi pertama kali dijalankan.
5. **Akun Default:**
   - **Admin:** `admin@lmsnet.com` / `Admin123!`
   - **Mentor:** `jacky@lmsnet.com` / `Mentor123!`
   - **Student:** `student@lmsnet.com` / `Student123!`
6. Buka halaman Swagger API di `/swagger`.

---
*Created by Gravicode Studios Team*
