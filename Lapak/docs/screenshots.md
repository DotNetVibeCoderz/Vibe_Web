# 📸 Galeri Tangkapan Layar

Semua gambar di halaman ini diambil otomatis dari aplikasi yang berjalan dengan
data contoh bawaan — bukan mockup. Lihat [Cara memperbarui](#cara-memperbarui) di
bagian bawah.

---

## Etalase

### Beranda

Hero memakai pola anyaman — tikar yang menjadi asal nama Lapak — dengan angka
katalog yang dihitung langsung dari database.

![Beranda](screenshots/01-beranda.png)

### Beranda, tema gelap

Tema mengikuti preferensi sistem, bisa diganti lewat tombol bulan/matahari, dan
disimpan di browser.

![Beranda tema gelap](screenshots/02-beranda-gelap.png)

### Daftar produk

Setiap kartu menganyam petak warnanya sendiri berdasarkan kategori, jadi katalog
tanpa foto tetap terbaca sebagai lapak yang beragam. Harga duduk di label bertakik
seperti kartu harga yang dijepit di barang dagangan.

![Daftar produk](screenshots/03-produk.png)

### Detail produk

![Detail produk](screenshots/04-detail-produk.png)

### Daftar toko

![Daftar toko](screenshots/05-toko.png)

### Promo dan voucher

Voucher digambar sebagai kupon berperforasi, lengkap dengan sisa kuota.

![Promo dan voucher](screenshots/06-promo.png)

---

## Asisten AI

### Tony Kurus — asisten belanja

Delapan tool Semantic Kernel tersambung ke database, jadi jawabannya berasal dari
katalog yang sedang tayang.

![Tony Kurus](screenshots/07-tony-kurus.png)

### Siti Bohay — bantuan pelanggan

Jawaban diambil dari dokumen kebijakan lewat pencarian TF-IDF; tiap balasan bisa
membuka kutipan sumbernya.

![Siti Bohay](screenshots/08-siti-bohay.png)

---

## Belanja dan pembayaran

### Keranjang

![Keranjang](screenshots/12-keranjang.png)

### Checkout — alamat

![Checkout alamat](screenshots/13-checkout-alamat.png)

### Checkout — pengiriman

Tujuh kurir dengan tiga level layanan; ongkir disimulasikan bila RajaOngkir belum
dikonfigurasi.

![Checkout pengiriman](screenshots/14-checkout-pengiriman.png)

### Checkout — pembayaran

Midtrans, Xendit, dan Stripe muncul sebagai pilihan. Gateway yang kredensialnya
belum diisi tampil nonaktif, bukan disembunyikan, supaya jelas apa yang kurang.

![Checkout pembayaran](screenshots/15-checkout-pembayaran.png)

### Pesanan saya

Pesanan yang belum lunas bisa melanjutkan pembayaran dari sini.

![Pesanan](screenshots/16-pesanan.png)

---

## Penjual

### Kelola toko

![Kelola toko](screenshots/17-kelola-toko.png)

### Kelola produk

![Kelola produk](screenshots/18-kelola-produk.png)

### Tambah produk

![Tambah produk](screenshots/19-tambah-produk.png)

---

## Admin dan laporan

### Dashboard

Grafik memakai jumlah pesanan harian sebenarnya dalam rentang yang dipilih.

![Dashboard](screenshots/20-dashboard.png)

### Dashboard, tema gelap

![Dashboard gelap](screenshots/21-dashboard-gelap.png)

### Panel admin

![Panel admin](screenshots/22-admin.png)

### Manajemen voucher

![Manajemen voucher](screenshots/23-admin-voucher.png)

---

## Mobile

Sidebar berubah jadi laci, dan navigasi pindah ke bilah bawah.

| Beranda | Produk |
|---|---|
| ![Beranda mobile](screenshots/10-mobile-beranda.png) | ![Produk mobile](screenshots/11-mobile-produk.png) |

---

## Autentikasi

![Halaman masuk](screenshots/09-masuk.png)

---

## Cara memperbarui

Tangkapan layar dibuat dengan Playwright terhadap aplikasi yang sedang berjalan.

```bash
# 1. jalankan aplikasi dengan data contoh yang bersih
rm -f lapak.db*
dotnet run

# 2. di terminal lain, jalankan skrip penangkap layar
npm install playwright
npx playwright install chromium
node scripts/shoot.js http://localhost:5247 docs/screenshots
```

Skrip masuk memakai akun demo (password `Lapak2025!`), mengisi keranjang, dan
melangkah sampai layar pembayaran supaya setiap halaman berisi data sungguhan.

Kalau menambah tangkapan layar baru, pakai penomoran yang sudah ada
(`NN-nama-halaman.png`) supaya urutannya tetap terbaca.
