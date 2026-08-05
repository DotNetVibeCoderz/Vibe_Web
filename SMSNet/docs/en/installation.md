# Installation & Configuration

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/instalasi.md)

---

## Requirements

| Component | Version | Notes |
| --- | --- | --- |
| .NET SDK | **10.0** or newer | Required. The old README said .NET 8 — that is stale. |
| OS | Windows, Linux, or macOS | Tested on Windows 11. |
| Database | SQLite | File is created automatically; no separate server. |
| Internet | Needed at runtime | Tailwind, Google Fonts, and Chart.js load from CDNs. Without a connection the app still runs but renders unstyled and chartless. |
| Node.js | Not required | There is no frontend build step. |

---

## Running

```bash
git clone <your-repository>
cd SMSNet

dotnet restore
dotnet run
```

The app listens on:

- `http://localhost:5175`
- `https://localhost:7184`

Launch profiles live in `Properties/launchSettings.json`.

### Default account

| Username | Password | Role |
| --- | --- | --- |
| `admin` | `admin123` | admin |

Seeded on first run. **Change the password before real use** via *Profil Saya*.

### Other commands

```bash
dotnet build          # compile only
dotnet watch          # run with hot reload
dotnet run --launch-profile http
```

Swagger UI is at `/swagger`, **Development environment only**.

---

## Database

The schema is created with `EnsureCreated()`, **not** EF Core migrations. This matters:

> After changing anything in `Models/*.cs` or `Data/ApplicationDbContext.cs`, the
> existing SQLite file is **not** updated. The app will fail or misbehave until the
> file is deleted.

```bash
rm smsnet.db smsnet.db-shm smsnet.db-wal
dotnet run
```

`DbInitializer.SeedAsync` returns early when `Students` already has rows, so seed-data
changes also require the delete above.

EF migrations are tracked as technical debt in [PLAN.md Phase 8](../../PLAN.md).

---

## Configuration

Everything lives in `appsettings.json`. For secrets, **use environment variables** so
they never reach the repository. The nesting separator is a double underscore.

### Database

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=smsnet.db"
}
```

### File storage

```json
"FileStorage": {
  "Provider": "FileSystem",
  "BasePath": "wwwroot/uploads"
}
```

`Provider` accepts `FileSystem` (default), `AzureBlob`, `AwsS3`.

> ⚠️ The `AzureBlob` and `AwsS3` implementations in
> `Services/CloudStoragePlaceholders.cs` are **stubs returning fake paths**. Do not use
> them in production until implemented.

### The assistant

Full detail in the [assistant guide](assistant.md). In short:

```bash
# OpenAI (default)
export Assistant__OpenAI__ApiKey="sk-..."

# or Anthropic
export Assistant__Provider="Anthropic"
export Assistant__Anthropic__ApiKey="sk-ant-..."

# or Google Gemini
export Assistant__Provider="Google"
export Assistant__Google__ApiKey="..."

# or Ollama — no API key, run Ollama locally
export Assistant__Provider="Ollama"

# internet search (optional)
export Assistant__Tavily__ApiKey="tvly-..."
```

On Windows PowerShell:

```powershell
$env:Assistant__OpenAI__ApiKey = "sk-..."
```

### Payments

Full detail in the [payments guide](payments.md). By default the app runs in
**sandbox mode** — no request reaches any provider, so the whole flow can be exercised
without a merchant account.

```json
"Payments": { "SandboxMode": true }
```

---

## Directory layout

```
SMSNet/
├── Components/
│   ├── App.razor              # HTML document, fonts, Tailwind config
│   ├── Routes.razor           # router + access-denied handling
│   ├── Layout/                # MainLayout, AuthLayout, NavMenu
│   ├── Shared/                # shared components + CrudPageBase
│   └── Pages/                 # pages, grouped by feature area
├── Controllers/               # AccountController (login/logout), REST API
├── Data/                      # ApplicationDbContext, DbInitializer
├── Models/                    # entities
├── Services/
│   ├── Assistant/             # Semantic Kernel, connector, plugins
│   ├── Payments/              # payment gateways
│   └── *.cs                   # SchoolClock, AuditService, ToastService, …
├── wwwroot/                   # app.css, chat.css, *.js
├── docs/                      # this documentation
├── PLAN.md                    # roadmap
└── Progress.md                # progress log
```

---

## Common problems

| Symptom | Cause | Fix |
| --- | --- | --- |
| Page renders unstyled | No internet — Tailwind and fonts come from CDNs | Connect, or self-host the assets |
| Charts missing | Chart.js CDN unreachable | As above |
| `SQLite Error: no such table` | Stale schema after an entity change | Delete `smsnet.db*` and re-run |
| Assistant says "belum dikonfigurasi" | No API key | Set the environment variable for your provider |
| "A second operation was started on this context" | Should no longer occur — every page uses `IDbContextFactory` | Report it with the page name if it appears |
| Swagger 404 | Environment is not Development | Set `ASPNETCORE_ENVIRONMENT=Development` |
