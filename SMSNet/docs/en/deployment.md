# Deployment

[← Back to documentation index](../README.md) · [Versi Bahasa Indonesia](../id/deployment.md)

---

## Pre-production checklist

Do not skip this section. Several items below are genuine security issues.

| # | Item | Default state | Action |
| --- | --- | --- | --- |
| 1 | Admin password | `admin123` | **Change immediately** via Profil Saya |
| 2 | Registration page | Public, and lets the visitor pick the `admin` role | Replace `[AllowAnonymous]` with `[Authorize(Roles = AppRoles.Admin)]` in `Components/Pages/Auth/Register.razor` |
| 3 | API keys | Empty | Supply via environment variables, **not** appsettings |
| 4 | HTTPS | Available | Required; `UseHsts()` is already on outside Development |
| 5 | Database migrations | None | Move to EF migrations before real data lands — see below |
| 6 | Password reset | Direct, no email | Replace with a single-use emailed link |
| 7 | Payment credentials | Stored as-is in the database | Consider Data Protection or a secrets vault |
| 8 | Cloud storage | `AzureBlob` and `AwsS3` are stubs | Implement if you intend to use them |
| 9 | Backups | None | Schedule copies of `smsnet.db` |
| 10 | Swagger | Development only | Already correct — leave it |

---

## The database migration problem

The schema is created with `EnsureCreated()`, not EF Core migrations. That means:

- Entity changes **cannot** be applied to a database that already holds data.
- The only way to update the schema is deleting the file, which means **losing
  everything in it**.

For a real school that is unacceptable. Move to EF Core migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
```

then replace the `EnsureCreated()` call in `Program.cs`:

```csharp
// before
dbContext.Database.EnsureCreated();

// after
await dbContext.Database.MigrateAsync();
```

Do this **before** real data is entered.

---

## Publishing

```bash
dotnet publish -c Release -o ./publish
```

The output contains everything needed, including `wwwroot`.

### Running

```bash
cd publish
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="http://0.0.0.0:5000"
export Assistant__OpenAI__ApiKey="sk-..."
dotnet SMSNet.dll
```

---

## Environment variables

The nesting separator is a **double underscore**.

```bash
# Required in production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ConnectionStrings__DefaultConnection="Data Source=/var/lib/smsnet/smsnet.db"

# Assistant
Assistant__Provider=OpenAI
Assistant__OpenAI__ApiKey=sk-...
Assistant__Tavily__ApiKey=tvly-...

# Payments
Payments__SandboxMode=false
Payments__Gateways__2__SecretKey=SB-Mid-server-...
```

---

## Docker

There is no `Dockerfile` in the repository. Here is one that works:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Keep the database on a volume so it survives container replacement
VOLUME /data
ENV ConnectionStrings__DefaultConnection="Data Source=/data/smsnet.db"
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SMSNet.dll"]
```

```bash
docker build -t smsnet .
docker run -d -p 8080:8080 \
  -v smsnet-data:/data \
  -e Assistant__OpenAI__ApiKey="sk-..." \
  --name smsnet smsnet
```

---

## Reverse proxy

Blazor Server needs **WebSockets**. The proxy must forward them.

### Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name smsnet.school.sch.id;

    ssl_certificate     /etc/letsencrypt/live/smsnet.school.sch.id/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/smsnet.school.sch.id/privkey.pem;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;

        # Required for the Blazor circuit
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";

        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        # Don't cut idle circuits too aggressively
        proxy_read_timeout 100s;
    }
}
```

Behind a proxy, add forwarded-header handling to `Program.cs`:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

---

## CDN dependencies

The application loads from the internet:

| Asset | Source |
| --- | --- |
| Tailwind CSS | `cdn.tailwindcss.com` |
| Google Fonts | `fonts.googleapis.com`, `fonts.gstatic.com` |
| Chart.js | `cdn.jsdelivr.net` |

Without internet access the application **still runs**, but renders unstyled and
chartless. For a school on a closed network, download all three into `wwwroot` and
update the references in `Components/App.razor`.

Worth noting: Tailwind's CDN build is not intended for production. For a serious
installation, consider building your own CSS file — although that means introducing the
Node build step this project has deliberately avoided.

---

## Backups

All data lives in one SQLite file.

```bash
# Safe copy while the app is running
sqlite3 /var/lib/smsnet/smsnet.db ".backup '/backup/smsnet-$(date +%F).db'"
```

Include the `wwwroot/uploads` directory too — it holds chat attachments and documents.

---

## Monitoring

Logs go to the console and follow `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

The error page shows a **request code** (trace identifier) that matches a log line — ask
users to include it when reporting a problem.

---

## Performance notes

| Area | Suggestion |
| --- | --- |
| SQLite | Enable WAL mode for better concurrent reads |
| Blazor circuits | Cap concurrent circuits if user numbers are high |
| Another database | `ApplicationDbContext` holds nothing SQLite-specific; moving to PostgreSQL is a matter of swapping the `UseSqlite` call |
| Static assets | `MapStaticAssets()` already handles fingerprinting and compression |
