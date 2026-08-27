namespace MyPoS.Services
{
    /// <summary>
    /// Bagian "Storage" pada appsettings.json. Ini konfigurasi infrastruktur, bukan
    /// pengaturan usaha, jadi tempatnya di berkas konfigurasi dan bukan di halaman
    /// Pengaturan - memindahkan penyimpanan berkas saat aplikasi berjalan akan membuat
    /// berkas lama tidak lagi dapat ditemukan.
    /// </summary>
    public class StorageConfig
    {
        /// <summary>FileSystem | AzureBlob | AwsS3 | MinIO</summary>
        public string Provider { get; set; } = "FileSystem";

        /// <summary>Nama bucket (S3/MinIO) atau container (Azure Blob).</summary>
        public string BucketOrContainerName { get; set; } = "mypos-uploads";

        /// <summary>Awalan URL berkas untuk penyimpanan sistem berkas lokal.</summary>
        public string BaseUrl { get; set; } = "/uploads/";

        /// <summary>Connection string Azure Storage.</summary>
        public string ConnectionString { get; set; } = "";

        // ---------- S3 dan MinIO ----------

        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string Region { get; set; } = "ap-southeast-1";

        /// <summary>
        /// Alamat endpoint untuk penyimpanan yang kompatibel dengan S3, mis.
        /// <c>http://localhost:9000</c> untuk MinIO. Dikosongkan untuk AWS S3 asli.
        /// </summary>
        public string ServiceUrl { get; set; } = "";

        /// <summary>
        /// MinIO dan kebanyakan penyimpanan kompatibel-S3 memakai gaya alamat
        /// <c>endpoint/bucket/key</c>, bukan <c>bucket.endpoint/key</c>.
        /// </summary>
        public bool ForcePathStyle { get; set; } = true;

        /// <summary>
        /// Awalan URL publik bila bucket disajikan lewat CDN atau domain sendiri.
        /// Dikosongkan untuk memakai alamat bawaan penyedia.
        /// </summary>
        public string PublicBaseUrl { get; set; } = "";

        /// <summary>Batas ukuran unggahan. Berlaku untuk semua penyedia.</summary>
        public int MaxUploadMegabytes { get; set; } = 10;

        public long MaxUploadBytes => (long)MaxUploadMegabytes * 1024 * 1024;
    }
}
