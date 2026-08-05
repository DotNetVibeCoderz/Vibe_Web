# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

SMSNet — a School Management System (Sistem Manajemen Sekolah) built as a single ASP.NET Core Blazor Server project. UI text and labels are in **Indonesian**; code identifiers are English. `requirements.md` is the original feature spec (Indonesian) and is the source of truth for intended scope.

## Commands

```bash
dotnet restore
dotnet build
dotnet run                    # http://localhost:5175 and https://localhost:7184 (Properties/launchSettings.json)
dotnet run --launch-profile http
dotnet watch                  # hot reload
```

Swagger UI is at `/swagger`, **Development environment only**.

Default login: `admin` / `admin123` (seeded on first run).

There is no test project, no linter config, and no npm/node build step.

### Database resets

Schema is created with `EnsureCreated()` at startup — **there are no EF migrations**. After changing anything in `Models/Entities.cs` or `ApplicationDbContext`, the existing SQLite file will not be updated. Delete it and re-run:

```bash
rm smsnet.db smsnet.db-shm smsnet.db-wal
dotnet run
```

`DbInitializer.SeedAsync` bails out early if `Students` already has rows, so seed data changes also require the delete above.

## Architecture

`Program.cs` wires everything; there is no startup/extension-method indirection. Note the target framework is **net10.0** (the README's "\.NET 8" is stale).

### Rendering

`Components/App.razor` applies `@rendermode="InteractiveServer"` to `<Routes>`, so **every page is interactive server-rendered globally** — individual pages don't declare a render mode.

### Authentication — the split between forms and components

Identity is registered via `AddIdentityCore` + cookie auth. Because an interactive Blazor circuit cannot write cookies, sign-in/sign-out cannot happen in a component:

- **Login and logout** use plain HTML `<form method="post">` posting to `Controllers/AccountController.cs` (`/account/login`, `/account/logout`), which calls `SignInManager` and redirects. `Login.razor` reads `?error=1` and `?ReturnUrl=` from the query string to render failures.
- **Register / profile / reset password** are ordinary interactive `EditForm`s calling `UserManager` directly — they don't sign the user in, so they don't need the controller round-trip.

Follow this split for any new flow that changes the auth cookie.

### Authorization

Every page carries `@attribute [Authorize]` or `@attribute [Authorize(Roles = ...)]`; anonymous pages must be explicit with `[AllowAnonymous]`. Roles come from the constants in `Models/AppRoles.cs` and are lowercase Indonesian: `admin`, `guru`, `siswa`, `orangtua`. Compose multi-role attributes as `AppRoles.Admin + "," + AppRoles.Guru`. `Components/Routes.razor` renders an in-layout "Akses Ditolak" panel for unauthorized users rather than redirecting.

The `api/*` controllers carry no `[Authorize]` — they are currently open.

### Data access

`ApplicationDbContext` (extends `IdentityDbContext<AppUser>`) is registered scoped and **injected directly into components** (`@inject ApplicationDbContext Db`) — no repository or service layer. All pages follow this; keep it consistent rather than introducing a factory unless the whole app is converted. Most page code queries synchronously in `OnInitializedAsync`.

Entities in `Models/Entities.cs` are deliberately **flat and denormalized**: relationships are stored as display strings, not foreign keys (e.g. `ScheduleItem.Teacher` is a teacher's name, `GradeRecord.StudentName` is a name). There are no navigation properties anywhere. Match this when adding entities — introducing real FKs would break the seeder and every page that joins by name.

### Page conventions

Pages live under `Components/Pages/<Area>/`, grouped by feature area (Academic, Admin, Analytics, Auth, Master, Parent, Reports, Security, Teacher, Integration).

**Master data CRUD pages** (`Components/Pages/Master/*.razor`) share one hand-rolled pattern — copy the closest one when adding another:
- Full table loaded into a `List<T>` field via `AsNoTracking()`; filtering/sorting/paging done in memory through computed properties (`Filtered…`, `Paged…`), `PageSize = 10`.
- Inline add/edit form toggled by a `ShowForm` bool, editing a detached copy in `FormModel`.
- CSV export builds the string in C# and calls `smsnetDownload` via JS interop.

**Report/dashboard pages** render Chart.js canvases: compute data in `OnInitializedAsync`, then call `smsnetChart.render(canvasId, type, labels, data, label)` from `OnAfterRenderAsync` guarded by a `_chartRendered` bool (charts must not be re-rendered on every render pass).

**New pages must be added by hand to the sidebar** in `Components/Layout/MainLayout.razor` — the nav is a hardcoded list of `NavLink`s grouped by area.

### Styling

Tailwind is loaded **from CDN in `Components/App.razor`** — there is no Tailwind config, no PostCSS, no build step, and no purge. Utility classes are written inline in markup with explicit `dark:` variants. Chart.js is also CDN-loaded, so the app renders unstyled/chartless without network access.

Project-specific classes (`.sms-card`, `.sms-badge`, `.sms-gauge`) live in `wwwroot/app.css`; each needs a matching `.dark` rule added manually. Theme toggling is `wwwroot/theme.js` (`smsnetTheme.toggle/load`), which swaps `dark`/`light` classes on `<html>` and persists to `localStorage`. JS helpers are globals attached to `window` in `wwwroot/*.js` and referenced by name from `IJSRuntime`.

### File storage

`Services/` exposes `IFileStorage` behind `IFileStorageFactory`, selected by the `FileStorage:Provider` setting (`FileSystem` default, `AzureBlob`, `AwsS3`). The Azure and S3 implementations in `CloudStoragePlaceholders.cs` are **stubs that return fake paths** — treat them as unimplemented.
