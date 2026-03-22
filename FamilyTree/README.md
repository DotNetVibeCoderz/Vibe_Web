# FamilyTree (Blazor Server)

## English
FamilyTree is a Neo Brutalism styled family tree app built with Blazor Server, EF Core, and SQLite. It supports tree visualization, member profiles, memories, analytics, and backup/restore in JSON.

### Default Admin
- **Username:** admin
- **Password:** admin123

### Features
- Drag & drop tree builder (demo)
- Member profiles with encrypted notes
- Multimedia and storytelling storage
- Insights & demographic analytics
- Export/import JSON backup
- Light/Dark theme toggle
- Role-based authentication (admin/user)

### Storage Providers
Configure in `appsettings.json`:
```json
"Storage": {
  "Provider": "FileSystem",
  "UploadRoot": "wwwroot/uploads"
}
```
Supported: `FileSystem`, `AzureBlob`, `AwsS3` (placeholders ready for integration).

### Run
```bash
dotnet restore
dotnet run
```

---

## Bahasa Indonesia
FamilyTree adalah aplikasi pohon keluarga dengan gaya Neo Brutalism yang dibuat menggunakan Blazor Server, EF Core, dan SQLite. Mendukung visualisasi tree, profil anggota, memori keluarga, analitik, serta backup/restore JSON.

### Admin Default
- **Username:** admin
- **Password:** admin123

### Fitur
- Drag & drop tree builder (demo)
- Profil anggota dengan catatan terenkripsi
- Penyimpanan multimedia & storytelling
- Insight & analitik demografi
- Export/import backup JSON
- Light/Dark theme toggle
- Autentikasi berbasis role (admin/user)

### Storage Providers
Konfigurasi di `appsettings.json`:
```json
"Storage": {
  "Provider": "FileSystem",
  "UploadRoot": "wwwroot/uploads"
}
```
Provider yang didukung: `FileSystem`, `AzureBlob`, `AwsS3` (placeholder siap diintegrasikan).

### Menjalankan
```bash
dotnet restore
dotnet run
```
