# 🧺 Lapak

> A modern Indonesian marketplace built on .NET 10 Blazor Server, with two AI
> assistants that read the live catalogue instead of guessing.

*Lapak* is the woven mat a vendor lays out at the pasar. The whole interface is
built from that idea: indigo and turmeric from batik dye, a market-awning stripe
under the header, and a woven *anyaman* pattern standing in for product photography.

![Lapak home page](docs/screenshots/01-beranda.png)

---

## Quick start

```bash
git clone https://github.com/yourusername/lapak.git
cd lapak
dotnet run
```

Open <https://localhost:7205>. SQLite is the default, so there is nothing to set up —
the database is created and seeded on first run with 30 users, 12 stores, 51 products,
and 30 orders.

**Requirements:** .NET 10 SDK.

### Demo accounts

Every seeded account uses the password **`Lapak2025!`**.

| Role | Email | What it opens |
|---|---|---|
| Buyer | `zahra.aulia@lapak.com` | Cart, checkout, orders, wishlist |
| Seller | `budi.santoso@lapak.com` | Store and product management, sales dashboard |
| Admin | `admin.lapak@lapak.com` | Admin panel, store verification, vouchers |

Demo credentials exist only in the seeded sample data. Real accounts are created
through `/account/register`.

---

## What's in it

### Storefront

Product search with category, price, rating, and sort filters. Product and store
pages with reviews, wishlist, and cart. A three-step checkout that calculates
shipping before asking for payment.

![Product catalogue](docs/screenshots/03-produk.png)

### Two AI assistants

**Tony Kurus** is the shopping assistant. He has eight Semantic Kernel tools wired
to the live database — product search with filters, store search, product detail,
active promos, order lookup, time, and arithmetic — so answers come from the
catalogue rather than the model's memory.

**Siti Bohay** handles support. Her answers are retrieved from the policy documents
in `Documents/` through a TF-IDF index, and every reply can show the passages it
was based on. When she cannot resolve something, she hands over to WhatsApp or email.

![Siti Bohay support chat](docs/screenshots/08-siti-bohay.png)

Both accept image uploads, stream their replies, and fall back across providers
(OpenAI → Gemini → Anthropic → Ollama) when one is unavailable.

### Payments — Midtrans, Xendit, and Stripe

Each gateway is an `IPaymentProvider`; the buyer picks one at checkout and
unconfigured gateways appear disabled. Webhook signatures are verified for all
three — SHA-512 for Midtrans, callback token for Xendit, timestamped HMAC-SHA256
for Stripe.

![Checkout payment step](docs/screenshots/15-checkout-pembayaran.png)

See [docs/payments.md](docs/payments.md) for setup and webhook testing.

### Shipping

RajaOngkir integration covering seven couriers (JNE, J&T, SiCepat, Pos Indonesia,
AnterAja, Ninja, Lion) with three service levels each, plus tracking. Falls back to
simulated rates when no API key is configured, so checkout works out of the box.

### Dashboard and reporting

Revenue, order counts, and customer segmentation, all filtered by date range, tier,
and status. The chart plots real per-day order counts, and **Unduh CSV** exports
exactly what the filters select.

![Sales dashboard](docs/screenshots/20-dashboard.png)

### Seller and admin tools

Sellers manage their store profile and product catalogue. Admins verify stores,
create vouchers and categories, and see platform totals. Both areas are guarded by
role policies, not just hidden menu links.

![Seller product management](docs/screenshots/18-kelola-produk.png)

### Light and dark

The theme follows the system preference, can be toggled, and persists across visits.

![Dark theme](docs/screenshots/02-beranda-gelap.png)

### Responsive

![Mobile home](docs/screenshots/10-mobile-beranda.png)

---

## Configuration

Everything is configured through `appsettings.json`.

| Section | Purpose |
|---|---|
| `DatabaseProvider` | `SQLite` (default), `SqlServer`, `MySql`, `PostgreSql` |
| `AI` | LLM provider keys, fallback order, chatbot prompts |
| `VectorDatabase` | RAG document folder, chunk size, reindex interval |
| `PaymentGateways` | Midtrans, Xendit, and Stripe credentials |
| `Shipping` | RajaOngkir key, courier list, origin city |
| `Storage` | File system, MinIO, S3, or Azure Blob |
| `CustomerScoring` | Tier thresholds and scoring weights |
| `RecommendationEngine` | Collaborative and content-based weights |

**Never commit real credentials.** Use user-secrets during development:

```bash
dotnet user-secrets set "AI:Providers:OpenAI:ApiKey" "sk-..."
dotnet user-secrets set "PaymentGateways:Stripe:SecretKey" "sk_test_..."
```

or environment variables in production (`AI__Providers__OpenAI__ApiKey`).

---

## Project layout

```
Lapak/
├── Components/
│   ├── Layout/MainLayout.razor      # shell: nav, sidebar, theme, cart badge
│   ├── Pages/                       # every route
│   └── Shared/ProductCard.razor     # the woven card + notched price tag
├── Controllers/
│   ├── AccountController.cs         # login, register, logout, claim refresh
│   ├── ApiControllers.cs            # uploads, payment webhooks
│   └── ReportsController.cs         # CSV export
├── Data/                            # DbContext and seed data
├── Documents/                       # source documents for RAG
├── Hubs/ChatHub.cs                  # SignalR: chat, notifications, dashboard
├── Models/
│   ├── Configurations/AppConfigs.cs # every config POCO
│   └── …                            # 15 entities on EntityBase
├── Services/
│   ├── SemanticKernel/              # kernel + 9 tools, multi-provider fallback
│   ├── Rag/                         # TF-IDF index + background reindexer
│   ├── Payment/                     # contracts + 3 gateway providers
│   ├── Shipping/                    # RajaOngkir + 7 couriers
│   ├── Storage/                     # file system / MinIO behind a factory
│   ├── RecommendationService.cs     # collaborative + content-based
│   └── CustomerScoringService.cs    # Bronze / Silver / Gold / Platinum
├── wwwroot/app.css                  # the whole design system
└── docs/                            # documentation and screenshots
```

---

## Documentation

- [Architecture](docs/architecture.md)
- [Payment gateways](docs/payments.md)
- [AI configuration](docs/ai-config.md)
- [Dashboard and reporting](docs/dashboard.md)
- [Database setup](docs/database.md)
- [Storage setup](docs/storage.md)
- [Screenshot gallery](docs/screenshots.md)

---

## Tech stack

.NET 10 Blazor Server · Entity Framework Core · ASP.NET Identity · SignalR ·
Microsoft Semantic Kernel · MinIO SDK · Polly

## Notes

The schema is created with `EnsureCreated()`, not migrations. If you change an
entity, delete `lapak.db*` and restart to regenerate the database.

## License

MIT — see LICENSE.

---

*Bahasa Indonesia: [README.id.md](README.id.md)*

Made with ❤️ by Jacky The Code Bender @ Gravicode Studios
