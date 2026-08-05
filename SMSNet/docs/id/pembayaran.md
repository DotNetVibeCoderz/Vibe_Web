# Metode Pembayaran

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/payments.md)

---

![Pengaturan metode pembayaran](../img/payment-gateways.png)

SMSNet mendukung lima kanal pembayaran yang dapat dikonfigurasi **dari appsettings
maupun dari antarmuka**, tanpa perlu deploy ulang.

---

## Kanal yang Tersedia

| Kunci | Nama | Jenis | Kredensial |
| --- | --- | --- | --- |
| `manual` | Transfer Manual | Transfer ke rekening sekolah, dikonfirmasi petugas | Tidak perlu — cukup nomor rekening |
| `qris` | QRIS | Pindai kode QR statis | Tidak perlu — cukup kode merchant |
| `midtrans` | Midtrans | Halaman Snap milik penyedia | Server key |
| `xendit` | Xendit | Halaman invoice milik penyedia | Secret key |
| `stripe` | Stripe | Checkout Session | Secret key |

`manual` dan `qris` aktif secara bawaan karena keduanya dapat dipakai sejak hari
pertama tanpa akun merchant apa pun.

---

## Mode Sandbox

Secara bawaan aplikasi berjalan dalam **mode sandbox**:

```json
"Payments": { "SandboxMode": true }
```

Dalam mode ini **tidak ada permintaan yang dikirim ke penyedia mana pun**. Transaksi
dibuat secara lokal dengan nomor referensi sungguhan, sehingga seluruh alur —
pembuatan tagihan, pemilihan kanal, petunjuk pembayaran, konfirmasi, rekonsiliasi
dengan buku keuangan — dapat diuji tanpa akun merchant.

Ini adalah keadaan saat sebuah sekolah pertama kali memasang aplikasi.

Untuk mengaktifkan panggilan sungguhan, matikan sandbox global lalu isi kredensial
kanal yang bersangkutan pada halaman **Metode Pembayaran**.

> **Keterusterangan yang perlu dicatat.** Titik integrasi HTTP untuk Midtrans, Xendit,
> dan Stripe sudah ditulis lengkap dan ditandai `LIVE CALL` di
> `Services/Payments/Gateways.cs`, tetapi **belum pernah diuji terhadap akun sungguhan**
> karena tidak tersedia kredensial pada lingkungan pengembangan. Bentuk permintaannya
> mengikuti dokumentasi masing-masing penyedia; verifikasi terhadap akun sandbox
> penyedia tetap diperlukan sebelum dipakai memproses uang sungguhan.

---

## Konfigurasi lewat appsettings

Nilai di sini adalah **bawaan saat pertama dijalankan**. Pengaturan yang disimpan
lewat antarmuka akan menimpanya.

```json
"Payments": {
  "Currency": "IDR",
  "ReferencePrefix": "SMSNET",
  "ExpiryHours": 24,
  "SandboxMode": true,
  "Gateways": [
    {
      "Key": "manual",
      "DisplayName": "Transfer Manual",
      "Enabled": true,
      "SortOrder": 10,
      "AccountDetail": "BCA 1234567890 a.n. Yayasan SMSNet",
      "Instructions": "Transfer ke rekening sekolah, lalu unggah bukti pembayaran."
    },
    {
      "Key": "midtrans",
      "DisplayName": "Midtrans",
      "Enabled": false,
      "SandboxMode": true,
      "SecretKey": "",
      "FeePercent": 2.0
    }
  ]
}
```

Untuk produksi, isi kredensial lewat environment variable:

```bash
export Payments__Gateways__2__SecretKey="SB-Mid-server-..."
```

---

## Konfigurasi lewat Antarmuka

Buka **Administrasi & Keuangan → Metode Pembayaran** (khusus admin).

Setiap kanal dapat diatur:

