# Asisten "Pak Dedi"

[← Kembali ke indeks dokumentasi](../README.md) · [English version](../en/assistant.md)

---

![Percakapan dengan Pak Dedi](../img/assistant-thread.png)

Pak Dedi adalah asisten informasi sekolah yang menjawab pertanyaan dengan **membaca
data sekolah yang sebenarnya**, bukan mengarang. Ia dibangun di atas
**Semantic Kernel 1.78** dan dapat berjalan pada lima penyedia model.

---

## Kemampuan

| Kemampuan | Keterangan |
| --- | --- |
| Multi-sesi | Buat, pilih, reset, dan hapus percakapan. Judul dibuat otomatis dari pertanyaan pertama. |
| Contoh pertanyaan | Layar sambutan menampilkan 19 contoh dalam 6 kelompok — sekali klik langsung terkirim. |
| Lampiran | Gambar (PNG, JPEG, GIF) dikirim sebagai konten visual; dokumen diunggah dan tautannya disertakan pada pesan. |
| Pemanggilan fungsi | 24 fungsi dalam 4 plugin, dijalankan otomatis oleh model. |
| Markdown | Tabel, blok kode dengan tombol salin, media (gambar/video/audio), daftar, kutipan. |
| Menghormati peran | Fungsi data memeriksa peran pemanggil, bukan sekadar mempercayai prompt. |
| Riwayat | Percakapan tersimpan di basis data dan bertahan setelah halaman dimuat ulang. |

---

## Memilih Penyedia Model

Lima penyedia didukung. Pilih lewat `Assistant:Provider`.

| Penyedia | Nilai | Kunci API | Catatan |
| --- | --- | --- | --- |
| OpenAI | `OpenAI` | Wajib | Bawaan. Mendukung endpoint yang kompatibel lewat `Endpoint` — termasuk DeepSeek. |
| Azure OpenAI | `AzureOpenAI` | Wajib | Dialamatkan lewat nama *deployment* — lihat di bawah. |
| Anthropic | `Anthropic` | Wajib | Konektor ditulis khusus — lihat di bawah. |
| Google Gemini | `Google` | Wajib | Konektor Semantic Kernel masih berstatus alpha. |
| Ollama | `Ollama` | Tidak perlu | Jalankan model secara lokal. Pilihan untuk lingkungan tanpa internet. |

### Contoh konfigurasi

```bash
# OpenAI
export Assistant__Provider="OpenAI"
export Assistant__OpenAI__ApiKey="sk-..."
export Assistant__OpenAI__Model="gpt-4o-mini"

# OpenAI-compatible (contoh: DeepSeek) — cukup ubah Endpoint
export Assistant__Provider="OpenAI"
export Assistant__OpenAI__ApiKey="sk-..."
export Assistant__OpenAI__Endpoint="https://api.deepseek.com"
export Assistant__OpenAI__Model="deepseek-v4-flash"

# Azure OpenAI
export Assistant__Provider="AzureOpenAI"
export Assistant__AzureOpenAI__ApiKey="..."
export Assistant__AzureOpenAI__Endpoint="https://namaresource.openai.azure.com/"
export Assistant__AzureOpenAI__Deployment="gpt-5-mini"   # nama deployment, bukan nama model
export Assistant__AzureOpenAI__ModelId="gpt-5-mini"

# Anthropic
export Assistant__Provider="Anthropic"
export Assistant__Anthropic__ApiKey="sk-ant-..."
export Assistant__Anthropic__Model="claude-opus-5"
export Assistant__Anthropic__Effort="medium"

# Google Gemini
export Assistant__Provider="Google"
export Assistant__Google__ApiKey="..."
export Assistant__Google__Model="gemini-2.0-flash"

# Ollama — tanpa kunci
export Assistant__Provider="Ollama"
export Assistant__Ollama__Endpoint="http://localhost:11434"
export Assistant__Ollama__Model="llama3.1"
```

### Azure OpenAI dialamatkan berbeda

Azure **bukan** sekadar OpenAI dengan `Endpoint` lain. URL-nya memuat nama *deployment*
dan wajib menyertakan `api-version`, sehingga konektor OpenAI biasa tidak dapat
menjangkaunya. Karena itu Azure memakai bagian pengaturannya sendiri.

