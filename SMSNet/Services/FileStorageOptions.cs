namespace SMSNet.Services;

public class FileStorageOptions
{
    public string Provider { get; set; } = "FileSystem";
    public string BasePath { get; set; } = "wwwroot/uploads";
}
