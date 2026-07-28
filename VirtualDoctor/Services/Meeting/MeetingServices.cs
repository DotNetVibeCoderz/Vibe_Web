using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Meeting;

/// <summary>Hasil pembuatan meeting dari provider mana pun.</summary>
public record MeetingInfo(
    string Provider,
    string JoinUrl,
    string? HostUrl = null,
    string? MeetingId = null,
    string? Password = null);

public interface IMeetingProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    Task<MeetingInfo> CreateAsync(string topic, DateTime startUtc, int durationMinutes, CancellationToken ct);
}

public interface IMeetingService
{
    /// <summary>Provider aktif saat ini ("None" berarti fitur video dimatikan).</summary>
    string ActiveProvider { get; }
    bool IsEnabled { get; }
    /// <summary>Provider yang kredensialnya sudah lengkap.</summary>
    List<string> ConfiguredProviders { get; }
    Task<MeetingInfo?> CreateMeetingAsync(string topic, DateTime startUtc, int? durationMinutes = null, CancellationToken ct = default);
    /// <summary>Uji kredensial provider aktif. Mengembalikan (berhasil, pesan).</summary>
    Task<(bool Ok, string Message)> TestAsync(CancellationToken ct = default);
}

// ============================================================
// Jitsi - tanpa kredensial, room dibuat dari nama unik
// ============================================================
public class JitsiMeetingProvider : IMeetingProvider
{
    private readonly MeetingConfig _cfg;
    public JitsiMeetingProvider(MeetingConfig cfg) => _cfg = cfg;

    public string Name => "Jitsi";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.Jitsi?.Domain);

    public Task<MeetingInfo> CreateAsync(string topic, DateTime startUtc, int durationMinutes, CancellationToken ct)
    {
        var domain = (_cfg.Jitsi?.Domain ?? "meet.jit.si").TrimEnd('/');
        var prefix = string.IsNullOrWhiteSpace(_cfg.Jitsi?.RoomPrefix) ? "vdoctor" : _cfg.Jitsi!.RoomPrefix;
        var room = $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(40, prefix.Length + 33)];
        var url = $"https://{domain}/{room}";
        return Task.FromResult(new MeetingInfo("Jitsi", url, url, room));
    }
}

// ============================================================
// Zoom - Server-to-Server OAuth
// ============================================================
public class ZoomMeetingProvider : IMeetingProvider
{
    private readonly MeetingConfig _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ZoomMeetingProvider> _log;
    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public ZoomMeetingProvider(MeetingConfig cfg, IHttpClientFactory http, ILogger<ZoomMeetingProvider> log)
    { _cfg = cfg; _http = http; _log = log; }

    public string Name => "Zoom";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_cfg.Zoom?.AccountId) &&
        !string.IsNullOrWhiteSpace(_cfg.Zoom?.ClientId) &&
        !string.IsNullOrWhiteSpace(_cfg.Zoom?.ClientSecret);

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiry) return _token;

        var z = _cfg.Zoom!;
        var client = _http.CreateClient("MeetingClient");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{z.ClientId}:{z.ClientSecret}"));

        var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={Uri.EscapeDataString(z.AccountId)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var res = await client.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Zoom OAuth gagal ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        _token = doc.RootElement.GetProperty("access_token").GetString()!;
        var expires = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expires - 60);
        return _token;
    }

    public async Task<MeetingInfo> CreateAsync(string topic, DateTime startUtc, int durationMinutes, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var client = _http.CreateClient("MeetingClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var host = string.IsNullOrWhiteSpace(_cfg.Zoom?.HostUserId) ? "me" : _cfg.Zoom!.HostUserId;
        var payload = new
        {
            topic,
            type = 2, // scheduled
            start_time = startUtc.ToString("yyyy-MM-ddTHH:mm:ss") + "Z",
            duration = durationMinutes,
            timezone = "UTC",
            settings = new { join_before_host = true, waiting_room = false, approval_type = 2 }
        };

        var res = await client.PostAsJsonAsync($"https://api.zoom.us/v2/users/{host}/meetings", payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Zoom create meeting gagal ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return new MeetingInfo(
            "Zoom",
            root.GetProperty("join_url").GetString()!,
            root.TryGetProperty("start_url", out var s) ? s.GetString() : null,
            root.TryGetProperty("id", out var id) ? id.ToString() : null,
            root.TryGetProperty("password", out var p) ? p.GetString() : null);
    }
}

// ============================================================
// Microsoft Teams - Graph API (client credentials)
// ============================================================
public class TeamsMeetingProvider : IMeetingProvider
{
    private readonly MeetingConfig _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<TeamsMeetingProvider> _log;
    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TeamsMeetingProvider(MeetingConfig cfg, IHttpClientFactory http, ILogger<TeamsMeetingProvider> log)
    { _cfg = cfg; _http = http; _log = log; }

    public string Name => "Teams";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_cfg.Teams?.TenantId) &&
        !string.IsNullOrWhiteSpace(_cfg.Teams?.ClientId) &&
        !string.IsNullOrWhiteSpace(_cfg.Teams?.ClientSecret) &&
        !string.IsNullOrWhiteSpace(_cfg.Teams?.OrganizerUserId);

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token != null && DateTime.UtcNow < _tokenExpiry) return _token;

        var t = _cfg.Teams!;
        var client = _http.CreateClient("MeetingClient");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = t.ClientId,
            ["client_secret"] = t.ClientSecret,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["grant_type"] = "client_credentials"
        });

        var res = await client.PostAsync($"https://login.microsoftonline.com/{t.TenantId}/oauth2/v2.0/token", form, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Teams OAuth gagal ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        _token = doc.RootElement.GetProperty("access_token").GetString()!;
        var expires = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expires - 60);
        return _token;
    }

    public async Task<MeetingInfo> CreateAsync(string topic, DateTime startUtc, int durationMinutes, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var client = _http.CreateClient("MeetingClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            subject = topic,
            startDateTime = startUtc.ToString("yyyy-MM-ddTHH:mm:ss") + "Z",
            endDateTime = startUtc.AddMinutes(durationMinutes).ToString("yyyy-MM-ddTHH:mm:ss") + "Z"
        };

        var url = $"https://graph.microsoft.com/v1.0/users/{_cfg.Teams!.OrganizerUserId}/onlineMeetings";
        var res = await client.PostAsJsonAsync(url, payload, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Teams create meeting gagal ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var join = root.GetProperty("joinWebUrl").GetString()!;
        return new MeetingInfo("Teams", join, join,
            root.TryGetProperty("id", out var id) ? id.GetString() : null);
    }
}