Nama *deployment* adalah nama yang Anda berikan di Azure AI Foundry, dan **tidak selalu
sama** dengan nama modelnya. `ModelId` boleh dikosongkan; bila kosong, nama deployment
yang dipakai sebagai label model di antarmuka.

### Model penalaran menolak parameter sampling

Keluarga **gpt-5** dan **o1/o3/o4** — baik lewat OpenAI maupun Azure — menolak
`max_tokens` (harus `max_completion_tokens`) serta hanya menerima nilai bawaan untuk
`temperature` dan `top_p`. Semantic Kernel mengirim nama parameter yang lama, sehingga
mengirimkannya berarti **HTTP 400**.

Aplikasi mengenali model-model itu dari namanya dan **tidak mengirim ketiga parameter
tersebut sama sekali**, mengikuti nilai bawaan layanan. Artinya `Assistant:Temperature`,
`TopP`, dan `MaxTokens` diabaikan saat memakai model penalaran — perilaku ini disengaja,
bukan pengaturan yang gagal terbaca.

### Mengapa konektor Anthropic ditulis sendiri

Microsoft **tidak** merilis `Microsoft.SemanticKernel.Connectors.Anthropic`. Paket
komunitas yang tersedia (`Lost.SemanticKernel.Connectors.Anthropic`) masih pada versi
1.25-alpha sementara Semantic Kernel yang dipakai aplikasi ini adalah 1.78 — selisih
53 versi minor.

Karena itu `Services/Assistant/AnthropicChatCompletionService.cs` mengimplementasikan
`IChatCompletionService` sendiri di atas **SDK resmi Anthropic**, lengkap dengan loop
pemanggilan alat. Ini lebih aman daripada memaksa dependensi yang tidak sepadan.

> **Temperature tidak dikirim ke Anthropic.** Model Claude terkini menolak parameter
> `temperature` dan `top_p` dengan galat 400. Kedalaman penalaran diatur lewat
> `Assistant:Anthropic:Effort` (`low`, `medium`, `high`, `xhigh`, `max`) sebagai gantinya.
> Nilai `Assistant:Temperature` tetap berlaku untuk OpenAI, Google, dan Ollama.

> **WebP tidak diterima sebagai lampiran gambar.** Enum tipe media pada SDK Anthropic
> hanya mencakup PNG, JPEG, dan GIF, sehingga WebP ditolak pada saat diunggah — bukan
> saat permintaan sudah terlanjur dikirim ke model.

---

## Pengaturan

Seluruhnya di bagian `Assistant` pada `appsettings.json`.

| Kunci | Bawaan | Keterangan |
| --- | --- | --- |
| `Name` | `Pak Dedi` | Nama yang tampil di antarmuka |
| `Tagline` | `Asisten informasi sekolah` | Keterangan di bawah nama |
| `Provider` | `OpenAI` | Penyedia model aktif |
| `SystemPromptLines` | (persona bawaan) | Persona, satu baris per elemen array |
| `Temperature` | `0.4` | Berlaku untuk OpenAI, Google, Ollama — diabaikan pada model penalaran |
| `TopP` | `0.95` | Sama seperti di atas |
| `MaxTokens` | `2048` | Batas panjang jawaban — diabaikan pada model penalaran |
| `AzureOpenAI.Deployment` | (kosong) | Nama deployment Azure; wajib bila `Provider = AzureOpenAI` |
| `HistoryWindow` | `20` | Jumlah giliran sebelumnya yang dikirim ulang |
| `EnableFunctionCalling` | `true` | Izinkan model memanggil fungsi sendiri |
| `MaxToolIterations` | `6` | Batas putaran pemanggilan alat per giliran |
| `Uploads.MaxFileSizeBytes` | `10485760` | 10 MB |
| `Uploads.MaxFilesPerMessage` | `5` | Batas lampiran per pesan |
| `Tavily.ApiKey` | (kosong) | Kosongkan untuk menonaktifkan pencarian internet |

### Mengubah persona

Persona ditulis sebagai array baris karena JSON tidak mendukung string multi-baris,
dan satu blok panjang dengan escape akan mustahil disunting tangan:

```json
"SystemPromptLines": [
  "Kamu adalah \"Pak Dedi\", asisten informasi resmi sekolah pada aplikasi SMSNet.",
  "",
  "Kepribadian:",
  "- Ramah, sabar, dan sopan — seperti staf tata usaha senior."
]
```

