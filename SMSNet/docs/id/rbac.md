# Hak Akses (RBAC)

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/rbac.md)

---

## Empat Peran

Konstanta didefinisikan pada `Models/AppRoles.cs` dan seluruhnya huruf kecil:

| Konstanta | Nilai | Untuk siapa |
| --- | --- | --- |
| `AppRoles.Admin` | `admin` | Tata usaha dan kepala sekolah |
| `AppRoles.Guru` | `guru` | Tenaga pendidik |
| `AppRoles.Siswa` | `siswa` | Peserta didik |
| `AppRoles.OrangTua` | `orangtua` | Wali murid |

Untuk beberapa peran sekaligus, gabungkan sebagai string:

```csharp
@attribute [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Guru)]
```

---

## Matriks Hak Akses

![Matriks RBAC](../img/rbac-matrix.png)

Halaman **Keamanan → Role Access** menampilkan matriks ini secara langsung dan dapat
diekspor ke CSV. Isinya dibangkitkan dari `Services/NavigationRegistry.cs`.

| Modul | Halaman | admin | guru | siswa | orangtua |
| --- | --- | :---: | :---: | :---: | :---: |
| Ringkasan | Dashboard | ✅ | ✅ | ✅ | ✅ |
| Ringkasan | Pak Dedi (Asisten) | ✅ | ✅ | ✅ | ✅ |
| Akademik | Kurikulum & Jadwal | ✅ | ✅ | — | — |
| Akademik | Absensi QR | ✅ | ✅ | — | — |
| Akademik | Absensi Manual | ✅ | ✅ | — | — |
| Akademik | Penilaian & Rapor | ✅ | ✅ | — | — |
| Akademik | E-Learning | ✅ | ✅ | ✅ | — |
| Guru & Staff | Dashboard Guru | ✅ | ✅ | — | — |
| Guru & Staff | Tugas & Ujian | ✅ | ✅ | — | — |
| Guru & Staff | Komunikasi Internal | ✅ | ✅ | — | — |
| Guru & Staff | Evaluasi Kinerja | ✅ | ✅ | — | — |
| Orang Tua & Siswa | Portal | ✅ | — | ✅ | ✅ |
| Orang Tua & Siswa | Notifikasi | ✅ | ✅ | ✅ | ✅ |
| Orang Tua & Siswa | E-Payment | ✅ | — | — | ✅ |
| Orang Tua & Siswa | Dokumen Digital | ✅ | — | ✅ | ✅ |
| Administrasi | Manajemen Keuangan | ✅ | — | — | — |
| Administrasi | Metode Pembayaran | ✅ | — | — | — |
| Administrasi | Inventory | ✅ | — | — | — |
| Administrasi | Payroll | ✅ | — | — | — |
| Administrasi | Laporan Keuangan | ✅ | — | — | — |
| Analitik | Dashboard Analitik | ✅ | — | — | — |
| Analitik | Data Analytics | ✅ | — | — | — |
| Analitik | Custom Reports | ✅ | — | — | — |
| Analitik | Laporan Akademik | ✅ | ✅ | — | — |
| Analitik | Laporan Guru & Staff | ✅ | ✅ | — | — |
| Analitik | Laporan Orang Tua | ✅ | ✅ | — | — |
| Analitik | Laporan Keuangan | ✅ | — | — | — |
| Analitik | Laporan Master Data | ✅ | — | — | — |
| Master Data | Siswa / Guru / Mapel / Kelas | ✅ | ✅ | — | — |
| Master Data | Kartu Ber-QR | ✅ | ✅ | — | — |
| Kegiatan | Event & Ekstrakurikuler | ✅ | ✅ | ✅ | ✅ |
| Keamanan | Role Access | ✅ | — | — | — |
| Keamanan | Audit Trail | ✅ | — | — | — |
| Keamanan | REST API | ✅ | — | — | — |

---

## Empat Lapis Penegakan

Hak akses tidak hanya ditegakkan di satu tempat.

### Lapis 1 — Navigasi

`NavMenu.razor` hanya menampilkan menu yang boleh dibuka oleh peran pengguna.
Ini kenyamanan, **bukan** keamanan: menyembunyikan tautan tidak menghalangi siapa pun
mengetik alamatnya langsung.

### Lapis 2 — Atribut halaman

Setiap halaman membawa atribut `[Authorize]` miliknya sendiri:

