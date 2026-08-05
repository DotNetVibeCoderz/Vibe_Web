# Architecture

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/arsitektur.md)

---

## Overview

SMSNet is **one ASP.NET Core Blazor Server project**. There is no backend/frontend
split, no separate SPA, and no JavaScript build step. That is deliberate: a school
should be able to deploy this without a Node toolchain.

```
┌──────────────────────────────────────────────────────────────┐
│ Browser                                                      │
│   • Tailwind CSS (CDN) + wwwroot/app.css (design tokens)     │
│   • Chart.js (CDN)                                           │
│   • Blazor Server circuit (WebSocket)                        │
└───────────────────────────┬──────────────────────────────────┘
                            │ SignalR
┌───────────────────────────▼──────────────────────────────────┐
│ ASP.NET Core (net10.0)                                       │
│                                                              │
│  Components/          Controllers/         Services/         │
│   ├ Layout            ├ AccountController   ├ Assistant/     │
│   ├ Shared            ├ StudentsController  ├ Payments/      │
│   └ Pages             └ TeachersController  └ (general)      │
│                                                              │
│  Data/ ApplicationDbContext  ──►  SQLite (smsnet.db)         │
└──────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
     LLM provider      Payment gateway       Tavily
  (OpenAI/Anthropic/  (Midtrans/Xendit/     (search)
   Google/Ollama)      Stripe)
```

---

## Architectural decisions

### 1. Every page is interactive, globally

`Components/App.razor` applies `@rendermode="InteractiveServer"` to `<Routes>`, so
**every page is interactive** and no page declares a render mode of its own.

### 2. The authentication split: forms vs components

This is the easiest thing to get wrong here, so it is worth stating plainly.

Identity is registered via `AddIdentityCore` + cookies. A running Blazor circuit
**cannot write a cookie** — the response headers left long ago. Therefore:

| Flow | Mechanism | Why |
| --- | --- | --- |
| **Login & logout** | Plain `<form method="post">` to `Controllers/AccountController.cs` | Must write the auth cookie |
| **Register, profile, password reset** | Ordinary interactive `EditForm` calling `UserManager` | They do not sign anyone in, so no cookie is written |

Follow this split for any new flow that changes the auth cookie.

### 3. Data access through `IDbContextFactory`

`ApplicationDbContext` is registered with
`AddDbContextFactory(..., ServiceLifetime.Scoped)`, which provides both the factory
**and** a scoped instance.

Pages use the **factory**, not an injected context:

```csharp
await using var db = await DbFactory.CreateDbContextAsync();
var students = await db.Students.AsNoTracking().ToListAsync();
```

A Blazor circuit outlives many HTTP requests. Sharing one `DbContext` across awaits
produces *"A second operation was started on this context"* — a bug this application
previously had.

### 4. Flat, denormalised entities

Entities in `Models/Entities.cs` **deliberately have no foreign keys**. Relationships
are stored as display strings:

```csharp
public class ScheduleItem
{
    public string ClassName { get; set; }   // "8A", not ClassRoomId
    public string Teacher { get; set; }     // "Guru 01", not TeacherId
}
```

Match this when adding entities. Introducing real foreign keys would break the seeder
and every page that joins by name.

**One exception:** the chat tables (`ChatSession` → `ChatMessage` → `ChatAttachment`)
use real relationships with cascade delete, because deleting a conversation genuinely
must take its messages and files with it.

A consequence of this design: the database does not enforce integrity. The
**Master Data Report** page therefore performs consistency checks — for example,
schedule entries naming a teacher who is not on the roster.

### 5. `NavigationRegistry` as the single source

`Services/NavigationRegistry.cs` defines every route with the roles allowed to open it.
Three things read it:

1. `NavMenu.razor` — builds the sidebar
2. The **Role Access** page — renders the permission matrix
3. This documentation

Because one definition is shared, a page **cannot** appear in the menu for a role its
own `[Authorize]` attribute then rejects.

### 6. Time always goes through `SchoolClock`

`DateTime.Now` returns the server's clock, which on most hosts is UTC. For an
Indonesian school that shifts attendance and payment records into the wrong day
between 00:00 and 07:00 local time.

All code uses `SchoolClock.Today`, `SchoolClock.LocalNow`, and `SchoolClock.Now`, which
return WIB (UTC+7) and survive the timezone-database differences between Windows and
Linux.

