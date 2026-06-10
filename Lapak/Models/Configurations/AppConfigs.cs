namespace Lapak.Models.Configurations;

// ============================================
// AI Configuration
// ============================================
public class AiProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
    public int TimeoutSeconds { get; set; } = 60;
}

public class AiConfig
{
    public string DefaultProvider { get; set; } = "OpenAI";
    public bool FallbackEnabled { get; set; } = true;
    public string EmbeddingProvider { get; set; } = "OpenAI";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public Dictionary<string, AiProviderConfig> Providers { get; set; } = new();
    public Dictionary<string, ChatBotConfig> ChatBots { get; set; } = new();
}

public class ChatBotConfig
{
    public string Name { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
}

// ============================================
// Vector Database Configuration
// ============================================
public class VectorDatabaseConfig
{
    public string Provider { get; set; } = "InMemory";
    public string DocumentFolderPath { get; set; } = "Documents";
    public int ReindexIntervalMinutes { get; set; } = 30;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    public VectorDbInMemoryConfig InMemory { get; set; } = new();
    public VectorDbSqliteConfig Sqlite { get; set; } = new();
    public VectorDbPostgreSqlConfig PostgreSql { get; set; } = new();
    public VectorDbQdrantConfig Qdrant { get; set; } = new();
    public VectorDbFilesystemConfig Filesystem { get; set; } = new();
}

public class VectorDbInMemoryConfig { }

public class VectorDbSqliteConfig
{
    public string ConnectionString { get; set; } = "Data Source=lapak_vectors.db";
}

public class VectorDbPostgreSqlConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class VectorDbQdrantConfig
{
    public string Endpoint { get; set; } = "http://localhost:6333";
    public string ApiKey { get; set; } = string.Empty;
}

public class VectorDbFilesystemConfig
{
    public string Path { get; set; } = "vector_store";
}

// ============================================
// Storage Configuration
// ============================================
public class StorageConfig
{
    public string Provider { get; set; } = "FileSystem";
    public FileSystemStorageConfig FileSystem { get; set; } = new();
    public MinioStorageConfig MinIO { get; set; } = new();
    public S3StorageConfig AmazonS3 { get; set; } = new();
    public AzureBlobStorageConfig AzureBlob { get; set; } = new();
}

public class FileSystemStorageConfig
{
    public string RootPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";
}

public class MinioStorageConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;
}

public class S3StorageConfig
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "ap-southeast-1";
}

public class AzureBlobStorageConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}

// ============================================
// Payment Configuration
// ============================================
public class PaymentGatewayConfig
{
    public string DefaultGateway { get; set; } = "Midtrans";
    public MidtransConfig Midtrans { get; set; } = new();
    public XenditConfig Xendit { get; set; } = new();
}

public class MidtransConfig
{
    public string ServerKey { get; set; } = string.Empty;
    public string ClientKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.midtrans.com/v2";
    public string SnapBaseUrl { get; set; } = "https://app.midtrans.com/snap/snap.js";
    public bool IsProduction { get; set; } = false;
    public string CallbackUrl { get; set; } = string.Empty;
}

public class XenditConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string CallbackToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.xendit.co";
    public bool IsProduction { get; set; } = false;
    public string CallbackUrl { get; set; } = string.Empty;
}

// ============================================
// Shipping Configuration
// ============================================
public class ShippingConfig
{
    public List<string> Couriers { get; set; } = new();
    public ShippingOriginConfig DefaultOrigin { get; set; } = new();
    public RajaOngkirConfig RajaOngkir { get; set; } = new();
    public int TrackingPollIntervalMinutes { get; set; } = 15;
    public bool AutoTrackingEnabled { get; set; } = true;
}

public class ShippingOriginConfig
{
    public string City { get; set; } = "Jakarta Pusat";
    public string CityId { get; set; } = "152";
    public string Province { get; set; } = "DKI Jakarta";
    public string ProvinceId { get; set; } = "6";
}

public class RajaOngkirConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

// ============================================
// Other Configurations
// ============================================
public class CustomerScoringConfig
{
    public int BronzeThreshold { get; set; } = 0;
    public int SilverThreshold { get; set; } = 100;
    public int GoldThreshold { get; set; } = 500;
    public int PlatinumThreshold { get; set; } = 1000;
    public double TransactionCountWeight { get; set; } = 0.3;
    public double TransactionValueWeight { get; set; } = 0.5;
    public double CategoryDiversityWeight { get; set; } = 0.2;
}

public class RecommendationConfig
{
    public double CollaborativeFilteringWeight { get; set; } = 0.6;
    public double ContentBasedWeight { get; set; } = 0.4;
    public int MaxRecommendations { get; set; } = 20;
    public double MinSimilarityScore { get; set; } = 0.3;
}

public class EmailConfig
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Lapak";
}
