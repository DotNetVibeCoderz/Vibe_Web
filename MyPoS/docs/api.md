# REST API

Antarmuka integrasi untuk aplikasi luar: katalog produk, kategori, pelanggan, transaksi, dan laporan. Dibangun dengan Minimal API dan didokumentasikan lewat Swagger.

![Swagger UI](screenshots/12-swagger.png)

- **Dokumentasi interaktif:** `/swagger`
- **Dokumen OpenAPI:** `/swagger/v1/swagger.json`
- **Awalan rute:** `/api/v1` (dapat diubah lewat `Api:RoutePrefix`)

## Otentikasi

Setiap permintaan wajib menyertakan kunci pada header `X-Api-Key`:

```bash
curl -H "X-Api-Key: mps_xxxxxxxxxxxxxxxx" \
     http://localhost:5296/api/v1/products?activeOnly=true
```

Kunci dibuat dari **Pengaturan → API** di dalam aplikasi, oleh pengguna dengan peran Admin.

![Pengelolaan kunci API](screenshots/11-pengaturan-api.png)

### Cara kunci disimpan

Kunci penuh **tidak pernah disimpan**. Yang tersimpan hanyalah hash PBKDF2-nya, persis seperti kata sandi pengguna. Dua belas karakter pertamanya disimpan terpisah sebagai `Prefix` supaya verifikasi tetap satu kueri berindeks, tanpa perlu mencocokkan hash seluruh baris.

Karena itu, kunci hanya diperlihatkan **satu kali** saat dibuat. Bila hilang, buat kunci baru dan hapus yang lama.

### Izin

Setiap kunci punya salah satu dari dua tingkat izin:

| Izin | Boleh | Ditolak |
|---|---|---|
| **Baca saja** | GET | POST, PUT, PATCH, DELETE → `403` |
| **Baca & tulis** | semua metode | — |

Integrasi pelaporan sebaiknya diberi kunci baca saja: tidak ada risiko kunci tersebut mengubah stok atau membuat transaksi.

Kunci juga dapat diberi tanggal kedaluwarsa dan dapat dinonaktifkan sementara tanpa dihapus. Waktu pemakaian terakhir dicatat, sehingga kunci yang sudah tidak dipakai mudah ditemukan.

### Kode status

| Kode | Arti |
|---|---|
| `401` | Header `X-Api-Key` tidak ada, atau kunci tidak dikenal / nonaktif / kedaluwarsa |
| `403` | Kunci hanya berizin baca, tetapi metodenya mengubah data |
| `400` | Isi permintaan tidak sah — pesannya menjelaskan apa yang salah |
| `404` | Sumber daya tidak ditemukan |
| `409` | Bentrok, mis. nama kategori yang sudah ada |

Bentuk galat selalu sama:

```json
{ "message": "Barcode 8992761111038 sudah dipakai produk lain.", "detail": null }
```

## Daftar endpoint

### Produk

| Metode | Rute | Keterangan |
|---|---|---|
| GET | `/products` | Daftar berhalaman. Parameter: `search`, `categoryId`, `activeOnly`, `lowStockOnly`, `page`, `pageSize` |
| GET | `/products/{id}` | Satu produk |
| GET | `/products/barcode/{barcode}` | Cari berdasarkan barcode |
| POST | `/products` | Buat produk |
| PUT | `/products/{id}` | Perbarui produk |
| POST | `/products/{id}/stock` | Sesuaikan stok — `{ "delta": 15, "reason": "Penerimaan barang" }` |
| DELETE | `/products/{id}` | Hapus; produk yang pernah terjual hanya dinonaktifkan |

### Kategori

| Metode | Rute |
|---|---|
| GET | `/categories` |
| POST | `/categories` |
| PUT | `/categories/{id}` |
| DELETE | `/categories/{id}` |

### Pelanggan

| Metode | Rute |
|---|---|
| GET | `/customers` — parameter `search`, `page`, `pageSize` |
| GET | `/customers/{id}` |
| POST | `/customers` |
| PUT | `/customers/{id}` |
| DELETE | `/customers/{id}` |

### Transaksi

| Metode | Rute | Keterangan |
|---|---|---|
| GET | `/transactions` | Parameter: `from`, `to`, `status`, `search`, `page`, `pageSize` |
| GET | `/transactions/{id}` | Satu transaksi beserta rincian barangnya |
| GET | `/transactions/invoice/{invoiceNumber}` | Cari berdasarkan nomor invoice |
| POST | `/transactions` | Buat transaksi baru |
| POST | `/transactions/{id}/void` | Batalkan — `{ "reason": "Salah input" }` |
| POST | `/transactions/{id}/refresh-status` | Tanyakan ulang status ke penyedia pembayaran |

