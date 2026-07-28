# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
dotnet build
dotnet run                                  # https profile: https://localhost:7299 + http://localhost:5295
dotnet run --launch-profile http            # http only: http://localhost:5295
dotnet publish -c Release -o publish
```

There is no test project and no test framework in the repo — nothing to run for tests. Verification is done by running the app.

Single-project solution (`VirtualDoctor.slnx` → `VirtualDoctor.csproj`), .NET 10, Blazor Server. Not a git repository.

## Architecture

Blazor Server (Interactive Server render mode everywhere) + EF Core + Semantic Kernel. One project, layered by folder: `Components` (UI) → `Services` (logic) → `Data` (EF) with `Hubs` (SignalR) and `Workers` (background) alongside.

### Everything pluggable is config-driven, chosen at startup

Six independent provider switches, all read from `appsettings.json` into `Models/AppConfig.cs`:

| Concern | Switch site | Options |
|---|---|---|
| Database | `Program.cs` `AddDbContext` | SQLite (default), SqlServer, PostgreSql, MySql |
| Vector store | `VectorStoreService` ctor | InMemory (default), Sqlite, Qdrant, Chroma, AzureAISearch — provider classes all live in `Services/RAG/VectorStoreService.cs` |
| File storage | `StorageServiceFactory.Create` | FileSystem, S3, MinIO, AzureBlob |
| LLM | `LlmProviderFactory.GetKernel` | OpenAI, Gemini, Anthropic, Ollama, OpenAICompatible |
| Video call | `MeetingService` ctor | None (default), Jitsi, Zoom, Teams |
| Payment | `PaymentService` ctor | Manual (default), Qris, Midtrans, Xendit |

`AppConfig` and its sub-objects are registered as **singletons and mutated at runtime**. Two paths write to them:

- `/settings` → `IAiChatService.UpdateSystemPromptAsync` / `UpdateTemperatureAsync` / `SetBotNameAsync` mutate the singleton only — process-wide, affects all users, **lost on restart**.
- `/admin/settings` → `ISettingsService.SaveAsync` writes `"Section:Sub:Property"` keys to the `AppSettings` table *and* applies them to the live singleton via reflection. `SettingsService.ApplyStoredOverridesAsync` replays them at startup, so **DB overrides beat `appsettings.json`**.

Neither path writes back to `appsettings.json`.

`SettingsService.ApplyToConfig` converts by reflection and only knows `string`, `int`, `long`, `double`, `decimal`, `bool`, and enums — **a property of any other type is silently dropped**, no error. Binding a `<select>` straight to a `bool` field also renders with nothing selected; the admin settings form stores those as `"true"`/`"false"` strings instead. Secrets are masked in the overrides table by `SettingsService.LooksSecret`, which matches on substrings (`ApiKey`, `Secret`, `AccessKey`, `ServerKey`, `Token`, `ConnectionString`) — a new credential whose key matches none of those will be displayed in clear text.

All five LLM providers go through `AddOpenAIChatCompletion` with different base URLs — there are no provider-specific SDKs. Kernels are cached in a plain `Dictionary` on the singleton factory keyed by provider name only; the `temperature` argument to `GetKernel` is ignored (temperature lives in `GetExecutionSettings`).

### AI chat has a silent local fallback

`AiChatService.GetAiResponse` tries Semantic Kernel with auto function-calling, and on **any** exception falls back to `GenerateLocalResponse` — a hardcoded Indonesian keyword/intent matcher that calls `IKernelFunctionService` methods directly. Consequences when debugging:

- A bad API key, network failure, or model error does not surface as an error. The user sees a plausible canned answer; the only signal is a `LogWarning` `[AI] SK failed, using local fallback`.
- Every tool exists **twice**: as an SK plugin method (`GeneralPlugin` / `HealthPlugin`, attribute-decorated, in `Services/AI/KernelFunctionService.cs`) and as an `IKernelFunctionService` method used by the fallback. Adding a tool means touching both.
- `SendStreamingMessageAsync` is not real streaming: it awaits the complete response, then yields it word-by-word with a 25 ms delay.

### RAG uses a placeholder embedding model

`SimpleEmbeddingGenerator` (a `file class` at the bottom of `Program.cs`) produces 256-dim vectors from a byte hash — deterministic but semantically meaningless. Similarity search "works" end-to-end but retrieval quality is arbitrary. Replace it with a real embedding generator before drawing any conclusion about RAG results.

Flow: `PdfIndexingWorker` (registered only when `Indexing:AutoIndex` is true) scans `wwwroot/HealthPdfs` every `IntervalMinutes` → `DocumentIndexingService` extracts text with PdfPig and chunks it → `VectorStoreService.IndexChunksAsync` → queried by `RagQueryService.QueryAsync`, which stuffs the top-5 chunks into a prompt at temperature 0.3.

### Auth is custom, not ASP.NET Identity

Despite the `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package reference, auth is hand-rolled cookie auth:

