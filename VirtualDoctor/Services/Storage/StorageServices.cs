using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.Storage;

public static class StorageServiceFactory
{
    public static IFileStorageService Create(IServiceProvider sp)
    {
        var config = sp.GetRequiredService<AppConfig>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var provider = (config.Storage.Provider ?? "FileSystem").Trim().ToLowerInvariant();

        return provider switch
        {
            "s3" => new S3FileStorageService(config, loggerFactory.CreateLogger<S3FileStorageService>()),
            "minio" => new MinioFileStorageService(config, loggerFactory.CreateLogger<MinioFileStorageService>()),
            "azureblob" => new AzureBlobFileStorageService(config, loggerFactory.CreateLogger<AzureBlobFileStorageService>()),
            _ => new FileStorageService(config)
        };
    }
}

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _publicBasePath;

    public FileStorageService(AppConfig config)
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", config.Storage.BasePath);
        _publicBasePath = "/" + config.Storage.BasePath.Trim('/');
        if (!Directory.Exists(_basePath)) Directory.CreateDirectory(_basePath);
    }
    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var name = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        using var fs = File.Create(Path.Combine(_basePath, name));
        await stream.CopyToAsync(fs);
        return name;
    }
    public Task<Stream?> DownloadAsync(string path)
    {
        var fp = Path.Combine(_basePath, path);
        return Task.FromResult<Stream?>(File.Exists(fp) ? File.OpenRead(fp) : null);
    }
    public Task<bool> DeleteAsync(string path)
    {
        var fp = Path.Combine(_basePath, path);
        if (!File.Exists(fp)) return Task.FromResult(false);
        File.Delete(fp);
        return Task.FromResult(true);
    }
    public Task<string> GetPublicUrlAsync(string path) => Task.FromResult($"{_publicBasePath}/{path}");
    public Task<bool> ExistsAsync(string path) => Task.FromResult(File.Exists(Path.Combine(_basePath, path)));
    public Task<List<string>> ListFilesAsync(string prefix = "")
        => Task.FromResult(Directory.GetFiles(_basePath)
            .Select(Path.GetFileName)
            .Where(f => f != null && f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Cast<string>()
            .ToList());
}

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageConfig _cfg;
    private readonly ILogger<S3FileStorageService> _log;

    public S3FileStorageService(AppConfig config, ILogger<S3FileStorageService> log)
    {
        _cfg = config.Storage;
        _log = log;

        var creds = new BasicAWSCredentials(_cfg.AccessKey, _cfg.SecretKey);
        var s3Config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(_cfg.Endpoint))
        {
            s3Config.ServiceURL = _cfg.Endpoint;
            s3Config.ForcePathStyle = true;
        }
        else
        {
            s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(_cfg.Region ?? "us-east-1");
        }

        _s3 = new AmazonS3Client(creds, s3Config);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var req = new PutObjectRequest
        {
            BucketName = _cfg.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };
        await _s3.PutObjectAsync(req);
        return key;
    }

    public async Task<Stream?> DownloadAsync(string path)
    {
        try
        {
            var res = await _s3.GetObjectAsync(_cfg.BucketName, path);
            var ms = new MemoryStream();
            await res.ResponseStream.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string path)
    {
        await _s3.DeleteObjectAsync(_cfg.BucketName, path);
        return true;
    }

    public async Task<string> GetPublicUrlAsync(string path)
    {
        if (!string.IsNullOrWhiteSpace(_cfg.Endpoint))
            return $"{_cfg.Endpoint.TrimEnd('/')}/{_cfg.BucketName}/{path}";

        var region = _cfg.Region ?? "us-east-1";
        return $"https://{_cfg.BucketName}.s3.{region}.amazonaws.com/{path}";
    }

    public async Task<bool> ExistsAsync(string path)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_cfg.BucketName, path);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<List<string>> ListFilesAsync(string prefix = "")
    {
        var result = new List<string>();
        string? token = null;
        do
        {
            var req = new ListObjectsV2Request
            {
                BucketName = _cfg.BucketName,
                Prefix = prefix,
                ContinuationToken = token
            };
            var res = await _s3.ListObjectsV2Async(req);
            result.AddRange(res.S3Objects.Select(o => o.Key));
            token = res.IsTruncated == true ? res.NextContinuationToken : null;
        } while (token != null);

        return result;
    }
}

