# 🏗️ Arsitektur Sistem — Lapak

Lapak adalah **satu proyek .NET 10 Blazor Server**. Seluruh UI dirender di server
dan dikirim ke browser lewat SignalR; tidak ada proyek API terpisah dan tidak ada
build front-end.

---

## Lapisan

```
┌──────────────────────────────────────────────────────┐
│  Presentasi                                          │
│  Komponen Blazor (Pages, Layout, Shared)             │
│  render mode: InteractiveServer untuk seluruh app    │
├──────────────────────────────────────────────────────┤
│  Layanan lintas-potong                               │
│  SkChatService · VectorRagService · PaymentService    │
│  ShippingService · StorageService                    │
│  RecommendationService · CustomerScoringService      │
├──────────────────────────────────────────────────────┤
│  Domain                                              │
│  15 entitas di atas EntityBase · POCO konfigurasi    │
├──────────────────────────────────────────────────────┤
│  Infrastruktur                                       │
│  EF Core · ASP.NET Identity · SignalR Hubs           │
│  Controller MVC untuk cookie, webhook, unduhan       │
└──────────────────────────────────────────────────────┘
```

---

## Keputusan yang perlu diketahui sebelum menyentuh kode

### Halaman berbicara langsung ke database

Sebagian besar halaman meng-`@inject LapakDbContext` dan menulis query EF Core di
dalam komponen. **Tidak ada lapisan repository**, dan itu disengaja — `Services/`
hanya berisi hal lintas-potong (AI, pembayaran, pengiriman, penyimpanan,
rekomendasi, skoring). Ikuti pola yang sudah ada; jangan menambah abstraksi untuk
satu halaman.

Konsekuensi Blazor Server: `LapakDbContext` yang di-inject hidup selama sirkuit
pengguna. **Jangan pernah menjalankan dua query ber-`await` secara bersamaan di
instance yang sama.** `MainLayout` karenanya membuat scope-nya sendiri lewat
`IServiceScopeFactory` — badge keranjang di-refresh saat navigasi dan tidak boleh
bertabrakan dengan query halaman.

### Autentikasi lewat controller, bukan form Blazor

Login dan register memakai `<form method="post">` biasa yang menembak
`AccountController`. Alasannya teknis: **sirkuit Blazor yang interaktif tidak bisa
menulis cookie** — respons HTTP-nya sudah dimulai. Hal yang sama berlaku untuk
logout (`/account/logout`) dan penyegaran klaim (`/account/refresh`).

Kesalahan dikembalikan sebagai query string `?error=` dan dibaca dengan
`[SupplyParameterFromQuery]`.

### Peran berasal dari kolom, bukan tabel AspNetRoles

`User.UserType` bernilai `Buyer`, `Seller`, atau `Admin`.
`LapakClaimsPrincipalFactory` memproyeksikannya menjadi klaim peran standar saat
sign-in, sehingga halaman bisa dijaga dengan atribut:

```razor
@attribute [Authorize(Policy = "AdminOnly")]
@attribute [Authorize(Policy = "SellerOnly")]
```

`Routes.razor` memakai `AuthorizeRouteView` — tanpa itu atribut `[Authorize]` pada
komponen tidak ditegakkan sama sekali.

Karena klaim dibekukan di dalam cookie, akun yang berubah peran di tengah sesi
(pembeli yang baru membuka toko) harus melewati `/account/refresh` supaya
cookie-nya ditulis ulang.

### Skema dibuat dengan `EnsureCreated()`

Tidak ada folder `Migrations/`. Perubahan pada entitas atau `OnModelCreating`
**tidak** akan terlihat di database yang sudah ada — hapus `lapak.db*` (tiga berkas)
lalu jalankan ulang. Data contoh hanya diisi ketika tabel `Categories` kosong, dan
memakai `Random(42)` sehingga hasilnya selalu sama.

### Aset statis

`app.MapStaticAssets()` melayani aset ber-fingerprint yang dirujuk `@Assets[…]` dan
`<ImportMap>`, termasuk `blazor.web.js`. `app.UseStaticFiles()` tetap dipertahankan
untuk berkas yang dibuat saat runtime (unggahan di `wwwroot/uploads`), yang tidak
ada di manifes build.

---

## AI: Semantic Kernel dengan fallback

`Services/SemanticKernel/SkChatService.cs`

- Dua chatbot dipilih lewat kunci string: `"TonyKurus"` dan `"SitiBohay"`. Nama,
  system prompt, temperature, dan max token semuanya ada di `appsettings.json` pada
  `AI:ChatBots` — **mengubah prompt adalah perubahan konfigurasi, bukan kode.**
- `GetKernel(provider)` membangun `Kernel` baru tiap panggilan, selalu lewat
  `AddOpenAIChatCompletion` yang diarahkan ke `BaseUrl` provider. Artinya semua
  provider dijalankan melalui permukaan yang kompatibel-OpenAI.
