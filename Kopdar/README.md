# Kopdar 🚀

![Kopdar Logo](wwwroot/images/logo.svg)

Kopdar is a real-time messaging and social media platform with a Neo-Brutalism design. Built efficiently using **C#**, **Blazor Server (Interactive Server)**, **Entity Framework Core (SQLite)**, and **Leaflet.js** for an interactive map experience. 

## Features 🌟
- **Timeline & Posting**: Share text updates and see what your friends are up to. Like and comment on posts in real-time.
- **Single & Group Chat**: Chat instantly with people and groups using Blazor Server's real-time SignalR capabilities.
- **People & Groups Nearby**: Find friends and communities around your location using the Haversine formula calculation.
- **Social Maps**: Interactive Leaflet.js maps displaying active public users and groups in your surrounding area.
- **Semantic Search Engine**: Intelligent search utilizing local TF-IDF & Cosine Similarity algorithm to match users based on their bios, hobbies, or job descriptions.
- **Customizable Profiles**: Edit your bio, gender, and track your followers.

## How to Run 💻
1. Ensure you have the **.NET 9/10 SDK** installed.
2. Clone or download this project.
3. Run `dotnet restore` to restore dependencies.
4. Run `dotnet run` inside the project folder.
5. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`.

*Note: The database (kopdar.db) will be automatically created and seeded with mock data upon the first run.*

---

# Kopdar (Bahasa Indonesia) 🇮🇩

Kopdar adalah platform pesan instan dan media sosial *real-time* bergaya desain **Neo-Brutalism** yang nyentrik dan mencolok. Dibangun menggunakan teknologi **C#**, **Blazor Server**, **Entity Framework Core (SQLite)**, dan peta interaktif **Leaflet.js**.

## Fitur Utama 🌟
- **Timeline & Posting**: Bagikan aktivitas seru kamu, berikan *like*, dan balas komentar secara *real-time*.
- **Chat Personal & Grup**: Mengobrol langsung dengan mulus tanpa perlu *refresh* halaman berkat teknologi SignalR dari Blazor.
- **Orang & Grup Sekitar (Nearby)**: Temukan teman baru atau komunitas publik yang lokasinya berada di dekatmu dengan pengaturan radius (Formula Haversine).
- **Peta Sosial (Maps)**: Tampilan peta Leaflet.js interaktif untuk melihat sebaran pengguna dan grup publik.
- **Pencarian Semantik (Semantic Search)**: Mesin pencari pintar tanpa AI external. Menggunakan perhitungan algoritma *TF-IDF* & *Cosine Similarity* bawaan untuk mencari *user* yang memiliki hobi atau bio yang mirip dengan kata kuncimu (contoh: "gamer", "suka kopi").
- **Profil Pengguna**: Lihat statistik *followers*, edit Bio, atur *Gender*, dan pantau daftar aktivitas *post* terakhirmu.

## Cara Menjalankan Aplikasi 💻
1. Pastikan komputer kamu sudah terinstal **.NET 9/10 SDK**.
2. Clone atau unduh project Kopdar ini.
3. Buka terminal/CMD, lalu jalankan perintah `dotnet restore`.
4. Jalankan aplikasi dengan `dotnet run`.
5. Buka web browser, lalu akses alamat `https://localhost:5001` atau `http://localhost:5000`.

*Catatan: Database SQLite (`kopdar.db`) akan ter-generate otomatis beserta data-data dummy untuk *testing* saat pertama kali aplikasi dijalankan.*

---
**Created by Jacky The Code Bender from Gravicode Studios ⚡**