### 7. `CrudPageBase<T>` for master-data screens

Each CRUD page previously copied ~120 lines of search, sort, paging, and export logic
by hand. They drifted: some paged, some didn't, and none confirmed a delete.

`Components/Shared/CrudPageBase.cs` now holds the shared mechanics. Pages supply only
what is specific to their entity:

```csharp
protected override string EntityLabel => "siswa";
protected override DbSet<Student> Table(ApplicationDbContext db) => db.Students;
protected override IEnumerable<string?> SearchableText(Student s) => new[] { s.FullName, s.ClassName };
protected override string Describe(Student s) => s.FullName;
protected override int IdOf(Student s) => s.Id;
```

---

## Design system

There is no CSS build. Tailwind loads from a CDN, and the design tokens live as CSS
custom properties in `wwwroot/app.css`. An inline Tailwind config in `App.razor`
bridges the two so utilities and the component layer cannot drift apart.

### Palette

| Token | Value | Role |
| --- | --- | --- |
| `--tinta` | `#101A2E` | Text and dark surfaces |
| `--dongker` | `#1B3A6B` | Primary (Indonesian junior-high uniform navy) |
| `--kunyit` | `#E8A317` | Accent, active state, achievement |
| `--kapur` | `#F4F5F7` | Page background |
| `--garis` | `#D8DCE4` | Rules and dividers |
| `--daun` | `#2F7D5C` | Success, present, paid |
| `--bata` | `#B3452F` | Alert, absent, overdue |

Dark theme overrides the same tokens under `html.dark`. Because every component reads
tokens, one definition covers both themes.

### Typography

| Role | Typeface |
| --- | --- |
| Display | Bricolage Grotesque |
| Body | Public Sans |
| Numerals & code | IBM Plex Mono |

All numerals are tabular so columns align — the application is essentially a register.

### Component classes

`.sms-card` · `.sms-table` · `.sms-btn` (variants `--primary`, `--accent`, `--ghost`,
`--quiet`, `--danger`) · `.sms-badge` · `.sms-stat` · `.sms-modal` · `.sms-toast` ·
`.sms-gauge` · `.sms-meter` · `.sms-input` / `.sms-select` / `.sms-textarea`

### Theming

`wwwroot/theme.js` owns theme switching. The initial resolution runs in a blocking
`<head>` script so the page never flashes the wrong scheme. Charts redraw on theme
change via the `smsnet:themechange` event.

---

## Request flows

### An ordinary page

```
Browser → Blazor circuit → Page component
                             → IDbContextFactory → fresh DbContext → SQLite
                             → AuditService (on mutations)
                             → ToastService (feedback)
```

### One assistant turn

```
User sends a message
  → AssistantService.SendAsync
    → AssistantKernelFactory.Build(user roles)
      → Kernel + 24 functions (Waktu, Matematika, Web, SekolahData)
    → IChatCompletionService (OpenAI / Anthropic / Google / Ollama)
      → model requests a function call
      → Semantic Kernel invokes it
        → plugin opens its own DI scope → fresh DbContext
      → result returns to the model
    → MarkdownRenderer: Markdig → sanitise → media enrichment
  → persisted as ChatMessage → rendered into the thread
```

### Creating a charge

```
E-Payment page
  → PaymentService.CreateChargeAsync
    → PaymentGatewayRegistry: appsettings overlaid by PaymentGatewayConfig
    → IPaymentGateway.CreateChargeAsync
        sandbox  → simulated locally
        live     → HTTP to the provider
  → PaymentTransaction persisted
  → AuditService records it
```

---

## Key files

| File | Role |
| --- | --- |
| `Program.cs` | All wiring; no startup indirection |
| `Components/App.razor` | HTML document, fonts, Tailwind config, theme script |
| `Components/Routes.razor` | Router and access-denied handling |
| `Services/NavigationRegistry.cs` | Route → role map |
| `Components/Shared/CrudPageBase.cs` | Shared master-data mechanics |
| `Services/SchoolClock.cs` | WIB time |
| `Services/Assistant/AnthropicChatCompletionService.cs` | Claude connector for Semantic Kernel |
| `Services/Payments/PaymentService.cs` | Charge creation and reconciliation |
| `wwwroot/app.css` | All tokens and the component layer |
