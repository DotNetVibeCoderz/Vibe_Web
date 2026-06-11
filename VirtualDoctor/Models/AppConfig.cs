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
}
