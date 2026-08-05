using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace SMSNet.Services.Assistant.Plugins;

/// <summary>
/// Internet access for the assistant: search, page reading, and fetching a file
/// by URL.
/// <para>
/// These functions fetch URLs the model chose, so every request is screened
/// first — see <see cref="IsPubliclyRoutableAsync"/>. Without that screen a
/// prompt-injected "baca http://169.254.169.254/..." would turn the assistant
/// into a proxy for the host's own network.
/// </para>
/// </summary>
public sealed partial class WebPlugin
{
    private const int MaxCharacters = 12_000;

    private readonly IHttpClientFactory _httpFactory;
    private readonly AssistantOptions _options;
    private readonly ILogger<WebPlugin> _logger;

    public WebPlugin(
        IHttpClientFactory httpFactory,
        IOptions<AssistantOptions> options,
        ILogger<WebPlugin> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    [KernelFunction("cari_internet")]
    [Description("Mencari informasi terkini di internet melalui Tavily. " +
                 "Gunakan untuk pertanyaan tentang peristiwa, harga, regulasi, atau apa pun " +
                 "yang berada di luar data sekolah dan mungkin berubah dari waktu ke waktu.")]
    public async Task<string> SearchAsync(
        [Description("Kata kunci pencarian")] string kueri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(kueri))
        {
            return "Kata kunci pencarian kosong.";
        }

        if (string.IsNullOrWhiteSpace(_options.Tavily.ApiKey))
        {
            return "Pencarian internet belum aktif. Isi Assistant:Tavily:ApiKey pada appsettings untuk mengaktifkannya.";
        }

        try
        {
            var client = _httpFactory.CreateClient("assistant");

            var payload = JsonSerializer.Serialize(new
            {
                api_key = _options.Tavily.ApiKey,
                query = kueri,
                search_depth = _options.Tavily.SearchDepth,
                max_results = _options.Tavily.MaxResults,
                include_answer = true
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(_options.Tavily.Endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return $"Pencarian gagal ({(int)response.StatusCode}). Periksa kunci API Tavily.";
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var sb = new StringBuilder();

            if (root.TryGetProperty("answer", out var answer) &&
                answer.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(answer.GetString()))
            {
                sb.AppendLine($"Ringkasan: {answer.GetString()}").AppendLine();
            }

            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                var index = 1;
                foreach (var item in results.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : "(tanpa judul)";
                    var url = item.TryGetProperty("url", out var u) ? u.GetString() : string.Empty;
                    var snippet = item.TryGetProperty("content", out var c) ? c.GetString() : string.Empty;

                    sb.AppendLine($"{index++}. {title}")
                      .AppendLine($"   URL: {url}")
                      .AppendLine($"   {Shorten(snippet, 400)}")
                      .AppendLine();
                }
            }

            return sb.Length == 0 ? "Tidak ada hasil ditemukan." : sb.ToString();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Tavily search failed for {Query}", kueri);
            return $"Pencarian gagal: {ex.Message}";
        }
    }

    [KernelFunction("baca_halaman")]
    [Description("Membuka sebuah URL halaman web dan mengembalikan isi teksnya. " +
                 "Gunakan setelah cari_internet bila perlu membaca isi lengkap sebuah sumber.")]
    public async Task<string> ScrapeAsync(
        [Description("URL lengkap halaman, diawali http:// atau https://")] string url,
        CancellationToken cancellationToken = default)
    {
        var (ok, message) = await ValidateAsync(url);
        if (!ok)
        {
            return message;
        }

        try
        {
            var client = _httpFactory.CreateClient("assistant");
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return $"Gagal membuka halaman ({(int)response.StatusCode} {response.ReasonPhrase}).";
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var text = HtmlToText(html);

            return $"Isi dari {url}:\n\n{Shorten(text, MaxCharacters)}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Scrape failed for {Url}", url);
            return $"Gagal membaca halaman: {ex.Message}";
        }
    }

    [KernelFunction("baca_file_dari_url")]
    [Description("Mengunduh sebuah berkas teks dari URL (txt, markdown, csv, json, xml) " +
                 "dan mengembalikan isinya. Untuk berkas biner seperti PDF atau Word, " +
                 "minta pengguna melampirkannya langsung ke percakapan.")]
    public async Task<string> ReadFileAsync(
        [Description("URL lengkap berkas")] string url,
        CancellationToken cancellationToken = default)
    {
        var (ok, message) = await ValidateAsync(url);
        if (!ok)
        {
            return message;
        }

        try
        {
            var client = _httpFactory.CreateClient("assistant");
            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return $"Gagal mengunduh berkas ({(int)response.StatusCode}).";
            }

            var mime = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
            var readable = mime.StartsWith("text/")
                           || mime is "application/json" or "application/xml" or "application/csv"
                           || mime.EndsWith("+json") || mime.EndsWith("+xml");

            if (!readable)
            {
                return $"Berkas bertipe '{mime}' tidak dapat dibaca sebagai teks. " +
                       "Silakan lampirkan berkas tersebut langsung ke percakapan.";
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"Isi berkas {url} (tipe {mime}):\n\n{Shorten(body, MaxCharacters)}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "File read failed for {Url}", url);
            return $"Gagal membaca berkas: {ex.Message}";
        }
    }

    // --- URL screening ----------------------------------------------------

    private static async Task<(bool Ok, string Message)> ValidateAsync(string url)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri))
        {
            return (false, "URL tidak valid. Sertakan URL lengkap diawali http:// atau https://.");
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return (false, "Hanya URL http dan https yang didukung.");
        }

        if (!await IsPubliclyRoutableAsync(uri.Host))
        {
            return (false, "URL menunjuk ke alamat jaringan internal dan tidak dapat diakses.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Rejects loopback, private, link-local, and cloud metadata addresses so the
    /// assistant cannot be steered into reading the host's own network.
    /// </summary>
    private static async Task<bool> IsPubliclyRoutableAsync(string host)
    {
        try
        {
            IPAddress[] addresses = IPAddress.TryParse(host, out var literal)
                ? new[] { literal }
                : await Dns.GetHostAddressesAsync(host);

            if (addresses.Length == 0)
            {
                return false;
            }

            foreach (var ip in addresses)
            {
                if (IPAddress.IsLoopback(ip))
                {
                    return false;
                }

                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var b = ip.GetAddressBytes();

                    if (b[0] == 10) return false;                             // 10.0.0.0/8
                    if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return false; // 172.16.0.0/12
                    if (b[0] == 192 && b[1] == 168) return false;              // 192.168.0.0/16
                    if (b[0] == 169 && b[1] == 254) return false;              // link-local + cloud metadata
                    if (b[0] == 127 || b[0] == 0) return false;
                    if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return false; // CGNAT
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // --- Text extraction ---------------------------------------------------

    private static string HtmlToText(string html)
    {
        var text = ScriptAndStyle().Replace(html, " ");
        text = Comments().Replace(text, " ");
        text = BlockBreaks().Replace(text, "\n");
        text = Tags().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = BlankLines().Replace(text, "\n\n");
        text = HorizontalSpace().Replace(text, " ");

        return text.Trim();
    }

    private static string Shorten(string? value, int limit)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= limit
            ? value
            : value[..limit] + $"\n\n[dipotong — {value.Length - limit} karakter berikutnya tidak ditampilkan]";
    }

    [GeneratedRegex(@"<(script|style|noscript|svg)\b[^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptAndStyle();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comments();

    [GeneratedRegex(@"</(p|div|section|article|li|tr|h[1-6]|br)\s*>|<br\s*/?>",
        RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreaks();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\n\s*\n\s*(\n\s*)+")]
    private static partial Regex BlankLines();

    [GeneratedRegex(@"[ \t\f\v]{2,}")]
    private static partial Regex HorizontalSpace();
}
