using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyPoS.Services
{
    /// <summary>
    /// Penyimpanan objek yang kompatibel dengan S3. Satu implementasi melayani dua penyedia
    /// karena protokolnya memang sama:
    ///
    /// - <b>AWS S3</b> — <c>ServiceUrl</c> dikosongkan, wilayah diambil dari <c>Region</c>.
    /// - <b>MinIO</b> — <c>ServiceUrl</c> diisi alamat endpoint dan <c>ForcePathStyle</c>
    ///   dibiarkan aktif, karena MinIO memakai alamat gaya <c>endpoint/bucket/key</c>.
    ///
    /// Bila <c>AccessKey</c> dikosongkan, kredensial diambil dari rantai bawaan AWS SDK
    /// (variabel lingkungan, berkas profil, atau IAM role) - cara yang dianjurkan di produksi.
    /// </summary>
    public class S3StorageService : IStorageService
    {
        private readonly StorageConfig _config;
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IOptions<StorageConfig> config, ILogger<S3StorageService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(IBrowserFile file, string fileName)
        {
            if (string.IsNullOrWhiteSpace(_config.BucketOrContainerName))
                throw new InvalidOperationException("Nama bucket belum diisi pada konfigurasi Storage.");

            var key = $"{Guid.NewGuid()}_{SanitiseFileName(fileName)}";

            // Isi berkas disalin ke memori lebih dulu karena S3 memerlukan panjang konten,
            // sedangkan aliran dari peramban tidak dapat dicari posisinya.
            using var buffer = new MemoryStream();
            await file.OpenReadStream(_config.MaxUploadBytes).CopyToAsync(buffer);
            buffer.Position = 0;

            using var client = CreateClient();
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _config.BucketOrContainerName,
                Key = key,
                InputStream = buffer,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                DisablePayloadSigning = !string.IsNullOrWhiteSpace(_config.ServiceUrl)
            });

            return BuildPublicUrl(key);
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl) || string.IsNullOrWhiteSpace(_config.BucketOrContainerName))
                return;

            try
            {
                var key = ExtractKey(fileUrl);
                if (string.IsNullOrWhiteSpace(key)) return;

                using var client = CreateClient();
                await client.DeleteObjectAsync(_config.BucketOrContainerName, key);
            }
            catch (Exception ex)
            {
                // Berkas yatim jauh lebih ringan akibatnya daripada penyimpanan yang gagal
                // hanya karena objeknya sudah tidak ada.
                _logger.LogWarning(ex, "Gagal menghapus objek {Url} dari penyimpanan.", fileUrl);
            }
        }

        private AmazonS3Client CreateClient()
        {
            var s3Config = new AmazonS3Config
            {
                ForcePathStyle = _config.ForcePathStyle
            };

            if (!string.IsNullOrWhiteSpace(_config.ServiceUrl))
            {
                // MinIO atau penyimpanan kompatibel lain.
                s3Config.ServiceURL = _config.ServiceUrl.TrimEnd('/');
                s3Config.AuthenticationRegion = _config.Region;
            }
            else
            {
                s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(_config.Region);
            }

            if (!string.IsNullOrWhiteSpace(_config.AccessKey))
            {
                return new AmazonS3Client(
                    new BasicAWSCredentials(_config.AccessKey, _config.SecretKey),
                    s3Config);
            }

            return new AmazonS3Client(s3Config);
        }

        private string BuildPublicUrl(string key)
        {
            if (!string.IsNullOrWhiteSpace(_config.PublicBaseUrl))
                return $"{_config.PublicBaseUrl.TrimEnd('/')}/{key}";

            if (!string.IsNullOrWhiteSpace(_config.ServiceUrl))
                return $"{_config.ServiceUrl.TrimEnd('/')}/{_config.BucketOrContainerName}/{key}";

            return $"https://{_config.BucketOrContainerName}.s3.{_config.Region}.amazonaws.com/{key}";
        }

        /// <summary>Mengambil kembali kunci objek dari URL yang pernah dihasilkan.</summary>
        private string? ExtractKey(string fileUrl)
        {
            if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri))
                return Path.GetFileName(fileUrl);

            var path = uri.AbsolutePath.TrimStart('/');
            var bucketPrefix = _config.BucketOrContainerName + "/";

            // Alamat gaya path menyertakan nama bucket di depan kunci.
            return path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase)
                ? path[bucketPrefix.Length..]
                : path;
        }

        private static string SanitiseFileName(string fileName)
        {
            var name = Path.GetFileName(fileName);
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return name.Replace(' ', '_');
        }
    }
}
