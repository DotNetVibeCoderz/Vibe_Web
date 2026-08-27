# Penyimpanan berkas

Gambar produk dan logo toko disimpan lewat `IStorageService`, yang punya empat penyedia: **sistem berkas lokal**, **Azure Blob Storage**, **AWS S3**, dan **MinIO**.

Penyedia dipilih sekali saat aplikasi menyala. Ini konfigurasi infrastruktur, bukan pengaturan usaha, jadi tempatnya di `appsettings.json` dan bukan di halaman Pengaturan — memindahkan penyimpanan saat aplikasi berjalan akan membuat berkas lama tidak lagi dapat ditemukan.

## Konfigurasi

```json
"Storage": {
  "Provider": "FileSystem",
  "BucketOrContainerName": "mypos-uploads",
  "BaseUrl": "/uploads/",
  "MaxUploadMegabytes": 10,
  "ConnectionString": "",
  "AccessKey": "",
  "SecretKey": "",
  "Region": "ap-southeast-1",
  "ServiceUrl": "",
  "ForcePathStyle": true,
  "PublicBaseUrl": ""
}
```

| Kunci | Berlaku untuk | Keterangan |
|---|---|---|
| `Provider` | semua | `FileSystem`, `AzureBlob`, `AwsS3`, `MinIO` |
| `BucketOrContainerName` | S3, MinIO, Azure | Nama bucket atau container |
| `BaseUrl` | FileSystem | Awalan URL berkas, mis. `/uploads/` |
| `MaxUploadMegabytes` | semua | Batas ukuran unggahan |
| `ConnectionString` | Azure | Connection string Azure Storage |
| `AccessKey`, `SecretKey` | S3, MinIO | Dikosongkan untuk memakai rantai kredensial AWS |
| `Region` | S3, MinIO | Wilayah AWS, atau wilayah penandatanganan MinIO |
| `ServiceUrl` | MinIO | Alamat endpoint, mis. `http://localhost:9000` |
| `ForcePathStyle` | S3, MinIO | `true` untuk `endpoint/bucket/key` |
| `PublicBaseUrl` | S3, MinIO | Awalan URL publik bila memakai CDN atau domain sendiri |

## Sistem berkas lokal

Bawaan. Berkas disimpan di `wwwroot/uploads/` dan disajikan langsung oleh aplikasi.

```json
"Storage": { "Provider": "FileSystem", "BaseUrl": "/uploads/" }
```

Cocok untuk satu server. Perlu diingat: berkas tidak ikut terbawa bila aplikasi dipindahkan ke server lain, dan tidak dapat dipakai bersama oleh beberapa instans di belakang penyeimbang beban.

## Azure Blob Storage

```json
"Storage": {
  "Provider": "AzureBlob",
  "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
  "BucketOrContainerName": "mypos-uploads"
}
```

Container dibuat otomatis bila belum ada, dengan akses publik tingkat blob supaya gambar dapat dimuat langsung oleh peramban.

## AWS S3

```json
"Storage": {
  "Provider": "AwsS3",
  "BucketOrContainerName": "mypos-uploads",
  "Region": "ap-southeast-1",
  "AccessKey": "",
  "SecretKey": ""
}
```

Bila `AccessKey` dikosongkan, kredensial diambil dari rantai bawaan AWS SDK: variabel lingkungan, berkas profil, atau IAM role. **Inilah cara yang dianjurkan di produksi** — kunci tidak perlu tersimpan di berkas konfigurasi sama sekali.

URL yang dihasilkan berbentuk `https://{bucket}.s3.{region}.amazonaws.com/{key}`, atau memakai `PublicBaseUrl` bila diisi.

## MinIO

MinIO memakai protokol yang sama dengan S3, sehingga ditangani kelas yang sama. Yang membedakan hanya `ServiceUrl` dan gaya penulisan alamat:

```json
"Storage": {
  "Provider": "MinIO",
  "ServiceUrl": "http://localhost:9000",
  "BucketOrContainerName": "mypos-uploads",
  "AccessKey": "minioadmin",
  "SecretKey": "minioadmin",
  "Region": "us-east-1",
  "ForcePathStyle": true,
  "PublicBaseUrl": ""
}
```

Menjalankan MinIO secara lokal:

```bash
docker run -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  quay.io/minio/minio server /data --console-address ":9001"
```

Buat bucket `mypos-uploads` lewat konsol di `http://localhost:9001`, lalu atur kebijakannya menjadi dapat dibaca publik agar gambar produk dapat dimuat peramban.

Bila MinIO berada di belakang proxy dengan nama domain sendiri, isi `PublicBaseUrl` dengan alamat publiknya — `ServiceUrl` tetap memakai alamat internal yang dipakai aplikasi untuk mengunggah.

## Catatan implementasi

**Satu kelas untuk S3 dan MinIO.** `S3StorageService` melayani keduanya karena protokolnya identik. Menduplikasi kelas hanya untuk mengganti satu URL justru membuat dua jalur kode yang harus diperbaiki bersamaan setiap kali ada perubahan.

**Isi berkas disalin ke memori dulu.** S3 memerlukan panjang konten di muka, sedangkan aliran dari peramban tidak dapat dicari posisinya. Batas `MaxUploadMegabytes` menjaga agar penyalinan ini tidak pernah membesar tanpa batas.

**Kegagalan penghapusan tidak menggagalkan penyimpanan.** Bila objek lama tidak dapat dihapus, kejadiannya dicatat sebagai peringatan dan proses berlanjut — berkas yatim jauh lebih ringan akibatnya daripada penyimpanan produk yang gagal hanya karena gambar lamanya sudah tidak ada.

**Gambar lama dihapus setelah penyimpanan berhasil**, bukan sebelumnya, supaya kegagalan simpan tidak meninggalkan produk tanpa gambar.

## Menambah penyedia baru

1. Buat kelas yang mengimplementasikan `IStorageService` di `Services/`.
2. Tambahkan cabangnya pada `StorageSetup.AddMyPosStorage`.
3. Tambahkan kunci konfigurasi yang diperlukan ke `StorageConfig`.
