# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Lapak — an Indonesian-market e-commerce app built as a **single .NET 10 Blazor Server project** (`Lapak.csproj`, solution `Lapak.slnx`). All user-facing copy, seed data, and AI prompts are in Indonesian; keep new strings in Indonesian to match.

Not a git repository. There are no tests and no test project.

## Commands

```bash
dotnet run                   # https://localhost:7205 (https profile is the default)
dotnet run --launch-profile http
dotnet watch run             # hot reload during UI work
dotnet build                 # NU1903/NU1608/NU1510 package warnings are pre-existing
```

No lint step, no test runner. Verification for a change = `dotnet build` plus running the app and exercising the page.

**Screenshots** for README/docs are generated from the running app:

```bash
rm -f lapak.db* && dotnet run        # clean seeded data
node scripts/shoot.js http://localhost:5247 docs/screenshots
```

The script (Playwright) signs in as the demo accounts, fills a cart, and steps through checkout so each page has real content. Re-run it after any UI change that affects a documented screen.

## Runtime setup you must know before changing anything

- **Schema is created with `EnsureCreated()`, not migrations.** There is no `Migrations/` folder. Any change to an entity or to `OnModelCreating` will **not** apply to an existing DB — delete `lapak.db*` (three files: `.db`, `.db-shm`, `.db-wal`) and restart. Don't introduce `dotnet ef migrations` unless converting the whole project.
- **Seeding** runs only when `Categories` is empty (`SeedData.Initialize`), using a fixed `Random(42)` so a delete-and-rerun reproduces identical data. Every seeded account has the password `SeedData.DemoPassword` (`Lapak2025!`); `admin.lapak@lapak.com` is the Admin, the first 12 users are Sellers.
- **Database provider** is switched by the `DatabaseProvider` key in `appsettings.json` (`SQLite` default / `SqlServer` / `MySql` / `PostgreSql`), each with its own `ConnectionStrings` entry.
- **Everything is `InteractiveServer`**: `Components/App.razor` sets `<Routes @rendermode="InteractiveServer" />` globally; individual pages do not declare a render mode.
- **Static assets**: `app.MapStaticAssets()` serves the fingerprinted assets `@Assets[...]` and `<ImportMap>` resolve to, including `blazor.web.js`. `app.UseStaticFiles()` is kept alongside it for runtime-written files under `wwwroot/uploads`, which are not in the build manifest. `builder.WebHost.UseStaticWebAssets()` is called explicitly so a non-published `dotnet run` works outside Development. Removing any of these silently kills interactivity — every button becomes a no-op.

## Architecture

### Pages talk to the database directly

Most Razor pages `@inject Lapak.Data.LapakDbContext Db` and query EF Core inline. There is no repository or CQRS layer — services under `Services/` exist only for cross-cutting concerns. Follow the existing pattern rather than introducing an abstraction for one page.

Blazor Server consequence: the injected `LapakDbContext` is scoped to the whole circuit. **Never run two awaited queries concurrently on it.** `MainLayout` therefore creates its own scope via `IServiceScopeFactory` — its cart-badge refresh fires on navigation and must not collide with a page's in-flight query.

### Auth is cookie-based via controller endpoints, not Blazor callbacks

`Login.razor` and `Register.razor` render plain HTML `<form method="post">` posting to `AccountController`, because **an interactive Blazor circuit cannot write a cookie**. The same applies to `/account/logout` and `/account/refresh`. Errors come back as `?error=` query strings read via `[SupplyParameterFromQuery]`. Keep that shape for any new auth flow.

### Roles come from a column, not AspNetRoles

`User.UserType` is `Buyer` / `Seller` / `Admin`. `LapakClaimsPrincipalFactory` (registered via `.AddClaimsPrincipalFactory<>`) projects it onto the standard role claim at sign-in, which is what makes these work:

```razor
@attribute [Authorize(Policy = "AdminOnly")]
@attribute [Authorize(Policy = "SellerOnly")]
```

`Routes.razor` uses `AuthorizeRouteView` — without it, `[Authorize]` on a component is not enforced at all. Because claims are baked into the cookie, a user whose `UserType` changes mid-session (a buyer opening a shop) must be routed through `/account/refresh?redirect=…` to have the cookie rewritten.

