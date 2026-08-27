# 💾 Konfigurasi Storage - Lapak

Unggahan (foto profil dan lampiran chat) melewati `StorageServiceFactory`, yang
memilih implementasi berdasarkan `Storage:Provider`.

## Status implementasi

| Provider | Status | Keterangan |
|----------|--------|------------|
| `FileSystem` | **Berjalan penuh** | Bawaan. Berkas disimpan di `wwwroot/uploads/` |
| `MinIO` | **Sebagian** | Unggah dan URL publik berjalan; `DeleteAsync` masih placeholder |
| `AmazonS3` | **Belum** | Saat ini dialihkan ke implementasi MinIO (protokol S3-compatible) |
| `AzureBlob` | **Belum** | Saat ini jatuh kembali ke `FileSystem` |

> Konfigurasi untuk keempatnya sudah ada, tetapi hanya `FileSystem` yang siap
> produksi. Untuk S3 atau Azure Blob sungguhan, tambahkan implementasi
> `IStorageService` baru lalu daftarkan di `StorageServiceFactory`.

## Cara memakainya dari kode

`IStorageService` **tidak** didaftarkan langsung di DI. Inject factory-nya:

```csharp
@inject Lapak.Services.Storage.StorageServiceFactory StorageFactory

var storage = StorageFactory.GetStorageService();
var storedName = await storage.UploadAsync(fileName, stream, contentType);
var publicUrl = await storage.GetPublicUrlAsync(storedName);
```

## Konfigurasi via appsettings.json

```json
{
  "Storage": {
    "Provider": "FileSystem",
    "FileSystem": {
      "RootPath": "wwwroot/uploads",
      "BaseUrl": "/uploads"
    },
    "MinIO": {
      "Endpoint": "localhost:9000",
      "AccessKey": "minioadmin",
      "SecretKey": "minioadmin",
      "BucketName": "lapak",
      "UseSsl": false
    },
    "AmazonS3": {
      "AccessKey": "your-aws-access-key",
      "SecretKey": "your-aws-secret-key",
      "BucketName": "lapak",
      "Region": "ap-southeast-1"
    },
    "AzureBlob": {
      "ConnectionString": "your-azure-connection-string",
      "ContainerName": "lapak"
    }
  }
}
```

## File System Storage

Default untuk development. File disimpan di folder `wwwroot/uploads/`.

**Setup:**
```bash
mkdir -p wwwroot/uploads
chmod 755 wwwroot/uploads
```

## MinIO Setup

```bash
# Docker
docker run -p 9000:9000 -p 9001:9001 \
  -e MINIO_ROOT_USER=minioadmin \
  -e MINIO_ROOT_PASSWORD=minioadmin \
  quay.io/minio/minio server /data --console-address ":9001"
```

## Amazon S3 Setup

1. Buat S3 bucket di AWS Console
2. Buat IAM user dengan policy S3 access
3. Konfigurasi Access Key & Secret Key
4. Set region bucket

## Azure Blob Setup

1. Buat Storage Account di Azure Portal
2. Buat Container dengan nama "lapak"
3. Copy Connection String dari Access Keys
