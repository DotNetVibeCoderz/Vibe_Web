# REST API

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/api.md)

---

## Autentikasi

**Seluruh endpoint memerlukan sesi login.** API memakai cookie autentikasi yang sama
dengan aplikasi web, sehingga klien harus masuk terlebih dahulu lalu menyertakan
cookie tersebut pada setiap permintaan.

Permintaan tanpa sesi dijawab `401 Unauthorized`.

```bash
# 1. Masuk dan simpan cookie
curl -c cookies.txt -X POST http://localhost:5175/account/login \
  -d "UserName=admin&Password=admin123"

# 2. Panggil endpoint dengan cookie tersebut
curl -b cookies.txt http://localhost:5175/api/students
```

> **Catatan riwayat.** Sebelum siklus pengembangan ini, seluruh endpoint `api/*`
> tidak memiliki atribut `[Authorize]`. `GET /api/students` mengembalikan data pribadi
> anak di bawah umur — nama, tanggal lahir, nama orang tua, nomor telepon — kepada
> siapa pun tanpa login. Lubang ini sudah ditutup.

---

## Hak Akses

| Operasi | Peran |
| --- | --- |
| Baca (`GET`) | `admin`, `guru` |
| Tulis (`POST`, `PUT`, `DELETE`) | `admin` |

---

## Endpoint

### Siswa

| Metode | Rute | Keterangan | Peran |
| --- | --- | --- | --- |
| `GET` | `/api/students` | Daftar seluruh siswa | admin, guru |
| `GET` | `/api/students/{id}` | Detail satu siswa | admin, guru |
| `POST` | `/api/students` | Tambah siswa | admin |
| `PUT` | `/api/students/{id}` | Ubah data siswa | admin |
| `DELETE` | `/api/students/{id}` | Hapus siswa | admin |

### Guru

| Metode | Rute | Keterangan | Peran |
| --- | --- | --- | --- |
| `GET` | `/api/teachers` | Daftar seluruh guru | admin, guru |
| `GET` | `/api/teachers/{id}` | Detail satu guru | admin, guru |
| `POST` | `/api/teachers` | Tambah guru | admin |
| `PUT` | `/api/teachers/{id}` | Ubah data guru | admin |
| `DELETE` | `/api/teachers/{id}` | Hapus guru | admin |

---

## Bentuk Data

### Student

```json
{
  "id": 1,
  "fullName": "Siswa 01",
  "className": "9B",
  "dateOfBirth": "2013-09-25T00:00:00",
  "gender": "Laki-laki",
  "parentName": "Orang Tua 01",
  "phone": "0812-000001",
  "status": "Active"
}
```

### Teacher

```json
{
  "id": 1,
  "fullName": "Guru 01",
  "subject": "Matematika",
  "email": "guru01@smsnet.sch.id",
  "phone": "0813-77001",
  "status": "Active"
}
```

`status` bernilai `Active` atau `Inactive`.

---

## Kode Status

| Kode | Arti |
| --- | --- |
| `200` | Berhasil |
| `201` | Data dibuat (disertai header `Location`) |
| `204` | Berhasil, tanpa isi (setelah `PUT` atau `DELETE`) |
| `400` | Permintaan tidak valid — termasuk `id` pada rute yang berbeda dengan `id` pada isi |
| `401` | Belum masuk |
| `403` | Sudah masuk, tetapi peran tidak berwenang |
| `404` | Data tidak ditemukan |

---

## Swagger

Swagger UI tersedia di `/swagger`, **hanya pada environment Development**. Pada
environment lain endpoint tersebut tidak dipasang sama sekali.

Halaman **Keamanan → Integrasi REST API** di dalam aplikasi menampilkan daftar endpoint
yang sama beserta peran yang diperlukan, dan menyediakan tautan ke Swagger bila sedang
berjalan di Development.

---

## Contoh Lengkap

```bash
BASE=http://localhost:5175

# Masuk
curl -c cookies.txt -X POST $BASE/account/login \
  -d "UserName=admin&Password=admin123"

# Daftar siswa
curl -b cookies.txt $BASE/api/students

# Detail satu siswa
curl -b cookies.txt $BASE/api/students/1

# Tambah siswa
curl -b cookies.txt -X POST $BASE/api/students \
  -H "Content-Type: application/json" \
  -d '{
        "fullName": "Budi Santoso",
        "className": "8A",
        "dateOfBirth": "2012-04-17T00:00:00",
        "gender": "Laki-laki",
        "parentName": "Santoso",
        "phone": "0812-3456789",
        "status": "Active"
      }'

# Ubah data
curl -b cookies.txt -X PUT $BASE/api/students/41 \
  -H "Content-Type: application/json" \
  -d '{ "id": 41, "fullName": "Budi Santoso", "className": "8B", "status": "Active" }'

# Hapus
curl -b cookies.txt -X DELETE $BASE/api/students/41
```

---

## Keterbatasan

Disebutkan terus terang:

- **Belum ada autentikasi berbasis token.** API memakai cookie sesi, sehingga cocok
  untuk integrasi server-ke-server dalam jaringan yang sama, tetapi kurang nyaman untuk
  klien pihak ketiga. API key atau OAuth adalah pekerjaan berikutnya.
- **Belum ada paginasi.** `GET /api/students` mengembalikan seluruh baris. Untuk sekolah
  dengan ribuan siswa ini perlu ditambahkan.
- **Belum ada pembatasan laju (rate limiting).**
- **Cakupan terbatas.** Baru siswa dan guru yang tersedia lewat API. Entitas lain hanya
  dapat diakses lewat antarmuka.
- **Belum ada versioning.** Perubahan bentuk data akan langsung memengaruhi klien.
