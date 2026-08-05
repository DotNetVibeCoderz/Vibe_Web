Aplikasi Manajemen Sekolah (School Management System) dengan fitur:  

Fitur Akademik
- Manajemen Kurikulum & Jadwal – pengaturan mata pelajaran, kelas, dan kalender akademik  
- Absensi Siswa & Guru – pencatatan kehadiran otomatis (barcode, RFID, biometrik)  
- Penilaian & Rapor Digital – input nilai, analisis hasil belajar, dan rapor online  
- E-learning Integration – modul pembelajaran online, kuis, dan ujian berbasis sistem  

Fitur untuk Guru & Staff
- Dashboard Guru – akses jadwal, daftar siswa, dan materi ajar  
- Manajemen Tugas & Ujian – upload soal, koreksi otomatis, dan feedback digital  
- Komunikasi Internal – forum diskusi, chat, dan pengumuman antar guru/staff  
- Evaluasi Kinerja Guru – laporan performa dan KPI  

Fitur untuk Orang Tua & Siswa
- Portal Orang Tua – memantau absensi, nilai, dan aktivitas anak  
- Notifikasi Real-Time – pengingat ujian, pembayaran, atau kegiatan sekolah  
- E-Payment – pembayaran SPP, buku, dan kegiatan sekolah via e-wallet/kartu  
- Sertifikat & Dokumen Digital – akses ijazah, rapor, dan surat resmi  

Fitur Administrasi & Keuangan
- Manajemen Keuangan – pencatatan SPP, denda, dan laporan keuangan sekolah  
- Inventory Management – pengelolaan aset sekolah (buku, laboratorium, fasilitas)  
- Payroll System – gaji guru dan staff otomatis  
- Laporan Keuangan – analitik pendapatan dan pengeluaran  

Fitur Analitik & Laporan
- Dashboard Administrasi – overview siswa, guru, keuangan, dan kegiatan sekolah  
- Data Analytics – tren kehadiran, performa akademik, dan efisiensi operasional  
- Custom Reports – laporan sesuai kebutuhan (akademik, keuangan, SDM)  

Fitur Keamanan & Akses
- Role-Based Access Control – hak akses berbeda untuk admin, guru, siswa, dan orang tua  
- Audit Trail – pencatatan aktivitas untuk keamanan  

Fitur Tambahan
- Integrasi dengan Sistem Lain – menyediakan Rest API yang menyediakan akses ke data dengan minapi dan swagger
- Event & Extracurricular Management – manajemen kegiatan sekolah dan organisasi siswa  

- Autentikasi
  Login, Register User, Reset Password, Edit Profile

- Master Data
  Halaman CRUD untuk table master data dilengkapi fitur export excel/csv, free text search, filter per kolom, sorting, paging

Notes:
- Buat semua halaman yang diperlukan untuk fitur-fitur diatas sampai selesai
- Dibuat dengan blazor server dan desain modern dan professional menggunakan tailwind css
- Dukungan light / dark theme
- Database dengan SQLite dan EF (bisa menggunakan database lain di masa depan)
- File storage bisa menggunakan FileSystem, Azure Blob atau AWS S3 (bisa di konfigurasi dari app setting, default: FileSystem)
- Berikan data awal contoh yang cukup banyak 
- Tambahkan readme.md (English dan bahasa indonesia)
- User admin default: admin / admin123
- Role user: admin, guru, siswa, dan orangtua  