Mengosongkan array akan mengembalikan persona bawaan aplikasi, bukan prompt kosong.

---

## Contoh Pertanyaan

![Contoh pertanyaan](../img/assistant-prompts.png)

Percakapan yang masih kosong menampilkan **19 contoh pertanyaan** dalam enam kelompok.
Sekali klik langsung mengirimkannya — tidak perlu mengetik ulang.

| Kelompok | Contoh |
| --- | --- |
| **Data sekolah** | "Ada berapa siswa aktif saat ini?" · "Siapa saja guru yang mengampu Matematika?" |
| **Jadwal & akademik** | "Tampilkan jadwal pelajaran kelas 8A hari Senin" · "Berapa rata-rata nilai Matematika?" |
| **Keuangan & operasional** | "Berapa total tunggakan SPP saat ini?" · "Aset apa saja yang kondisinya rusak?" |
| **Hitung & waktu** | "Hitung 15% dari 2.500.000" · "Berapa hari lagi menuju 17 Agustus 2026?" |
| **Cari di internet** | "Cari kabar terbaru tentang Kurikulum Merdeka" |
| **Analisis gabungan** | "Siswa mana yang nilainya di bawah KKM sekaligus menunggak pembayaran?" |

Kelompok terakhir sengaja memaksa **lebih dari satu fungsi** dipanggil dalam satu
giliran, karena kemampuan itulah yang paling sulit ditebak pengguna baru hanya dari
kolom isian kosong.

Contoh-contoh ini didefinisikan pada `Components/Pages/Assistant/Chat.razor`
(daftar `PromptGroup`), sehingga dapat disesuaikan dengan kebutuhan sekolah.

---

## Fungsi yang Tersedia (24)

### Plugin Waktu

Tanpa plugin ini model menjawab "hari ini" berdasarkan tanggal batas pelatihannya,
yang selalu salah.

| Fungsi | Kegunaan |
| --- | --- |
| `tanggal_hari_ini` | Tanggal dan jam sekarang dalam WIB |
| `hitung_selisih_hari` | Selisih hari antara dua tanggal |
| `tambah_hari` | Menambah/mengurangi hari dari sebuah tanggal |
| `info_tahun_ajaran` | Tahun ajaran dan semester berjalan (kalender Indonesia) |

### Plugin Matematika

| Fungsi | Kegunaan |
| --- | --- |
| `hitung` | Evaluasi ekspresi: `+ - * / % ^`, kurung, `sqrt`, `abs`, `round`, `floor`, `ceil`, `min`, `max`, `pow`, `log`, `ln`, `exp`, `sin`, `cos`, `tan`, konstanta `pi` dan `e` |
| `persentase` | Persentase bagian terhadap total |
| `statistik` | Jumlah, total, rata-rata, median, minimum, maksimum |

Evaluator ditulis tangan sebagai *recursive-descent parser* dengan tata bahasa
tertutup, bukan memakai `DataTable.Compute`, agar tidak ada string ekspresi yang
dapat mencapai interpreter serbaguna.

### Plugin Web

| Fungsi | Kegunaan |
| --- | --- |
| `cari_internet` | Pencarian melalui Tavily |
| `baca_halaman` | Membuka URL dan mengembalikan teksnya |
| `baca_file_dari_url` | Mengunduh berkas teks (txt, md, csv, json, xml) |

> **Penjagaan SSRF.** Dua fungsi terakhir menembak URL yang dipilih model, sehingga
> alamat loopback, privat (10.x, 172.16–31.x, 192.168.x), link-local, CGNAT, dan
> metadata awan (169.254.x) **ditolak sebelum permintaan dikirim**. Tanpa penjagaan
> ini sebuah *prompt injection* berisi "baca http://169.254.169.254/..." akan
> menjadikan asisten sebagai proksi ke jaringan internal host.

### Plugin SekolahData