| Kolom | Keterangan |
| --- | --- |
| Nama tampilan | Yang dilihat orang tua saat memilih kanal |
| Status | Aktif / nonaktif |
| Urutan tampil | Angka lebih kecil tampil lebih dahulu |
| Mode | Sandbox (simulasi lokal) atau Produksi (memanggil API) |
| Secret / Server key | Kredensial utama penyedia |
| Client / Public key | Kredensial publik bila diperlukan |
| Merchant ID | Pengenal merchant |
| Rekening / kode merchant | Untuk kanal `manual` dan `qris` |
| Biaya tetap | Ditambahkan ke nominal, dalam IDR |
| Biaya persentase | Ditambahkan sebagai persentase nominal |
| Petunjuk | Ditampilkan kepada pembayar |

Pengaturan disimpan ke tabel `PaymentGatewayConfig` dan menimpa nilai appsettings.

---

## Alur Pembayaran

![Halaman E-Payment](../img/epayment.png)

1. Admin menerbitkan tagihan pada **Manajemen Keuangan**, atau tagihan sudah ada
   dari data yang diimpor.
2. Orang tua membuka **E-Payment**, menekan **Bayar** pada tagihan yang belum lunas.
3. Daftar kanal aktif ditampilkan, lengkap dengan biaya tambahan masing-masing.
4. Setelah kanal dipilih dan **Lanjutkan** ditekan:
   - `PaymentService.CreateChargeAsync` membuat nomor referensi
     (`SMSNET-20260805-0001` — berurutan per hari),
   - memanggil gateway terpilih,
   - menyimpan `PaymentTransaction`,
   - mencatat ke audit trail.
5. Petunjuk pembayaran ditampilkan: tautan penyedia, kode QRIS, atau nomor rekening.
6. Untuk kanal `manual` dan `qris`, admin menekan **Tandai lunas** setelah dana masuk.
   Tindakan ini juga **memperbarui `PaymentRecord` yang terkait**, sehingga Manajemen
   Keuangan dan Portal Orang Tua tidak berselisih soal status tagihan.

---

## Status Transaksi

| Status | Arti |
| --- | --- |
| `Pending` | Menunggu pembayaran di halaman penyedia |
| `AwaitingConfirmation` | Menunggu konfirmasi petugas (manual/QRIS) |
| `Paid` | Lunas |
| `Failed` | Gagal dibuat |
| `Expired` | Melewati batas waktu |
| `Cancelled` | Dibatalkan |
| `Refunded` | Dikembalikan |

---

## Menambah Penyedia Baru

Tiga langkah:

**1.** Implementasikan `IPaymentGateway` — turunkan dari `HostedGatewayBase` bila
penyedia memakai halaman checkout:

```csharp
public sealed class DokuGateway : HostedGatewayBase
{
    public override string Key => "doku";
    public override PaymentChannelKind Channel => PaymentChannelKind.Redirect;

    protected override async Task<ChargeResult> CreateLiveChargeAsync(
        ChargeRequest request, PaymentGatewayConfig config, CancellationToken ct)
    {
        // LIVE CALL
    }

    protected override ChargeResult Simulate(ChargeRequest request, PaymentGatewayConfig config) =>
        ChargeResult.Ok(PaymentStatus.Pending, $"doku-sandbox-{…}", …);
}
```

**2.** Daftarkan pada konstruktor `PaymentGatewayRegistry`.

**3.** Tambahkan entri bawaan pada bagian `Payments:Gateways` di appsettings.

Tidak ada halaman yang perlu diubah — daftar kanal dibangun dari registry.

---

## Yang Belum Ada

Dicatat terus terang agar tidak menjadi kejutan:

- **Callback / webhook penyedia belum ditangani.** Untuk kanal `midtrans`, `xendit`,
  dan `stripe`, status transaksi tidak berubah otomatis saat pembayaran selesai di sisi
  penyedia. Konfirmasi masih manual. Endpoint webhook adalah pekerjaan berikutnya.
- **Belum ada verifikasi tanda tangan callback**, karena callback-nya sendiri belum ada.
- **Belum ada alur refund.** Status `Refunded` tersedia pada model tetapi tidak ada
  operasi yang mengubahnya.
- **Kredensial disimpan apa adanya** di basis data. Untuk produksi, pertimbangkan
  ASP.NET Core Data Protection atau brankas rahasia.
