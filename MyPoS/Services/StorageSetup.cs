using System;

namespace MyPoS.Services
{
    public static class StorageSetup
    {
        /// <summary>
        /// Memilih implementasi <see cref="IStorageService"/> sekali saat startup.
        /// AwsS3 dan MinIO memakai kelas yang sama karena protokolnya identik; yang
        /// membedakan hanyalah <c>ServiceUrl</c> dan gaya penulisan alamat.
        /// </summary>
        public static IServiceCollection AddMyPosStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection("Storage");
            services.Configure<StorageConfig>(section);

            var provider = section.GetValue<string>("Provider") ?? "FileSystem";

            switch (provider.Trim().ToLowerInvariant())
            {
                case "azureblob":
                case "azure":
                    services.AddScoped<IStorageService, AzureBlobStorageService>();
                    break;

                case "awss3":
                case "s3":
                case "minio":
                    services.AddScoped<IStorageService, S3StorageService>();
                    break;

                default:
                    services.AddScoped<IStorageService, FileSystemStorageService>();
                    break;
            }

            return services;
        }
    }
}