| Fungsi | Pembatasan peran |
| --- | --- |
| `ringkasan_sekolah` | Semua |
| `cari_siswa` | admin, guru |
| `cari_guru` | Semua (kontak hanya untuk staf) |
| `daftar_kelas` | Semua |
| `daftar_mata_pelajaran` | Semua |
| `jadwal_pelajaran` | Semua |
| `rekap_absensi` | admin, guru |
| `nilai_siswa` | Bukan staf wajib menyebut nama siswa |
| `daftar_tugas_ujian` | Semua |
| `materi_elearning` | Semua |
| `rekap_pembayaran` | admin, orangtua |
| `inventaris_sekolah` | admin, guru |
| `daftar_kegiatan` | Semua |
| `notifikasi_terbaru` | Semua |

Setiap fungsi membuka **scope DI sendiri** untuk memperoleh `DbContext` baru, karena
beberapa pemanggilan alat dapat berjalan bersamaan dan satu context tidak dapat
melayani kueri yang tumpang tindih.

---

## Keamanan

| Aspek | Penanganan |
| --- | --- |
| **Peran** | Diperiksa di dalam badan fungsi, bukan di prompt |
| **SSRF** | Alamat internal ditolak sebelum permintaan |
| **HTML tak tepercaya** | Keluaran model disanitasi: tanpa `script`, `iframe`, atribut event, atau `style` inline |
| **Tautan keluar** | Otomatis `target="_blank"` dengan `rel="noopener noreferrer nofollow"` |
| **Unggahan** | Tipe berkas dari daftar putih; nama berkas dibangkitkan, bukan memakai nama dari klien |
| **Isolasi sesi** | Setiap kueri disaring berdasarkan `UserId`, sehingga percakapan tidak bocor antarpengguna |
| **Rahasia** | Kunci API tidak pernah masuk ke prompt maupun keluaran |

Alur render selalu **render → sanitasi → pengayaan**, karena keluaran model adalah
masukan tak tepercaya: ia bisa saja mengutip halaman web yang baru dibacanya, dan
halaman itu bisa memuat muatan berbahaya.

---

## Cara Kerja Satu Giliran

```
Pengguna mengirim pertanyaan
  → AssistantService menyusun riwayat + konteks pengguna (nama, peran, tanggal)
  → AssistantKernelFactory merakit Kernel untuk peran tersebut
      (kernel dibuat per giliran, karena plugin terikat pada peran pemanggil —
       satu kernel bersama akan membocorkan akses admin ke sesi siswa)
  → Model menerima 24 definisi fungsi
  → Model memutuskan memanggil, misalnya, ringkasan_sekolah
  → Semantic Kernel menjalankan fungsi → basis data → hasil
  → Hasil kembali ke model
  → Model menyusun jawaban dalam Markdown
  → Markdig → HtmlSanitizer → pengayaan media
  → Disimpan sebagai ChatMessage → dirender
```

Nama fungsi yang dipakai ditampilkan di bawah jawaban sebagai "Alat yang dipakai",
sehingga jejaknya dapat ditelusuri.

---

## Pemecahan Masalah

| Gejala | Sebab | Solusi |
| --- | --- | --- |
| "Asisten belum dikonfigurasi" | Kunci API kosong | Isi environment variable sesuai penyedia yang dipilih |
| "Pencarian internet belum aktif" | `Tavily:ApiKey` kosong | Isi kunci Tavily, atau abaikan bila memang tidak diperlukan |
| Galat 400 dari Anthropic | Parameter `temperature` terkirim | Seharusnya tidak terjadi — laporkan bila muncul |
| `'max_tokens' is not supported with this model` | Model penalaran yang belum dikenali dari namanya | Laporkan — keluarga model itu perlu ditambahkan pada pemeriksaan nama di `AssistantKernelFactory` |
| Azure membalas 404 `DeploymentNotFound` | `Deployment` diisi nama model, bukan nama deployment | Salin nama deployment persis dari Azure AI Foundry |
| `Temperature` tampak tidak berpengaruh | Model aktif adalah model penalaran | Perilaku yang diharapkan — model itu hanya menerima nilai bawaan |
| Lampiran WebP ditolak | Tidak didukung SDK Anthropic | Ubah ke PNG atau JPEG |
| Jawaban menyebut tanggal yang salah | Model tidak memanggil `tanggal_hari_ini` | Pastikan `EnableFunctionCalling` bernilai `true` |
| Fungsi menolak permintaan | Peran pengguna tidak berwenang | Perilaku yang diharapkan — lihat tabel pembatasan di atas |
