using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.OpenApi;
using MudBlazor;
using MudBlazor.Services;
using MyPoS.Api;
using MyPoS.Data;
using MyPoS.Services;
using MyPoS.Services.Import;
using MyPoS.Services.Payments;

var builder = WebApplication.CreateBuilder(args);

// Rupiah dan format tanggal Indonesia sebagai bawaan seluruh aplikasi, sehingga tampilan
// tidak berubah mengikuti locale mesin tempat aplikasi dijalankan.
var defaultCulture = new CultureInfo("id-ID");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// ---------- Infrastruktur ----------
builder.Services.AddMyPosDatabase(builder.Configuration);
builder.Services.AddMyPosStorage(builder.Configuration);

// ---------- Blazor & MudBlazor ----------
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.VisibleStateDuration = 2500;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.PreventDuplicates = true;
});

// ---------- Layanan aplikasi ----------
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Singleton: pengaturan dipakai lintas circuit dan di-cache di memori.
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<PdfReportService>();
builder.Services.AddScoped<MoneyFormatter>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ReceiptService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<ApiKeyService>();

// Impor data master: satu importer per jenis data, dipakai halaman dan dialog impor.
builder.Services.AddScoped<ProductImporter>();
builder.Services.AddScoped<CategoryImporter>();
builder.Services.AddScoped<CustomerImporter>();

builder.Services.AddSingleton<IPaymentGateway, CashPaymentGateway>();
builder.Services.AddSingleton<IPaymentGateway, XenditPaymentGateway>();
builder.Services.AddSingleton<IPaymentGateway, MidtransPaymentGateway>();
builder.Services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddSingleton<PaymentGatewayResolver>();

// ---------- REST API ----------
var apiSection = builder.Configuration.GetSection("Api");
var apiEnabled = apiSection.GetValue("Enabled", true);
var swaggerEnabled = apiSection.GetValue("SwaggerEnabled", true);
var apiPrefix = apiSection.GetValue<string>("RoutePrefix") ?? "/api/v1";

if (apiEnabled)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MyPoS REST API",
            Version = "v1",
            Description =
                "Antarmuka integrasi untuk aplikasi luar: katalog produk, pelanggan, transaksi, dan laporan.\n\n" +
                "**Otentikasi** — sertakan kunci pada header `X-Api-Key` di setiap permintaan. " +
                "Kunci dibuat dari halaman Pengaturan → API di dalam aplikasi. " +
                "Kunci berizin baca akan ditolak pada metode POST, PUT, PATCH, dan DELETE."
        });

        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Name = ApiKeyEndpointFilter.HeaderName,
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Description = "Kunci API, mis. mps_xxxxxxxxxxxx"
        });

        // Menandai seluruh endpoint memerlukan kunci, sehingga tombol Authorize di Swagger UI
        // langsung menyertakan header pada setiap percobaan permintaan.
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
        });

        // Komentar XML dipakai sebagai deskripsi model pada halaman Swagger.
        var xmlPath = Path.Combine(AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
        if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
    });
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/kesalahan");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

await DatabaseBootstrapper.InitialiseAsync(app);

// ---------- Webhook penyedia pembayaran ----------
// Notifikasi hanya dipakai untuk mengetahui invoice mana yang berubah; statusnya selalu
// ditanyakan ulang langsung ke penyedia, sehingga payload palsu tidak bisa memalsukan lunas.
app.MapPost("/api/payments/{provider}/callback", async (
    string provider,
    HttpRequest request,
    CheckoutService checkout,
    SettingsService settingsService,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("PaymentWebhook");
    var settings = await settingsService.GetAsync();

    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    string? invoiceNumber = null;
    try
    {
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var root = doc.RootElement;

        invoiceNumber = provider.ToLowerInvariant() switch
        {
            "xendit" => root.TryGetProperty("external_id", out var e) ? e.GetString() : null,
            "midtrans" => root.TryGetProperty("order_id", out var o) ? o.GetString() : null,
            "stripe" => root.TryGetProperty("data", out var d)
                        && d.TryGetProperty("object", out var obj)
                        && obj.TryGetProperty("client_reference_id", out var c)
                            ? c.GetString()
                            : null,
            _ => null
        };
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest(new { message = "Payload bukan JSON yang sah." });
    }

    // Xendit menyertakan token statis yang bisa dicocokkan tanpa memanggil balik.
    if (provider.Equals("xendit", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(settings.XenditWebhookToken))
    {
        var token = request.Headers["x-callback-token"].ToString();
        if (!string.Equals(token, settings.XenditWebhookToken, StringComparison.Ordinal))
        {
            logger.LogWarning("Callback Xendit ditolak: token tidak cocok.");
            return Results.Unauthorized();
        }
    }

    if (string.IsNullOrWhiteSpace(invoiceNumber))
        return Results.BadRequest(new { message = "Nomor invoice tidak ditemukan pada notifikasi." });

    var status = await checkout.RefreshStatusByInvoiceAsync(invoiceNumber);
    if (status is null)
    {
        logger.LogWarning("Callback {Provider} menyebut invoice tidak dikenal: {Invoice}", provider, invoiceNumber);
        return Results.NotFound(new { message = "Invoice tidak dikenal." });
    }

    logger.LogInformation("Callback {Provider} untuk {Invoice}: status kini {Status}", provider, invoiceNumber, status);
    return Results.Ok(new { invoice = invoiceNumber, status = status.ToString() });
})
.ExcludeFromDescription();

// ---------- REST API untuk aplikasi luar ----------
if (apiEnabled)
{
    app.MapMyPosApi(apiPrefix);

    if (swaggerEnabled)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyPoS REST API v1");
            options.DocumentTitle = "MyPoS REST API";
            options.RoutePrefix = "swagger";
        });
    }
}

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
