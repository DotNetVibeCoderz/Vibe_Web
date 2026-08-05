# Feature Guide

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/fitur.md)

---

## Dashboard

![Dashboard](../img/dashboard.png)

The first page after signing in. Four key figures (active students, active teachers,
today's attendance, outstanding balance), a 14-day attendance trend, grade
distribution, upcoming agenda, and latest notifications.

---

## Academic

### Curriculum & schedule

Two tabs on one page:

- **Curriculum** — the curricula in force with their level and description.
- **Schedule** — grouped by day, filterable by class and day.

### Automatic timetabling

Builds a full week for every class at once, with no teacher booked into two classrooms
at the same hour. The result is a simulation that stays editable cell by cell, re-checked
for conflicts on every change, and only reaches the database once confirmed.
More: [timetabling guide](scheduling.md).

### QR attendance

QR cards for students and teachers, used directly as the attendance token.
Two scanning modes: device camera, or handheld scanner / manual entry.
More: [QR attendance guide](qr-attendance.md).

### Attendance

![Attendance](../img/attendance.png)

Attendance for students and teachers. Four capture methods are supported: barcode,
RFID, biometric, and manual. Statuses: present, absent, sick, excused.

Free-text search, per-column filters, sorting, paging, and CSV export are all available.

### Grades & reports

![Grade entry](../img/nilai-lookup.png)

Per-student, per-subject grade entry with teacher notes. Pass/fail is computed against
the Indonesian minimum standard (KKM) of 75. Summary tiles show the mean, the highest
mark, and how many students are below standard.

Student name and subject are entered through a **lookup** against master data — typing
offers suggestions, and a hint states whether the typed value matches an existing
record. Picking a student **fills in their class automatically**, and that class is
**stored alongside the grade**.

> The class is stored, not re-read from the student record at display time. A grade is
> a record of a moment; reading the class live would silently rewrite last year's
> results the moment a student is promoted.

Scores are validated to 0–100, and the list can be filtered by class.

### E-learning

Modules, videos, quizzes, and online exams. Students can read; only admins and teachers
can add or edit.

Each item can carry a **link** to where the material actually lives — a video, a
document, a quiz form. The open button adapts to the material type ("Tonton" / watch,
"Kerjakan kuis" / take the quiz, "Mulai ujian" / start the exam), opens in a new tab,
and accepts only `http`/`https` addresses.

---

## Teachers & staff

### Teacher dashboard

**Today's** teaching schedule (matched against the Indonesian day name), tasks
approaching their deadline, average grade per subject, and recent forum activity.

### Tasks & exams

Scheduling for assignments, quizzes, and exams. The "time remaining" column counts down
and changes colour as a deadline approaches or passes.

Each task can carry an **optional link** to the paper or form, and targets **several
classes at once, or all of them**. Classes are picked from a row of toggles; leaving it
empty means every class, and such a task still appears when the list is filtered to one
class.

### Internal forum

![Forum and comments](../img/forum-komentar.png)

Discussion between teachers and staff. The author field is pre-filled from the
signed-in account.

Topic bodies are written in a **rich-text editor**, and every topic can be
**commented on** — with file/image attachments and emoji. A comment can only be deleted
by its author or by an admin.

More: [rich text, comments & uploads](collaboration.md).

### Performance review

![Performance review](../img/kinerja.png)

Key performance indicators per teacher. The teacher name comes from a **lookup** over
active staff, and the indicator can be picked from **templates** or written freely.

The score carries an explicit **unit**:

| Unit | Range | Progress bar |
| --- | --- | --- |
| Persen (percent) | 0–100 | yes |
| Skala (scale) | 0–5 | yes, rescaled |
| Teks (text) | free, max 40 chars | no — marked "tidak terukur" |

> The unit is stored, not guessed. Without it the progress bar read the value wrong:
> "4.2" on a 0–5 scale used to be drawn as 4%, not 84%.

Each indicator can be **commented on** separately, under the same deletion rule.

---

## Parents & students

### Parent portal

Pick a student and see, on one page: attendance rate (as a gauge), grade average,
per-subject grade detail, and all of their bills.

### Notifications

School announcements with a target audience (everyone, students, teachers, parents).
Today's notifications are flagged.

### E-payment

See the [payments guide](payments.md).

### Digital documents

![Document upload](../img/dokumen-unggah.png)

Report cards, diplomas, certificates, and official letters. All family roles can
download; only admins manage them.

A document can be **uploaded directly** to the school's server, or recorded as a
**link** to another service. The two are marked with different icons, and deleting a
record removes the physical file only when this app is the one holding it.

More: [rich text, comments & uploads](collaboration.md).

---

## Administration & finance

### Financial management

SPP (tuition), books, activities, uniforms, and fines. Shows total billed, amount
settled, outstanding balance, and collection rate.

### Payment gateways

See the [payments guide](payments.md).

### Inventory

School assets with a category and condition (good, fair, poor).

### Payroll

Teacher and staff salaries per period.

### Period financial reports

Income and expenditure per period, with the surplus.

---

## Analytics & reports

### Analytics dashboard

Four charts: 30-day attendance trend, payment status composition, student distribution
per class, and attendance capture methods in use.

### Data analytics

Operational indicators **with their interpretation** — student-to-teacher ratio, class
occupancy, pass rate, outstanding bills, active tasks, and asset condition. Each carries
a short judgement, not just a number.

### Custom reports

Pick one of eight data sources, filter the rows, then download as CSV.

### Academic report

![Academic report](../img/report-academic.png)

Attendance gauge, grade distribution, and a per-subject summary with each subject's
pass rate.

### Teacher & staff report

Teaching load per teacher, attendance, and KPI results.

### Parent & student report

Highlights students **needing attention** — those in arrears or below the pass mark —
with the reason stated.

### Master-data report

Completeness of each master table, plus **consistency checks**. Because relationships
are stored as names rather than foreign keys, the database cannot enforce integrity
itself; this page does:

- Students pointing at a class that is not registered
- Schedule entries naming an unregistered teacher, subject, or class
- Classes over capacity
- Students without a phone number

---

## Master data

![Student master data](../img/master-students.png)

Four pages sharing one pattern: **students, teachers, subjects, classes**.

Each page has:

- Summary tiles at the top
- Free-text search
- Per-column filters
- Sorting by clicking a column header
- Paging, 10 rows per page
- CSV export (with a BOM, so Excel reads accented names correctly)
- An add/edit form in a dialog
- **Confirmation before deleting**

On the classes page, occupancy is computed from active students whose class name
matches, and shown as a progress bar.

---

## Activities

Events and extracurriculars as a calendar-style list. Past events render dimmed. The
default filter shows what is upcoming.

---

## Security & integration

### Role access

See the [RBAC guide](rbac.md).

### Audit trail

Every create, update, and delete is recorded with its actor and timestamp. Filterable by
actor and exportable to CSV.

### REST API

See the [API guide](api.md).

---

## Pak Dedi

See the [assistant guide](assistant.md).

---

## Application-wide behaviour

| Feature | Detail |
| --- | --- |
| Light/dark theme | Resolved before first paint, so the page never flashes. Follows the system preference until the user chooses. |
| Responsive | The sidebar becomes a drawer below 1024px. Tables scroll inside their own container so the page never scrolls sideways. |
| Delete confirmation | Every deletion goes through a dialog naming the record. |
| Toast feedback | Transient feedback appears bottom-right. |
| CSV export | Uses a UTF-8 BOM so Excel does not mangle accented names. |
| Print | Report pages hide the sidebar and buttons when printed. |
| Considerate motion | All animation respects `prefers-reduced-motion`. |
| Accessibility | A clear marigold focus ring on every interactive element; labels on all inputs. |
