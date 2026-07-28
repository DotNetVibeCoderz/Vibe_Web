// Configuration models untuk appsettings
namespace VirtualDoctor.Models;

/// <summary>
/// Konfigurasi LLM untuk multi-provider AI Chat
/// </summary>
public class LlmConfig
{
    public string DefaultProvider { get; set; } = "OpenAI";
    public OpenAIConfig? OpenAI { get; set; }
    public GeminiConfig? Gemini { get; set; }
    public AnthropicConfig? Anthropic { get; set; }
    public OllamaConfig? Ollama { get; set; }
    public OpenAICompatibleConfig? OpenAICompatible { get; set; }

    // Bot personality settings
    public string SystemPrompt { get; set; } = "Kamu adalah dokter Markonah Al-senyumwati, seorang dokter yang cerdas, ramah, informatif, sopan, dan humoris. Kamu siap memberikan pelayanan informasi kesehatan yang akurat dan bermanfaat. Jawablah dengan bahasa yang mudah dipahami, gunakan analogi sederhana bila perlu, dan selalu ingatkan pasien untuk berkonsultasi langsung ke dokter untuk kondisi darurat.";
    public string BotName { get; set; } = "dokter Markonah Al-senyumwati";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
}

public class OpenAIConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.0-flash";
}

public class AnthropicConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
}

public class OllamaConfig
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.1";
}

public class OpenAICompatibleConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// Konfigurasi database
/// </summary>
public class DatabaseConfig
{
    public string Provider { get; set; } = "SQLite"; // SQLite, SqlServer, PostgreSql, MySql
    public string ConnectionString { get; set; } = "Data Source=VirtualDoctor.db";
}

/// <summary>
/// Konfigurasi Vector Database untuk RAG
/// </summary>
public class VectorDbConfig
{
    public string Provider { get; set; } = "InMemory"; // InMemory, SQLite, Qdrant, AzureAISearch, Chroma
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? CollectionName { get; set; } = "health-docs";
    public string? ConnectionString { get; set; } = "Data Source=VectorStore.db";
}

/// <summary>
/// Konfigurasi storage untuk file upload
/// </summary>
public class StorageConfig
{
    public string Provider { get; set; } = "FileSystem"; // FileSystem, MinIO, S3, AzureBlob
    public string BasePath { get; set; } = "uploads";
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public string? ConnectionString { get; set; }
    public string? AccountName { get; set; }
    public string? ContainerName { get; set; }
}

/// <summary>
/// Konfigurasi indexing worker
/// </summary>
public class IndexingConfig
{
    public string PdfFolderPath { get; set; } = "HealthPdfs";
    public int IntervalMinutes { get; set; } = 30;
    public bool AutoIndex { get; set; } = true;
}

/// <summary>
/// Konfigurasi Google Maps
/// </summary>
public class GoogleMapsConfig
{
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Konfigurasi search internet (Tavily/Perplexity)
/// </summary>
public class SearchConfig
{
    public string Provider { get; set; } = "Tavily"; // Tavily, Perplexity
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Konfigurasi video conference untuk konsultasi (opsional).
/// Provider: None, Jitsi (tanpa API key), Zoom (Server-to-Server OAuth), Teams (Microsoft Graph).
/// Bisa diubah lewat appsettings.json maupun halaman Pengaturan.
/// </summary>
public class MeetingConfig
{
    public string Provider { get; set; } = "None";
    public int DefaultDurationMinutes { get; set; } = 30;
    public ZoomConfig? Zoom { get; set; } = new();
    public TeamsConfig? Teams { get; set; } = new();
    public JitsiConfig? Jitsi { get; set; } = new();
}

/// <summary>Zoom Server-to-Server OAuth (Account ID + Client ID + Client Secret).</summary>
public class ZoomConfig
{
    public string AccountId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>User pemilik meeting. "me" = user pemilik app credential.</summary>
    public string HostUserId { get; set; } = "me";
}

/// <summary>Microsoft Teams via Graph API (client credentials).</summary>
public class TeamsConfig
{
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    /// <summary>Object ID / UPN user yang menjadi organizer meeting.</summary>
    public string OrganizerUserId { get; set; } = string.Empty;
}

/// <summary>Jitsi Meet - tanpa kredensial, cocok untuk pilot / on-premise.</summary>
public class JitsiConfig
{
    public string Domain { get; set; } = "meet.jit.si";
    public string RoomPrefix { get; set; } = "vdoctor";
}

/// <summary>
/// Konfigurasi pembayaran. Provider: Manual (transfer + unggah bukti),
/// Qris (QRIS statis milik merchant diubah jadi dinamis), Midtrans, Xendit.
/// Dapat diubah lewat appsettings.json maupun halaman Pengaturan Sistem.
/// </summary>
public class PaymentConfig
{
    public string Provider { get; set; } = "Manual";
    public bool Enabled { get; set; } = true;

    /// <summary>Batas waktu pembayaran dalam menit.</summary>
    public int ExpiryMinutes { get; set; } = 120;

    /// <summary>Biaya penanganan yang dibebankan ke pasien (rupiah, bukan persen).</summary>
    public decimal ServiceFee { get; set; } = 0;

    /// <summary>Awalan nomor invoice.</summary>
    public string InvoicePrefix { get; set; } = "INV";

    public MerchantInfo Merchant { get; set; } = new();
    public QrisConfig? Qris { get; set; } = new();
    public ManualPaymentConfig? Manual { get; set; } = new();
    public MidtransConfig? Midtrans { get; set; } = new();
    public XenditConfig? Xendit { get; set; } = new();
}

/// <summary>Identitas penerbit tagihan yang tercetak di invoice.</summary>
public class MerchantInfo
{
    public string Name { get; set; } = "VirtualDoctor";
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? TaxId { get; set; }
    public string? LogoUrl { get; set; }
}

/// <summary>
/// QRIS statis milik merchant. Payload diambil dari QR cetak yang diberikan
/// penyedia (bank/PJSP), lalu aplikasi menyisipkan nominal agar menjadi QRIS dinamis.
/// </summary>
public class QrisConfig
{
    /// <summary>Payload EMVCo dari QR statis merchant (diawali "00020101...").</summary>
    public string StaticPayload { get; set; } = string.Empty;
    public string? MerchantName { get; set; }
    public string? MerchantCity { get; set; }
}

public class ManualPaymentConfig
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Instructions { get; set; }
}

/// <summary>Midtrans Snap / Core API.</summary>
public class MidtransConfig
{
    public string ServerKey { get; set; } = string.Empty;
    public string ClientKey { get; set; } = string.Empty;
    public bool IsProduction { get; set; }
}

/// <summary>Xendit API v2.</summary>
public class XenditConfig
{
    public string SecretKey { get; set; } = string.Empty;
    public string? CallbackToken { get; set; }
}

/// <summary>
/// Root app configuration
/// </summary>
public class AppConfig
{
    public LlmConfig Llm { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public VectorDbConfig VectorDb { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
    public IndexingConfig Indexing { get; set; } = new();
    public GoogleMapsConfig GoogleMaps { get; set; } = new();
    public SearchConfig Search { get; set; } = new();
    public MeetingConfig Meeting { get; set; } = new();
    public PaymentConfig Payment { get; set; } = new();
}