`status` menerima `Pending`, `Paid`, `Failed`, `Voided`, `Refunded`.

### Laporan

| Metode | Rute | Keterangan |
|---|---|---|
| GET | `/reports/summary` | Omzet, harga pokok, laba kotor, margin, pajak, diskon |
| GET | `/reports/daily` | Deret harian lengkap termasuk hari tanpa penjualan |
| GET | `/reports/by-product` | Penjualan per produk, parameter `categoryId` dan `top` |
| GET | `/reports/low-stock` | Produk yang perlu diisi ulang |
| GET | `/reports/store-info` | Mata uang, aturan pajak, dan metode pembayaran yang aktif |

Endpoint laporan menerima `from` dan `to` (format `yyyy-MM-dd`); bila tidak diisi, dipakai 30 hari terakhir. Semuanya hanya menghitung transaksi berstatus **lunas**.

## Membuat transaksi lewat API

Endpoint ini menjalankan **alur yang sama persis dengan halaman kasir**: validasi stok, perhitungan pajak lewat `TaxCalculator`, pemanggilan penyedia pembayaran, pengurangan stok, dan pemberian poin loyalitas — semuanya di dalam satu transaksi basis data.

```bash
curl -X POST http://localhost:5296/api/v1/transactions \
  -H "X-Api-Key: mps_xxxxxxxxxxxxxxxx" \
  -H "Content-Type: application/json" \
  -d '{
    "lines": [
      { "productId": 3, "quantity": 2 },
      { "productId": 4, "quantity": 1, "discountAmount": 500 }
    ],
    "paymentProvider": "Cash",
    "customerId": 1,
    "orderDiscountPercent": 5,
    "paidAmount": 100000,
    "cashierName": "Integrasi Toko Online"
  }'
```

Catatan:

- `unitPrice` boleh diisi untuk menimpa harga jual, mis. untuk harga grosir. Bila tidak diisi, dipakai harga yang berlaku saat ini.
- Untuk penyedia non-tunai, respons berisi `paymentUrl` yang harus dibuka pelanggan, dan transaksinya berstatus `Pending` sampai pembayaran dikonfirmasi.
- Stok yang tidak mencukupi menolak **seluruh** transaksi dengan `400`, bukan membuang barisnya diam-diam.

Respons:

```json
{
  "success": true,
  "transaction": {
    "id": 101,
    "invoiceNumber": "INV-20260827-0007",
    "status": "Paid",
    "subTotal": 50000,
    "taxableAmount": 50000,
    "taxAmount": 5500,
    "totalAmount": 55500,
    "taxRate": 11,
    "lines": [ ... ]
  },
  "paymentUrl": null,
  "error": null
}
```

## Bentuk daftar berhalaman

Semua endpoint daftar memakai amplop yang sama:

```json
{
  "page": 1,
  "pageSize": 50,
  "total": 12,
  "totalPages": 1,
  "items": [ ... ]
}
```

`pageSize` dibatasi maksimal 200.

## Konfigurasi

```json
"Api": {
  "Enabled": true,
  "SwaggerEnabled": true,
  "RoutePrefix": "/api/v1"
}
```

Setel `Enabled` menjadi `false` untuk mematikan REST API sepenuhnya. Di produksi, pertimbangkan `SwaggerEnabled: false` agar daftar endpoint tidak dapat dijelajahi publik — API-nya sendiri tetap berjalan.

## Catatan keamanan

- Sajikan API lewat HTTPS. Kunci dikirim sebagai header teks biasa dan akan terbaca di jaringan yang tidak terenkripsi.
- Berikan kunci baca saja bila integrasinya memang hanya membaca.
- Beri tanggal kedaluwarsa pada kunci yang diberikan ke pihak ketiga.
- Kunci yang bocor cukup dinonaktifkan atau dihapus dari halaman Pengaturan; akses langsung terputus pada permintaan berikutnya.
- Endpoint webhook pembayaran (`/api/payments/{provider}/callback`) berada di luar grup ini dan **tidak** memakai kunci API — ia diamankan dengan cara lain, lihat [pembayaran.md](pembayaran.md).
