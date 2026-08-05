# Access Control (RBAC)

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/rbac.md)

---

## Four roles

Constants live in `Models/AppRoles.cs` and are all lowercase:

| Constant | Value | Who |
| --- | --- | --- |
| `AppRoles.Admin` | `admin` | Administration and the head teacher |
| `AppRoles.Guru` | `guru` | Teaching staff |
| `AppRoles.Siswa` | `siswa` | Students |
| `AppRoles.OrangTua` | `orangtua` | Parents and guardians |

Compose multiple roles as a string:

```csharp
@attribute [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Guru)]
```

---

## Permission matrix

![RBAC matrix](../img/rbac-matrix.png)

**Security → Role Access** renders this matrix live and exports it to CSV. It is
generated from `Services/NavigationRegistry.cs`.

| Module | Page | admin | guru | siswa | orangtua |
| --- | --- | :---: | :---: | :---: | :---: |
| Summary | Dashboard | ✅ | ✅ | ✅ | ✅ |
| Summary | Pak Dedi (assistant) | ✅ | ✅ | ✅ | ✅ |
| Academic | Curriculum & schedule | ✅ | ✅ | — | — |
| Academic | QR attendance | ✅ | ✅ | — | — |
| Academic | Manual attendance | ✅ | ✅ | — | — |
| Academic | Grades & reports | ✅ | ✅ | — | — |
| Academic | E-learning | ✅ | ✅ | ✅ | — |
| Staff | Teacher dashboard | ✅ | ✅ | — | — |
| Staff | Tasks & exams | ✅ | ✅ | — | — |
| Staff | Internal forum | ✅ | ✅ | — | — |
| Staff | Performance review | ✅ | ✅ | — | — |
| Family | Parent portal | ✅ | — | ✅ | ✅ |
| Family | Notifications | ✅ | ✅ | ✅ | ✅ |
| Family | E-payment | ✅ | — | — | ✅ |
| Family | Digital documents | ✅ | — | ✅ | ✅ |
| Finance | Financial management | ✅ | — | — | — |
| Finance | Payment gateways | ✅ | — | — | — |
| Finance | Inventory | ✅ | — | — | — |
| Finance | Payroll | ✅ | — | — | — |
| Finance | Period reports | ✅ | — | — | — |
| Analytics | Analytics dashboard | ✅ | — | — | — |
| Analytics | Data analytics | ✅ | — | — | — |
| Analytics | Custom reports | ✅ | — | — | — |
| Analytics | Academic report | ✅ | ✅ | — | — |
| Analytics | Staff report | ✅ | ✅ | — | — |
| Analytics | Family report | ✅ | ✅ | — | — |
| Analytics | Finance report | ✅ | — | — | — |
| Analytics | Master-data report | ✅ | — | — | — |
| Master data | Students / teachers / subjects / classes | ✅ | ✅ | — | — |
| Master data | QR cards | ✅ | ✅ | — | — |
| Activities | Events & extracurriculars | ✅ | ✅ | ✅ | ✅ |
| Security | Role access | ✅ | — | — | — |
| Security | Audit trail | ✅ | — | — | — |
| Security | REST API | ✅ | — | — | — |

---

## Four layers of enforcement

Access is not enforced in only one place.

### Layer 1 — Navigation

`NavMenu.razor` renders only what the user's roles can open. This is convenience,
**not** security: hiding a link stops nobody from typing the URL.

### Layer 2 — Page attributes

Every page carries its own `[Authorize]`:

```csharp
@page "/admin/payroll"
@attribute [Authorize(Roles = AppRoles.Admin)]
```

Anonymous pages must say so explicitly with `[AllowAnonymous]`.

`Components/Routes.razor` distinguishes two cases: an anonymous visitor is redirected
to sign in carrying their original destination, while a signed-in user who merely lacks
the role is told so in place — bouncing them to a login form they already passed is
only confusing.

### Layer 3 — API endpoints

The `api/*` controllers carry role attributes, and mutating verbs are restricted
further still:

```csharp
[Authorize(Roles = AppRoles.Admin + "," + AppRoles.Guru)]   // on the controller
public class StudentsController : ControllerBase
{
    [Authorize(Roles = AppRoles.Admin)]   // on writes
    [HttpPost]
    public async Task<ActionResult<Student>> Create(Student student) { … }
}
```

> **Historical note.** Before this development cycle the `api/*` controllers had no
> `[Authorize]` attribute at all. `GET /api/students` returned every minor's name, date
> of birth, guardian, and phone number to any unauthenticated caller. This was the most
> serious audit finding and it has been closed.

### Layer 4 — Assistant functions

This is the layer most easily missed. The assistant runs functions server-side with
full database access, so **the model must not be the thing deciding** who sees what.

`SekolahDataPlugin` checks the caller's role inside the function body:

```csharp
if (!_user.IsStaff)
{
    return Denied("rekap absensi sekolah", "admin atau guru");
}
```

The system prompt is guidance, and a model can be talked out of guidance. A role check
cannot.

| Function | Restriction |
| --- | --- |
| `cari_siswa` | admin, guru |
| `rekap_absensi` | admin, guru |
| `inventaris_sekolah` | admin, guru |
| `rekap_pembayaran` | admin, orangtua |
| `nilai_siswa` | Non-staff must name a student; browsing everyone is staff-only |
| `cari_guru` | All roles, but email and phone appear only for staff |
| The rest | All roles |

---

## Audit trail

Every create, update, and delete is recorded by `Services/AuditService.cs` into the
`AuditTrail` table with the actor and timestamp.

```csharp
await Audit.RecordDeleteAsync("siswa", "Siswa 07");
```

An audit failure **never** fails the operation being audited — a broken audit must not
take down a save.

History is visible at **Security → Audit Trail** and exports to CSV.

---

## Adding a new page

Four steps, in order:

1. Create the page with the correct `[Authorize]` attribute.
2. Register it in `Services/NavigationRegistry.cs` with exactly the same roles.
3. Run the app and check **Role Access** — the matrix grows automatically.
4. Update the table in this document.

If steps 1 and 2 disagree, the menu shows a link that is rejected on click. Keeping
both in one list is how that is avoided.

---

## Creating users

The **Register** page (`/auth/register`) is open to the public and lets the visitor
choose a role.

> ⚠️ **Lock this down for production.** As shipped, anyone can register themselves as
> `admin`. The sensible change is replacing `[AllowAnonymous]` with
> `[Authorize(Roles = AppRoles.Admin)]` so only administrators can issue accounts.

The default `admin` / `admin123` account is created by `DbInitializer` on first run.
Change the password via **Profil Saya** before real use.