### AI: Semantic Kernel with provider fallback

`Services/SemanticKernel/SkChatService.cs`

- Two chatbots keyed by string: `"TonyKurus"` and `"SitiBohay"`. Names, system prompts, temperature, and max tokens all live in `appsettings.json` under `AI:ChatBots` — **prompt changes are config changes, not code changes**.
- `GetKernel(provider)` builds a fresh `Kernel` per call, always via `AddOpenAIChatCompletion` pointed at the provider's `BaseUrl`, so every provider runs through the OpenAI-compatible surface. It also registers the request's `LapakDbContext` and the singleton `IVectorRagService` into that kernel's own container so the plugins can query.
- Fallback order is the hardcoded `FallbackOrder = { "OpenAI", "Gemini", "Anthropic", "Ollama" }`, starting at `AI:DefaultProvider`, gated by `AI:FallbackEnabled`.
- Tools are plugin classes at the bottom of the same file — `ProductSearchTools`, `StoreSearchTools`, `OrderTools`, `KnowledgeBaseTools`, `GeneralTools` — using `[KernelFunction]` + `[Description]` (descriptions are Indonesian; the model reads them). **To add a tool, add a `[KernelFunction]` method to one of those classes** — registration goes through `AddFromType<T>`, no other wiring.
- `ChatStreamAsync` buffers a provider's whole response before yielding, so fallback can still take over mid-failure.

### RAG

`Services/Rag/VectorRagService.cs` is a **singleton in-memory TF-IDF index** over files in `Documents/`, rebuilt by `VectorIndexingBackgroundService` every `VectorDatabase:ReindexIntervalMinutes`.

The index is built off to the side and swapped in as one immutable snapshot. **No lock is ever held across an `await`** — the previous version did that with a `ReaderWriterLockSlim`, which left the lock permanently stuck when the continuation resumed on another thread and hung the whole chat page. Keep that property.

`SitiBohayChat.razor` also calls `RagService.SearchAsync` directly to show citations next to the answer, while the model can call `search_knowledge_base` itself. Only the `InMemory` provider is implemented; Sqlite/PostgreSql/Qdrant/Filesystem are config-only.

### Payments: one interface, three gateways

`Services/Payment/` — `PaymentContracts.cs` (DTOs, `PaymentState`, `IPaymentProvider`), `PaymentService.cs` (router), and one provider each for Midtrans, Xendit, and Stripe.

- `PaymentService` receives providers as `IEnumerable<IPaymentProvider>` and dispatches on `PaymentRequest.Gateway`. **Adding a gateway means one new class plus one `AddScoped<IPaymentProvider, …>` line in `Program.cs`** — no switch to update; the checkout page reads `GetAvailableGateways()`.
- Providers own their protocol; `PaymentService.ApplyState` is the **only** place order status columns change, so all three gateways move an order through identical transitions.
- Every provider verifies inbound webhooks: SHA-512 body signature (Midtrans), `x-callback-token` header (Xendit), timestamped HMAC-SHA256 in `Stripe-Signature` (Stripe). All use constant-time comparison and answer 401 on mismatch. Never weaken this.
- Stripe uses the raw REST API (no SDK) to match the other two. IDR is zero-decimal there — `unit_amount` is whole rupiah, not ×100.

See `docs/payments.md`.

### Config binding

Every section binds to a POCO in `Models/Configurations/AppConfigs.cs` and is registered with `builder.Services.Configure<T>(...)`. Consume via `IOptions<T>` — do not read `IConfiguration` directly in services. Adding a section = POCO + `Configure<T>` line + `appsettings.json` block.

### Storage

`IStorageService` is **not** registered in DI. Inject `StorageServiceFactory` and call `GetStorageService()`, which picks from `Storage:Provider`.

### SignalR

Three hubs in `Hubs/ChatHub.cs`: `/hubs/chat`, `/hubs/notifications`, `/hubs/dashboard`. Mapped and functional, but the chat pages call `ISkChatService` directly from the component rather than routing through `ChatHub`.

### Entities

