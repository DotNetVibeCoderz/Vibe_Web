namespace MyPoS.Services
{
    public class StorageConfig
    {
        public string Provider { get; set; } = "FileSystem";
        public string ConnectionString { get; set; } = "";
        public string BucketOrContainerName { get; set; } = "mypos-uploads";
        public string BaseUrl { get; set; } = "/uploads/";
    }
}