```csharp
@page "/admin/payroll"
@attribute [Authorize(Roles = AppRoles.Admin)]
```

Halaman anonim harus menyatakannya secara eksplisit dengan `[AllowAnonymous]`.

`Components/Routes.razor` membedakan dua keadaan: pengunjung yang belum masuk
diarahkan ke halaman login sambil membawa tujuan awalnya, sedangkan pengguna yang
sudah masuk tetapi kurang peran diberi tahu di tempat — memantulkannya ke formulir
login yang sudah ia lewati hanya membingungkan.

### Lapis 3 — Endpoint API

Controller `api/*` membawa atribut peran, dan setiap verb yang mengubah data
dibatasi lebih ketat lagi:

```csharp
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Guru)]   // pada controller
public class StudentsController : ControllerBase
{
    [Authorize(Roles = AppRoles.Admin)]   // pada operasi tulis
    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student) { … }
}
```

> **Catatan riwayat.** Sebelum siklus pengembangan ini, controller `api/*` tidak
> memiliki atribut `[Authorize]` sama sekali. `GET /api/students` mengembalikan nama,
> tanggal lahir, nama orang tua, dan nomor telepon seluruh siswa kepada siapa pun
> tanpa login. Ini adalah temuan paling serius dari audit dan sudah ditutup.

### Lapis 4 — Fungsi asisten

Ini yang paling mudah terlewat. Asisten menjalankan fungsi di sisi server dengan akses
basis data penuh, sehingga **model tidak boleh menjadi penentu** siapa melihat apa.

`SekolahDataPlugin` memeriksa peran pemanggil di dalam badan fungsinya:

```csharp
if (!_user.IsStaff)
{
    return Denied("rekap absensi sekolah", "admin atau guru");
}
```

Prompt sistem adalah panduan, dan model dapat dibujuk keluar dari panduan.
Pemeriksaan peran tidak bisa.

| Fungsi | Pembatasan |
| --- | --- |
| `cari_siswa` | admin, guru |
| `rekap_absensi` | admin, guru |
| `inventaris_sekolah` | admin, guru |
| `rekap_pembayaran` | admin, orangtua |
| `nilai_siswa` | Bukan staf wajib menyebut nama siswa; penelusuran menyeluruh hanya untuk staf |
| `cari_guru` | Semua peran, tetapi email dan telepon hanya tampil untuk staf |
| Sisanya | Semua peran |

---

## Audit Trail

Setiap penambahan, perubahan, dan penghapusan dicatat oleh `Services/AuditService.cs`
ke tabel `AuditTrail`, lengkap dengan pelaku dan waktunya.

```csharp
await Audit.RecordDeleteAsync("siswa", "Siswa 07");
```

Kegagalan pencatatan audit **tidak pernah** menggagalkan operasi yang dicatat —
audit yang error tidak boleh menjatuhkan penyimpanan data.

Riwayatnya dapat dilihat pada **Keamanan → Audit Trail** dan diekspor ke CSV.

---

## Menambah Halaman Baru

Empat langkah, berurutan:

1. Buat halaman dengan atribut `[Authorize]` yang benar.
2. Daftarkan pada `Services/NavigationRegistry.cs` dengan peran yang sama persis.
3. Jalankan aplikasi dan periksa halaman **Role Access** — matriksnya ikut bertambah otomatis.
4. Perbarui tabel di dokumen ini.

Bila langkah 1 dan 2 tidak sepakat, menu akan menampilkan tautan yang justru ditolak
saat diklik. Menempatkan keduanya berdampingan dalam satu daftar adalah cara
menghindarinya.

---

## Membuat Pengguna

Halaman **Daftar** (`/auth/register`) terbuka untuk umum dan memungkinkan memilih peran.

> ⚠️ **Untuk produksi, batasi halaman ini.** Dalam bentuk sekarang siapa pun dapat
> mendaftarkan diri sebagai `admin`. Pilihan yang wajar: ganti `[AllowAnonymous]`
> menjadi `[Authorize(Roles = AppRoles.Admin)]` sehingga hanya administrator yang
> dapat menerbitkan akun.

Akun bawaan `admin` / `admin123` dibuat oleh `DbInitializer` saat pertama dijalankan.
Ganti kata sandinya lewat **Profil Saya** sebelum dipakai sungguhan.
