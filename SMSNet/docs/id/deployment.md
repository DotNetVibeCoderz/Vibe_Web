# Deployment

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/deployment.md)

---

## Daftar Periksa Sebelum Produksi

Jangan lewati bagian ini. Beberapa butir di bawah adalah masalah keamanan sungguhan.

| # | Butir | Status bawaan | Tindakan |
| --- | --- | --- | --- |
| 1 | Kata sandi admin | `admin123` | **Ganti segera** lewat Profil Saya |
| 2 | Halaman pendaftaran | Terbuka untuk umum, bisa memilih peran `admin` | Ubah `[AllowAnonymous]` menjadi `[Authorize(Roles = AppRoles.Admin)]` pada `Components/Pages/Auth/Register.razor` |
| 3 | Kunci API | Kosong | Isi lewat environment variable, **jangan** di appsettings |
| 4 | HTTPS | Tersedia | Wajib aktif; `UseHsts()` sudah menyala di luar Development |
| 5 | Migrasi basis data | Tidak ada | Pertimbangkan migrasi EF sebelum data sungguhan masuk — lihat catatan di bawah |
| 6 | Reset kata sandi | Langsung tanpa email | Ganti dengan tautan sekali pakai lewat email |
| 7 | Kredensial pembayaran | Tersimpan apa adanya di basis data | Pertimbangkan Data Protection atau brankas rahasia |
| 8 | Penyimpanan awan | `AzureBlob` dan `AwsS3` masih stub | Implementasikan bila akan dipakai |
| 9 | Cadangan | Tidak ada | Jadwalkan salinan berkas `smsnet.db` |
| 10 | Swagger | Hanya Development | Sudah benar — tidak perlu diubah |

---

## Masalah Migrasi Basis Data

Skema dibuat dengan `EnsureCreated()`, bukan migrasi EF Core. Artinya:

- Perubahan entitas **tidak** dapat diterapkan ke basis data yang sudah berisi data.
- Satu-satunya cara memperbarui skema adalah menghapus berkasnya, yang berarti
  **kehilangan seluruh data**.

Untuk sekolah sungguhan ini tidak dapat diterima. Beralih ke migrasi EF Core:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

lalu ganti pemanggilan `EnsureCreated()` pada `Program.cs`:

```csharp
// sebelum
dbContext.Database.EnsureCreated();

// sesudah
await dbContext.Database.MigrateAsync();
```

Lakukan ini **sebelum** data sungguhan dimasukkan.

---

## Menerbitkan Aplikasi

```bash
dotnet publish -c Release -o ./publish
```

Hasilnya berisi seluruh berkas yang diperlukan, termasuk `wwwroot`.

### Menjalankan

```bash
cd publish
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="http://0.0.0.0:5000"
export Assistant__OpenAI__ApiKey="sk-..."
dotnet SMSNet.dll
```

---

## Environment Variable

Pemisah tingkat adalah **dua garis bawah**.

```bash
# Wajib untuk produksi
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ConnectionStrings__DefaultConnection="Data Source=/var/lib/smsnet/smsnet.db"

# Asisten
Assistant__Provider=OpenAI
Assistant__OpenAI__ApiKey=sk-...
Assistant__Tavily__ApiKey=tvly-...

# Pembayaran
Payments__SandboxMode=false
Payments__Gateways__2__SecretKey=SB-Mid-server-...
```

---

## Docker

Belum ada `Dockerfile` di repositori. Berikut contoh yang berfungsi:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Basis data disimpan di volume agar tidak hilang saat kontainer diganti
VOLUME /data
ENV ConnectionStrings__DefaultConnection="Data Source=/data/smsnet.db"
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SMSNet.dll"]
```

```bash
docker build -t smsnet .
docker run -d -p 8080:8080 \
  -v smsnet-data:/data \
  -e Assistant__OpenAI__ApiKey="sk-..." \
  --name smsnet smsnet
```

---

## Reverse Proxy

Blazor Server memerlukan **WebSocket**. Konfigurasi proxy harus meneruskannya.

### Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name smsnet.sekolah.sch.id;

    ssl_certificate     /etc/letsencrypt/live/smsnet.sekolah.sch.id/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/smsnet.sekolah.sch.id/privkey.pem;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;

        # Wajib untuk circuit Blazor
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";

        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # Circuit yang menganggur tidak boleh diputus terlalu cepat
        proxy_read_timeout 100s;
    }
}
```

Bila memakai proxy, tambahkan penanganan header terusan pada `Program.cs`:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

---

## Ketergantungan CDN

Aplikasi memuat dari internet:

| Aset | Sumber |
| --- | --- |
| Tailwind CSS | `cdn.tailwindcss.com` |
| Google Fonts | `fonts.googleapis.com`, `fonts.gstatic.com` |
| Chart.js | `cdn.jsdelivr.net` |

Tanpa akses internet aplikasi **tetap berjalan**, tetapi tampil tanpa gaya dan tanpa
grafik. Untuk sekolah dengan jaringan tertutup, unduh ketiganya ke `wwwroot` lalu ubah
rujukannya di `Components/App.razor`.

Perlu dicatat: Tailwind dari CDN memang tidak ditujukan untuk produksi. Untuk instalasi
serius, pertimbangkan membangun berkas CSS sendiri — meskipun itu berarti memperkenalkan
proses build Node yang selama ini sengaja dihindari.

---

## Cadangan

Seluruh data ada dalam satu berkas SQLite.

```bash
# Salinan aman saat aplikasi berjalan
sqlite3 /var/lib/smsnet/smsnet.db ".backup '/backup/smsnet-$(date +%F).db'"
```

Sertakan juga direktori `wwwroot/uploads` yang berisi lampiran percakapan dan dokumen.

---

## Pemantauan

Log ditulis ke konsol dan mengikuti pengaturan pada `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

Halaman galat menampilkan **kode permintaan** (trace identifier) yang dapat dicocokkan
dengan baris log — minta pengguna menyertakannya saat melapor.

---

## Penyetelan Kinerja

| Aspek | Saran |
| --- | --- |
| SQLite | Aktifkan mode WAL untuk pembacaan bersamaan yang lebih baik |
| Circuit Blazor | Batasi jumlah circuit bersamaan bila jumlah pengguna besar |
| Basis data lain | `ApplicationDbContext` tidak mengandung hal khusus SQLite; berpindah ke PostgreSQL cukup mengganti pemanggilan `UseSqlite` |
| Aset statis | `MapStaticAssets()` sudah menangani fingerprint dan kompresi |