// ============================================================
// Facade
// ============================================================
public class MeetingService : IMeetingService
{
    private readonly MeetingConfig _cfg;
    private readonly Dictionary<string, IMeetingProvider> _providers;
    private readonly ILogger<MeetingService> _log;

    public MeetingService(AppConfig config, IHttpClientFactory http, ILoggerFactory lf)
    {
        _cfg = config.Meeting;
        _log = lf.CreateLogger<MeetingService>();
        _providers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Jitsi"] = new JitsiMeetingProvider(_cfg),
            ["Zoom"] = new ZoomMeetingProvider(_cfg, http, lf.CreateLogger<ZoomMeetingProvider>()),
            ["Teams"] = new TeamsMeetingProvider(_cfg, http, lf.CreateLogger<TeamsMeetingProvider>())
        };
    }

    public string ActiveProvider => string.IsNullOrWhiteSpace(_cfg.Provider) ? "None" : _cfg.Provider;
    public bool IsEnabled => !ActiveProvider.Equals("None", StringComparison.OrdinalIgnoreCase);

    public List<string> ConfiguredProviders =>
        _providers.Where(p => p.Value.IsConfigured).Select(p => p.Key).ToList();

    public async Task<MeetingInfo?> CreateMeetingAsync(string topic, DateTime startUtc, int? durationMinutes = null, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;
        if (!_providers.TryGetValue(ActiveProvider, out var provider))
        {
            _log.LogWarning("[Meeting] Provider '{P}' tidak dikenal", ActiveProvider);
            return null;
        }
        if (!provider.IsConfigured)
        {
            _log.LogWarning("[Meeting] Provider '{P}' belum dikonfigurasi lengkap", provider.Name);
            return null;
        }

        try
        {
            var duration = durationMinutes ?? (_cfg.DefaultDurationMinutes > 0 ? _cfg.DefaultDurationMinutes : 30);
            var info = await provider.CreateAsync(topic, startUtc, duration, ct);
            _log.LogInformation("[Meeting] Dibuat via {P}: {Id}", info.Provider, info.MeetingId);
            return info;
        }
        catch (Exception ex)
        {
            // Konsultasi tetap jalan lewat chat walaupun pembuatan meeting gagal.
            _log.LogError(ex, "[Meeting] Gagal membuat meeting via {P}", provider.Name);
            return null;
        }
    }

    public async Task<(bool Ok, string Message)> TestAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return (false, "Provider meeting dimatikan (None).");
        if (!_providers.TryGetValue(ActiveProvider, out var provider)) return (false, $"Provider '{ActiveProvider}' tidak dikenal.");
        if (!provider.IsConfigured) return (false, $"Kredensial {provider.Name} belum lengkap.");

        try
        {
            var info = await provider.CreateAsync("VirtualDoctor - uji koneksi", DateTime.UtcNow.AddMinutes(10), 15, ct);
            return (true, $"Berhasil. Contoh link: {info.JoinUrl}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
