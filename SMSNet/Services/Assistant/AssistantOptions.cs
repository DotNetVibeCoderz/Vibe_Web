namespace SMSNet.Services.Assistant;

/// <summary>
/// Everything about the assistant that an operator can change without a rebuild.
/// Bound from the <c>Assistant</c> section of appsettings.json.
/// </summary>
public class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>What the assistant is called in the interface.</summary>
    public string Name { get; set; } = "Pak Dedi";

    public string Tagline { get; set; } = "Asisten informasi sekolah";

    /// <summary>OpenAI | AzureOpenAI | Anthropic | Google | Ollama</summary>
    public string Provider { get; set; } = "OpenAI";

    /// <summary>
    /// The persona, written as one line per array entry — JSON has no multi-line
    /// string, and a single escaped blob is unusable to edit by hand.
    /// Overrides <see cref="SystemPrompt"/> when non-empty.
    /// </summary>
    public string[] SystemPromptLines { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Resolved persona: the configured lines if present, else the configured
    /// single string, else the built-in default. An operator who blanks the key
    /// in appsettings gets the default back rather than an empty prompt.
    /// </summary>
    public string ResolveSystemPrompt()
    {
        if (SystemPromptLines.Length > 0)
        {
            return string.Join('\n', SystemPromptLines);
        }

        return string.IsNullOrWhiteSpace(SystemPrompt) ? DefaultSystemPrompt : SystemPrompt;
    }

    /// <summary>The persona. Sent as the system prompt on every turn.</summary>
    public string SystemPrompt { get; set; } = DefaultSystemPrompt;

    private const string DefaultSystemPrompt =
        """
        Kamu adalah "Pak Dedi", asisten informasi resmi sekolah pada aplikasi SMSNet.

        Kepribadian:
        - Ramah, sabar, dan sopan — seperti staf tata usaha senior yang sudah lama bekerja di sekolah.
        - Menjawab dalam Bahasa Indonesia yang jelas dan tidak bertele-tele. Ikuti bahasa penanya bila ia menggunakan bahasa lain.
        - Panggil lawan bicara dengan "Bapak/Ibu" untuk guru dan orang tua, dan "Ananda" untuk siswa, bila perannya diketahui.

        Cara kerja:
        - Untuk pertanyaan tentang data sekolah (siswa, guru, kelas, jadwal, absensi, nilai, pembayaran, inventaris, kegiatan),
          gunakan fungsi SekolahData. Jangan menebak angka — ambil dari fungsi.
        - Untuk informasi terkini di luar sekolah, gunakan fungsi Web (pencarian internet atau baca halaman).
        - Untuk perhitungan, gunakan fungsi Matematika agar hasilnya tepat.
        - Untuk tanggal dan waktu, gunakan fungsi Waktu — jangan mengarang tanggal hari ini.
        - Bila data tidak ditemukan, katakan terus terang dan sarankan langkah berikutnya.

        Format jawaban:
        - Gunakan Markdown. Sajikan data berjumlah banyak sebagai tabel.
        - Sebutkan sumber (nama fungsi atau tautan) bila jawabanmu berasal dari pencarian atau data.
        - Jawaban ringkas dan langsung ke inti; tambahkan detail hanya bila diminta.

        Batasan:
        - Jangan pernah menampilkan kata sandi, token, atau kunci API.
        - Jangan memberi nasihat medis, hukum, atau keuangan pribadi yang mengikat.
        - Hormati privasi: sampaikan data pribadi siswa hanya secara ringkas dan seperlunya.
        """;

    /// <summary>
    /// Sampling temperature. Applies to OpenAI, Google, and Ollama.
    /// Current Anthropic models reject this parameter, so it is not sent there.
    /// </summary>
    public double Temperature { get; set; } = 0.4;

    public double TopP { get; set; } = 0.95;

    public int MaxTokens { get; set; } = 2048;

    /// <summary>How many prior turns to replay. Keeps long threads inside the context window.</summary>
    public int HistoryWindow { get; set; } = 20;

    /// <summary>Let the model call kernel functions on its own.</summary>
    public bool EnableFunctionCalling { get; set; } = true;

    /// <summary>Cap on the tool-call round trips per user turn, so a loop can't run away.</summary>
    public int MaxToolIterations { get; set; } = 6;

    public bool EnableStreaming { get; set; } = true;

    public UploadOptions Uploads { get; set; } = new();

    public OpenAIOptions OpenAI { get; set; } = new();

    public AzureOpenAIOptions AzureOpenAI { get; set; } = new();

    public AnthropicOptions Anthropic { get; set; } = new();

    public GoogleOptions Google { get; set; } = new();

    public OllamaOptions Ollama { get; set; } = new();

    public TavilyOptions Tavily { get; set; } = new();

    public class OpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        /// <summary>Override for Azure OpenAI or an OpenAI-compatible gateway.</summary>
        public string? Endpoint { get; set; }
        public string? OrganizationId { get; set; }
    }

    /// <summary>
    /// Azure OpenAI is a separate provider, not an <see cref="OpenAIOptions.Endpoint"/>
    /// override: its URLs carry a deployment name and an api-version query string, so
    /// the plain OpenAI connector cannot reach it.
    /// </summary>
    public class AzureOpenAIOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>Resource root, e.g. https://myresource.openai.azure.com/</summary>
        public string Endpoint { get; set; } = string.Empty;
        /// <summary>The deployment name, which is often but not always the model name.</summary>
        public string Deployment { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
    }

    public class AnthropicOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "claude-opus-5";
        /// <summary>low | medium | high | xhigh | max — replaces temperature on current Claude models.</summary>
        public string Effort { get; set; } = "medium";
        /// <summary>Return a readable summary of the model's reasoning alongside the answer.</summary>
        public bool ShowThinking { get; set; }
    }

    public class GoogleOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.0-flash";
    }

    public class OllamaOptions
    {
        public string Endpoint { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama3.1";
    }

    public class TavilyOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = "https://api.tavily.com/search";
        public int MaxResults { get; set; } = 5;
        /// <summary>basic | advanced</summary>
        public string SearchDepth { get; set; } = "basic";
    }

    public class UploadOptions
    {
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
        public int MaxFilesPerMessage { get; set; } = 5;

        /// <summary>
        /// WebP is excluded on purpose: Anthropic's media-type enum does not carry it,
        /// so a WebP upload would fail at request time rather than at upload time.
        /// </summary>
        public string[] AllowedImageTypes { get; set; } =
            { "image/png", "image/jpeg", "image/gif" };

        public string[] AllowedDocumentTypes { get; set; } =
        {
            "application/pdf", "text/plain", "text/markdown", "text/csv",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };

        /// <summary>Path under wwwroot where chat uploads land.</summary>
        public string SubFolder { get; set; } = "uploads/chat";
    }

    /// <summary>Provider names the UI offers, in display order.</summary>
    public static readonly string[] SupportedProviders =
        { "OpenAI", "AzureOpenAI", "Anthropic", "Google", "Ollama" };

    public string ResolveModel() => Provider.ToLowerInvariant() switch
    {
        "azureopenai" => string.IsNullOrWhiteSpace(AzureOpenAI.ModelId)
            ? AzureOpenAI.Deployment
            : AzureOpenAI.ModelId,
        "anthropic" => Anthropic.Model,
        "google" => Google.Model,
        "ollama" => Ollama.Model,
        _ => OpenAI.Model
    };

    /// <summary>
    /// Whether the selected provider has enough configuration to run.
    /// Ollama needs no key — it is the offline fallback.
    /// </summary>
    public bool IsConfigured(out string reason)
    {
        switch (Provider.ToLowerInvariant())
        {
            case "azureopenai":
                reason = "Assistant:AzureOpenAI:ApiKey atau Endpoint belum diisi.";
                return !string.IsNullOrWhiteSpace(AzureOpenAI.ApiKey)
                       && !string.IsNullOrWhiteSpace(AzureOpenAI.Endpoint)
                       && !string.IsNullOrWhiteSpace(AzureOpenAI.Deployment);
            case "anthropic":
                reason = "Assistant:Anthropic:ApiKey belum diisi.";
                return !string.IsNullOrWhiteSpace(Anthropic.ApiKey);
            case "google":
                reason = "Assistant:Google:ApiKey belum diisi.";
                return !string.IsNullOrWhiteSpace(Google.ApiKey);
            case "ollama":
                reason = "Assistant:Ollama:Endpoint belum diisi.";
                return !string.IsNullOrWhiteSpace(Ollama.Endpoint);
            default:
                reason = "Assistant:OpenAI:ApiKey belum diisi.";
                return !string.IsNullOrWhiteSpace(OpenAI.ApiKey);
        }
    }
}