- Login / register / reset-password are **minimal-API form POST handlers in `Program.cs`**, not Blazor event handlers — interactive Blazor components cannot set auth cookies. The Razor pages under `Components/Pages/Auth/` are plain HTML forms posting to `/auth/*-handler`. Antiforgery middleware is deliberately skipped for `/auth/` paths (`app.UseWhen`).
- Passwords go through `AuthHelpers`: `HashPassword` writes PBKDF2 via `PasswordHasher<ApplicationUser>`, and **`VerifyPassword` is the only correct way to check one** — it returns `Failed | Success | SuccessNeedsRehash` and transparently accepts the legacy unsalted SHA-256 format (64 hex chars) still present on accounts that haven't logged in since the migration. Every caller must rewrite the hash when it gets `SuccessNeedsRehash`, otherwise those rows never migrate. `PasswordHash` is a side table keyed by `UserId`.
- A global fallback authorization policy makes **every** page and endpoint require auth. New anonymous endpoints need `.AllowAnonymous()`; anonymous pages need `@attribute [AllowAnonymous]`.
- Roles live in the `UserRoles` table (`AppRoles.Admin | Doctor | Patient`) and are issued as `ClaimTypes.Role` at sign-in. **Both sign-in paths must build the principal through `AuthClaims.BuildAsync`** — the minimal-API `/auth/login-handler` and `AuthService.LoginAsync` — so claims never diverge. `IsAdmin()` / `IsDoctor()` read the claims; `GetDoctorId()` reads the `vd:doctorId` claim. Gate pages with `@attribute [Authorize(Roles = AppRoles.Admin)]` and endpoints with `.RequireAuthorization(p => p.RequireRole(...))`.
- **Role changes only take effect at next sign-in**, since roles are baked into the auth cookie. `SetRolesAsync` refuses to remove the last Admin.
- `DataSeeder.EnsureRolesAsync` runs on every startup and grants a default role to any user that has none — this is what migrated the old "admin is whoever has `admin@virtualdoctor.com`" convention into data. It is idempotent; it never touches users who already have roles.

### Database: EnsureCreated, no migrations

`Program.cs` calls `db.Database.EnsureCreatedAsync()` then `SchemaUpgrader.UpgradeAsync` then `DataSeeder.SeedAsync`. There are no EF migrations and adding one would conflict with this.

`EnsureCreated` does nothing on an existing database, so **new model properties need a patch in `Data/SchemaUpgrader.cs`** (idempotent `ALTER TABLE` guarded by `PRAGMA table_info`, SQLite only — other providers just get a log warning). Add the column there when you add it to a model, or existing databases will throw at query time. Replacing this with real EF migrations is roadmap item P1-4.

The seeder re-runs only when both `Users` and `Doctors` are empty. Seeded accounts all use password `Password123!` (`budi@email.com` user, `admin@virtualdoctor.com` admin, `andi.pratama@virtualdoctor.com` etc. doctors). In Development with `Seed:DemoTransactions` true, `DemoDataSeeder` also generates 90 days of demo consultations/orders/reviews — turn it off before working with real data. When demo transactions already exist it skips generation but still runs `BackfillPaymentsAsync`, which issues invoices for them if the `Payments` table is empty.

### Payments: every transaction gets an invoice

`Services/Payment/` holds the whole subsystem. `PaymentService.CreateAsync` writes one `Payment` per transaction and returns the same row if one is already open for that reference, so checkout is safe to retry.