- Urutan fallback: `OpenAI → Gemini → Anthropic → Ollama`, dimulai dari
  `AI:DefaultProvider`, dikendalikan `AI:FallbackEnabled`.
- Tool adalah kelas plugin di berkas yang sama, ditandai `[KernelFunction]` dan
  `[Description]` (deskripsinya berbahasa Indonesia — model yang membacanya).

**Menambah tool:** tambahkan satu method `[KernelFunction]` di salah satu kelas
plugin. Pendaftarannya lewat `AddFromType<T>`, tidak ada wiring lain.

| Plugin | Tool |
|---|---|
| `ProductSearchTools` | `search_products`, `get_product_detail`, `get_promos` |
| `StoreSearchTools` | `search_stores` |
| `OrderTools` | `check_order_status` |
| `KnowledgeBaseTools` | `search_knowledge_base` |
| `GeneralTools` | `get_current_time`, `calculate`, `search_internet` |

### Alur chat

```
Input pengguna
   → komponen chat (TonyKurusChat / SitiBohayChat)
   → [Siti Bohay] VectorRagService.SearchAsync — kutipan disisipkan ke prompt
   → SkChatService.ChatStreamAsync
      → provider utama; gagal → provider berikutnya
      → ToolCallBehavior.AutoInvokeKernelFunctions memanggil tool sesuai kebutuhan
   → token dialirkan kembali ke komponen
```

Streaming membuffer seluruh respons satu provider sebelum menyerahkannya, supaya
fallback masih sempat bekerja bila provider gagal di tengah jalan.

---

## RAG

`Services/Rag/VectorRagService.cs` adalah indeks **TF-IDF in-memory** atas berkas
di folder `Documents/`. `VectorIndexingBackgroundService` mengindeks ulang sesuai
`VectorDatabase:ReindexIntervalMinutes`.

Indeks dibangun di samping lalu ditukar sebagai satu snapshot yang tak berubah.
Pembacaan tidak pernah menunggu I/O berkas, dan **tidak ada kunci yang ditahan
melintasi `await`** — pola itu membuat kunci thread-affine macet permanen ketika
kelanjutan `await` berjalan di thread lain.

Provider `Sqlite`, `PostgreSql`, `Qdrant`, dan `Filesystem` baru ada di konfigurasi;
yang terimplementasi adalah `InMemory`.

---

## Pembayaran

Tiga gateway di belakang satu antarmuka. Rincian ada di
[payments.md](payments.md).

```
Checkout
   → PaymentService (router)
      → MidtransPaymentProvider  │
      → XenditPaymentProvider    ├─ IPaymentProvider
      → StripePaymentProvider    │
   → PaymentService.ApplyState — satu-satunya tempat status pesanan berubah

Webhook gateway
   → PaymentController (satu handler, tiga rute)
   → provider memverifikasi signature/token
   → PaymentService.ApplyState
```

---

## Pengiriman

`ShippingService` memanggil RajaOngkir untuk ongkos kirim tujuh kurir. Bila API key
kosong, ongkir dan pelacakan disimulasikan supaya alur checkout tetap bisa dicoba.

---

## Penyimpanan

`IStorageService` **tidak** didaftarkan di DI. Inject `StorageServiceFactory` lalu
panggil `GetStorageService()`, yang memilih implementasi dari `Storage:Provider`.

---

## SignalR

Tiga hub di `Hubs/ChatHub.cs`:

| Hub | Rute |
|---|---|
| `ChatHub` | `/hubs/chat` |
| `NotificationHub` | `/hubs/notifications` |
| `DashboardHub` | `/hubs/dashboard` |

Halaman chat memanggil `ISkChatService` langsung dari komponen; hub tersedia untuk
skenario multi-klien.

---

## Konfigurasi

Setiap seksi terikat ke POCO di `Models/Configurations/AppConfigs.cs` dan
didaftarkan dengan `builder.Services.Configure<T>(…)`. **Konsumsi konfigurasi lewat
`IOptions<T>`, jangan membaca `IConfiguration` langsung di dalam service.**

Menambah seksi konfigurasi berarti tiga hal: POCO di `AppConfigs.cs`, satu baris
`Configure<T>` di `Program.cs`, dan blok di `appsettings.json`.

---

## Entitas

Semua model mewarisi `EntityBase` (`Guid Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`).
Konfigurasi relasi, indeks unik (`Slug` pada Product/Store/Category), dan indeks
komposit ada di `LapakDbContext.OnModelCreating` — tidak ada kelas
`IEntityTypeConfiguration` meski ada folder bernama `Models/Configurations`
(folder itu berisi POCO konfigurasi aplikasi).

> `IsDeleted` ada di setiap entitas tetapi belum dipakai sebagai filter global.
> Penghapusan saat ini bersifat nyata atau lewat flag `IsActive`.
