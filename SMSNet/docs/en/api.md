# REST API

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/api.md)

---

## Authentication

**Every endpoint requires a signed-in session.** The API uses the same authentication
cookie as the web application, so a client signs in first and then presents that cookie
on each request.

Requests without a session receive `401 Unauthorized`.

```bash
# 1. Sign in and save the cookie
curl -c cookies.txt -X POST http://localhost:5175/account/login \
  -d "UserName=admin&Password=admin123"

# 2. Call an endpoint with it
curl -b cookies.txt http://localhost:5175/api/students
```

> **Historical note.** Before this development cycle every `api/*` endpoint carried no
> `[Authorize]` attribute. `GET /api/students` returned personal data about minors —
> names, dates of birth, guardians, phone numbers — to any unauthenticated caller. That
> hole is now closed.

---

## Permissions

| Operation | Roles |
| --- | --- |
| Read (`GET`) | `admin`, `guru` |
| Write (`POST`, `PUT`, `DELETE`) | `admin` |

---

## Endpoints

### Students

| Method | Route | Purpose | Roles |
| --- | --- | --- | --- |
| `GET` | `/api/students` | List all students | admin, guru |
| `GET` | `/api/students/{id}` | One student | admin, guru |
| `POST` | `/api/students` | Create | admin |
| `PUT` | `/api/students/{id}` | Update | admin |
| `DELETE` | `/api/students/{id}` | Delete | admin |

### Teachers

| Method | Route | Purpose | Roles |
| --- | --- | --- | --- |
| `GET` | `/api/teachers` | List all teachers | admin, guru |
| `GET` | `/api/teachers/{id}` | One teacher | admin, guru |
| `POST` | `/api/teachers` | Create | admin |
| `PUT` | `/api/teachers/{id}` | Update | admin |
| `DELETE` | `/api/teachers/{id}` | Delete | admin |

---

## Payload shapes

### Student

```json
{
  "id": 1,
  "fullName": "Siswa 01",
  "className": "9B",
  "dateOfBirth": "2013-09-25T00:00:00",
  "gender": "Laki-laki",
  "parentName": "Orang Tua 01",
  "phone": "0812-000001",
  "status": "Active"
}
```

### Teacher

```json
{
  "id": 1,
  "fullName": "Guru 01",
  "subject": "Matematika",
  "email": "guru01@smsnet.sch.id",
  "phone": "0813-77001",
  "status": "Active"
}
```

`status` is `Active` or `Inactive`.

---

## Status codes

| Code | Meaning |
| --- | --- |
| `200` | Success |
| `201` | Created (with a `Location` header) |
| `204` | Success, no content (after `PUT` or `DELETE`) |
| `400` | Invalid request — including a route `id` that differs from the body `id` |
| `401` | Not signed in |
| `403` | Signed in, but the role is not permitted |
| `404` | Not found |

---

## Swagger

Swagger UI is at `/swagger`, **Development environment only**. In other environments
the endpoint is not mapped at all.

The in-app page **Security → REST API Integration** lists the same endpoints with the
roles each requires, and links to Swagger when running in Development.

---

## Worked example

```bash
BASE=http://localhost:5175

# Sign in
curl -c cookies.txt -X POST $BASE/account/login \
  -d "UserName=admin&Password=admin123"

# List
curl -b cookies.txt $BASE/api/students

# One record
curl -b cookies.txt $BASE/api/students/1

# Create
curl -b cookies.txt -X POST $BASE/api/students \
  -H "Content-Type: application/json" \
  -d '{
        "fullName": "Budi Santoso",
        "className": "8A",
        "dateOfBirth": "2012-04-17T00:00:00",
        "gender": "Laki-laki",
        "parentName": "Santoso",
        "phone": "0812-3456789",
        "status": "Active"
      }'

# Update
curl -b cookies.txt -X PUT $BASE/api/students/41 \
  -H "Content-Type: application/json" \
  -d '{ "id": 41, "fullName": "Budi Santoso", "className": "8B", "status": "Active" }'

# Delete
curl -b cookies.txt -X DELETE $BASE/api/students/41
```

---

## Limitations

Stated plainly:

- **No token authentication.** The API uses the session cookie, which suits
  server-to-server integration on the same network but is awkward for third-party
  clients. API keys or OAuth are the next piece of work.
- **No pagination.** `GET /api/students` returns every row. A school with thousands of
  students will need this added.
- **No rate limiting.**
- **Narrow coverage.** Only students and teachers are exposed. Other entities are
  reachable through the interface only.
- **No versioning.** A change to a payload shape reaches clients immediately.