public class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _minio;
    private readonly StorageConfig _cfg;
    private readonly ILogger<MinioFileStorageService> _log;

    public MinioFileStorageService(AppConfig config, ILogger<MinioFileStorageService> log)
    {
        _cfg = config.Storage;
        _log = log;

        var endpoint = _cfg.Endpoint?.Replace("https://", "").Replace("http://", "") ?? "localhost:9000";
        _minio = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(_cfg.AccessKey, _cfg.SecretKey)
            .WithSSL(_cfg.Endpoint?.StartsWith("https", StringComparison.OrdinalIgnoreCase) ?? false)
            .Build();
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        var args = new PutObjectArgs()
            .WithBucket(_cfg.BucketName)
            .WithObject(key)
            .WithStreamData(ms)
            .WithObjectSize(ms.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args);
        return key;
    }

    public async Task<Stream?> DownloadAsync(string path)
    {
        try
        {
            var ms = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(_cfg.BucketName)
                .WithObject(path)
                .WithCallbackStream(s => s.CopyTo(ms));

            await _minio.GetObjectAsync(args);
            ms.Position = 0;
            return ms;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string path)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_cfg.BucketName)
            .WithObject(path);
        await _minio.RemoveObjectAsync(args);
        return true;
    }

    public Task<string> GetPublicUrlAsync(string path)
        => Task.FromResult($"{_cfg.Endpoint?.TrimEnd('/')}/{_cfg.BucketName}/{path}");

    public async Task<bool> ExistsAsync(string path)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(_cfg.BucketName)
                .WithObject(path);
            await _minio.StatObjectAsync(args);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<List<string>> ListFilesAsync(string prefix = "")
    {
        var result = new List<string>();
        var args = new ListObjectsArgs()
            .WithBucket(_cfg.BucketName)
            .WithPrefix(prefix)
            .WithRecursive(true);

        await foreach (var item in _minio.ListObjectsEnumAsync(args))
        {
            result.Add(item.Key);
        }

        return result;
    }
}

public class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly StorageConfig _cfg;
    private readonly ILogger<AzureBlobFileStorageService> _log;

    public AzureBlobFileStorageService(AppConfig config, ILogger<AzureBlobFileStorageService> log)
    {
        _cfg = config.Storage;
        _log = log;

        if (!string.IsNullOrWhiteSpace(_cfg.ConnectionString))
        {
            var service = new BlobServiceClient(_cfg.ConnectionString);
            _container = service.GetBlobContainerClient(_cfg.ContainerName);
        }
        else
        {
            var uri = new Uri(_cfg.Endpoint ?? "");
            var credential = new StorageSharedKeyCredential(_cfg.AccountName, _cfg.AccessKey);
            var service = new BlobServiceClient(uri, credential);
            _container = service.GetBlobContainerClient(_cfg.ContainerName);
        }

        _container.CreateIfNotExists();
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var key = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var blob = _container.GetBlobClient(key);
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
        return key;
    }

    public async Task<Stream?> DownloadAsync(string path)
    {
        var blob = _container.GetBlobClient(path);
        if (!await blob.ExistsAsync()) return null;
        var res = await blob.DownloadStreamingAsync();
        var ms = new MemoryStream();
        await res.Value.Content.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<bool> DeleteAsync(string path)
    {
        var blob = _container.GetBlobClient(path);
        var res = await blob.DeleteIfExistsAsync();
        return res.Value;
    }

    public Task<string> GetPublicUrlAsync(string path)
        => Task.FromResult(_container.GetBlobClient(path).Uri.ToString());

    public async Task<bool> ExistsAsync(string path)
    {
        var blob = _container.GetBlobClient(path);
        return await blob.ExistsAsync();
    }

    public async Task<List<string>> ListFilesAsync(string prefix = "")
    {
        var result = new List<string>();
        await foreach (var item in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None))
        {
            result.Add(item.Name);
        }
        return result;
    }
}

public class LocationService : ILocationService
{
    public LocationService(AppConfig config, IHttpClientFactory hf) { }
    public Task<List<(string, double, double, string)>> FindNearbyHospitalsAsync(double lat, double lng, double r = 10)
        => Task.FromResult(new List<(string, double, double, string)> { ("RS Premier", lat + 0.01, lng + 0.01, "Jl. Sudirman No.1"), ("Klinik Sehat", lat - 0.005, lng + 0.015, "Jl. Thamrin No.5"), ("Puskesmas Makmur", lat + 0.02, lng - 0.01, "Jl. Merdeka No.10") });
    public Task<double> CalculateDistanceAsync(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; var dLat = (lat2 - lat1) * Math.PI / 180; var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return Task.FromResult(R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }
    public Task<string?> GeocodeAsync(string a) => Task.FromResult<string?>(null);
    public Task<(double, double)?> ReverseGeocodeAsync(string a) => Task.FromResult<(double, double)?>((-6.2088, 106.8456));
}

public class SearchService : ISearchService
{
    private readonly AppConfig _c; private readonly HttpClient _h;
    public SearchService(AppConfig c, IHttpClientFactory hf) { _c = c; _h = hf.CreateClient("LlmClient"); }
    public async Task<string> SearchAsync(string q)
    {
        if (string.IsNullOrEmpty(_c.Search.ApiKey) || _c.Search.ApiKey == "YOUR_TAVILY_API_KEY")
            return $"[Pencarian: '{q}'] - API key belum dikonfigurasi.";
        return $"[Hasil pencarian untuk: {q}]";
    }
    public async Task<string> SearchHealthAsync(string q) => await SearchAsync($"kesehatan {q}");
}