- **A failing provider never blocks the patient.** If the active provider is unconfigured or throws, `CreateAsync` falls back to `ManualPaymentProvider` (bank transfer + staff verification) and logs `LogError`. `AvailableChannels` therefore always includes the manual channels. A gateway outage looks like "everyone is transferring manually", not like an error.
- **QRIS is generated locally, not by a gateway.** `QrisPayload.WithAmount` takes the merchant's *static* EMVCo payload from config and turns it into a dynamic one: tag `01` becomes `"12"`, tag `54` (amount) is inserted **in ascending tag order**, and tag `63` CRC-16/CCITT-FALSE (poly `0x1021`, init `0xFFFF`) is recomputed over everything up to and including `"6304"`. Any edit that reorders tags or skips the CRC produces a QR that banking apps reject. `ValidateQrisConfig()` (the "Uji payload QRIS" button in `/admin/settings`) is the quick check.
- `SyncReferenceAsync` is what marks the originating `Order`/`Appointment`/`HomecareService` as paid/confirmed — it runs on verification, on webhook, and on status refresh, but **only when the state becomes `Paid`**.
- Invoice numbers are `INV/yyyy/MM/0001`, allocated by `InvoiceNumbering.NextAsync` from the `InvoiceCounters` table — a single `UPDATE … SET LastNumber = LastNumber + 1` inside a transaction, so the row stays locked until commit and concurrent callers queue instead of reading a stale value. A prefix with no counter row seeds itself from the highest number already issued, so old databases never reuse a printed number. Don't go back to reading `MAX(InvoiceNumber)` — `InvoiceNumber` has a unique index, so a collision fails the whole checkout.
- **Webhooks go through `PaymentWebhookService`, not the endpoints.** `/api/payments/webhook/{midtrans,xendit}` in `Program.cs` are `.AllowAnonymous()` two-liners that hand the raw body to `ReceiveAsync`; all signature checking, status mapping, logging, and idempotency live in the service so that `/admin/webhooks` can replay a stored body through the identical path. Four guards, in order: bad signature → `Rejected` (401); a body whose SHA-256 fingerprint was already decided → `Duplicate`, only bumping `Attempts` (200); a state that would move backwards (`CanTransition` in `PaymentService`) → `Ignored`; a notification timestamped more than 7 days ago → `Ignored`. Every receipt is a row in `PaymentWebhookEvents`. `Rejected` rows can never be replayed — allowing it would make the admin button a way around signature verification.
- `ApplyExternalStatusAsync` returns `ExternalStatusResult(Found, Changed, Message)`, not `bool`; the message is what the admin log shows, so keep it in Indonesian and specific.
- `DashboardService.BuildFinanceAsync` reports from `Payments`, i.e. money actually collected. That is deliberately different from `DashboardData.Revenue`, which sums transaction values whether or not anyone paid. Don't "reconcile" the two — they answer different questions.

### SignalR hubs are parallel to the UI path

`/hubs/chat` and `/hubs/consultation` exist and are `[Authorize]`d, but `AiChat.razor` injects `IAiChatService` and calls it directly (Blazor Server already runs over SignalR). The hubs are for external/other clients — changing chat behavior usually means changing the service, not the hub.

## Conventions

- **UI language is Indonesian.** User-facing strings, page titles, and most code comments are in Indonesian (`Beranda`, `Konsultasi`, `Farmasi`). Keep new strings and comments consistent with that.
- **Interfaces are centralized**, not file-per-type: all of them live in `Services/Interfaces.cs` across four namespaces (`Services`, `Services.AI`, `Services.RAG`, `Services.Storage`). Implementations are grouped into `CoreServices.cs` / `BusinessServices.cs` rather than one file per class.
- **Razor pages are written ultra-dense** — markup and `@code` bodies collapsed onto single long lines (see `Articles.razor`). Match the surrounding density rather than reformatting.
- **Adding a page** means three edits: `@page` route, a `<NavLink>` in `Components/Layout/MainLayout.razor` (inside the `_isDoctor` / `_isAdmin` block if role-gated), and an arm in that layout's `LocationChanged` switch that sets `_pageTitle`.
- **Styling is a hand-rolled `vd-` design system** in `wwwroot/app.css`. No component library — Bootstrap 5.3 and Bootstrap Icons come from CDN in `App.razor` for grid/dropdown/icons only. Theme is `html[data-theme="dark"]` (with `.dark-theme` kept as an alias), set by `wwwroot/js/vd-app.js`, persisted in `localStorage`, defaulting to `prefers-color-scheme`, and applied by an inline script in `App.razor` before first paint.
- **Charts go through `wwwroot/js/vd-charts.js`** (`vdCharts.render(id, type, data, opts)` where type is `ecg | area | donut | bars | heat`). D3 v7 is vendored at `wwwroot/lib/d3/` — do not switch it to a CDN. Chart colors are read from `--vd-chart-*` CSS variables at draw time, and `vd-app.js` fires a `vd:themechange` event that triggers a redraw, so charts must never hardcode colors. `area` stacks its series by default; pass `stack: false` for series that overlap rather than compose (e.g. billed vs collected, where the sum is meaningless). `money: true` switches tooltips and labels to rupiah.
- **Pages under `Layout.MinimalLayout` sit on a fixed dark background** regardless of theme (invoice, receipt). Theme-colored text on them disappears in light mode — give such elements explicit light colors, as `.vd-doc-toolbar .vd-btn-outline` does.
- **Planning docs**: `PLAN.md` is the forward-looking roadmap (work items carry stable IDs like `P0-1`); `Progress.md` records what shipped, when, and how it was verified — update both together, and add new work to `PLAN.md` first so it gets an ID. `docs/feature-audit.md` maps `requirements.md` to actual implementation status; `docs/recommendations.md` explains the reasoning behind each roadmap item.
- `Design.md` at the repo root is a copy of the generic frontend-design skill, not a design spec for this app. `docs/architecture.md` describes the architecture but predates the Chroma vector provider, the review/rating feature, the meeting service, and the analytics layer.

## Notes

`appsettings.json` currently contains live credentials (OpenAI key, Tavily key, Google Maps key, Azure Storage connection string) rather than placeholders. Treat it as secret material: don't echo it into output, docs, or anything published.