All models derive from `EntityBase` (`Guid Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Relationship config, unique indexes (`Slug` on Product/Store/Category), and composite uniques live in `LapakDbContext.OnModelCreating` — there are no `IEntityTypeConfiguration` classes despite the `Models/Configurations` folder name (that folder holds config POCOs).

## Design system

`wwwroot/app.css` is the entire design system — one file, token-driven, no framework.

The identity comes from the word *lapak*: the woven mat a vendor lays out at the pasar. Concretely:

- **Palette**: nila (indigo `#1B2A4A`) as brand and ink, kunyit (turmeric `#C8791A`) as the single bright accent for prices and primary actions. Signals are muted market colours, not stock Bootstrap hues.
- **Type**: Bricolage Grotesque for display, Plus Jakarta Sans for body (loaded from Google Fonts in `App.razor`). Prices and table figures use `font-variant-numeric: tabular-nums`.
- **The weave**: `.weave` / `.weave-tile` / `.product-image` generate an *anyaman* pattern from CSS gradients, hue set per element via `--weave-hue`, with `--weave-ink` giving the matching icon tint. This replaces product photography, which the dataset doesn't have.
- **The icons**: `Components/Shared/GlyphIcon.razor` is a hand-drawn line-art set (24×24, `currentColor`, 1.5 stroke). Use it instead of emoji for anything structural — nav, categories, products, stores, stat tiles. Emoji render differently per platform and cannot be tinted. Emoji remain only where they act as a character avatar (the two chatbot personas). Add a new icon as one more arm of the `switch` in that file.
- **The mapping**: `Services/ProductIcons.cs` is the single source for category → icon key and category → weave hue (`For()`, `HueFor()`, `StableHue()`). Every surface that draws a product must call it — the earlier per-page copies drifted apart.
- **The signature**: `.product-price` is a notched chalk price tag (`clip-path` cuts one corner), like the cut-cardboard tags clipped to goods in a market. It is the one loud element — keep everything around it quiet.
- **The awning**: `.top-nav::after` is a striped market canopy. It is the only purely ornamental element in the system; don't add more.

### Theming — two rules that are easy to break

**`--accent` is ink, `--surface-brand` is fill.** Dark mode inverts the first and not the second. Painting a panel with `--accent` and its text with a light colour produces near-white-on-near-white in dark mode — that exact bug shipped in the hero and CTA. Filled brand panels use `--surface-brand` / `--on-surface-brand` / `--on-surface-brand-dim` / `--on-surface-brand-line`.

**The theme class lives on `<html>`.** An inline script in `App.razor` sets it from `localStorage` before first paint (no flash), `lapak.setTheme` in `wwwroot/lapak.js` flips it, and `MainLayout` keeps a mirror class on `.lapak-app` for its own reactivity. Putting the class only on the layout `div` leaves `body` painted with the light tokens, and the page shows a light ground behind a dark shell.

When adding a dark-mode value, check it against a screenshot rather than by reading the hex — `node scripts/shoot.js` captures both themes.

Use the existing tokens rather than hardcoding values, and prefer the shared classes (`.card`, `.option-row`, `.empty-state`, `.chip`, `.data-table`, `.alert`) over new inline styles. Page-specific CSS goes in a `<style>` block at the bottom of that page's `.razor` file (note: `@` must be escaped as `@@` in Razor `<style>` blocks — e.g. `@@media`).

The project ships a local `frontend-design` skill in `.claude/skills/`.

## Known gaps (present in code — don't assume they work)

- `Services/AI/LlmService.cs` (`ILlmService`) is dead code: never registered, never injected. `SkChatService` superseded it.
- `IsDeleted` exists on every entity but no query filters on it — soft delete is not implemented. Deletion is real, or via `IsActive` flags.
- `MinioStorageService.DeleteAsync` is a logging placeholder; `AmazonS3` maps to the MinIO service and `AzureBlob` falls back to the file system in `StorageServiceFactory`.
- `ChatMessage` / `ChatMessages` exist, but chat pages keep history in component state only — nothing is persisted.
- `GeneralTools.search_internet` is explicitly a simulation.
- Payment and shipping credentials are empty by default; `ShippingService` falls back to simulated costs and tracking, and checkout shows unconfigured gateways as disabled.
- Google SSO on the login page is a disabled placeholder, and `ForgotPassword` shows a confirmation without sending mail.
