namespace FamilyTree.Services;

public class StorageSettings
{
    public string Provider { get; set; } = "FileSystem";
    public string UploadRoot { get; set; } = "wwwroot/uploads";
    public AzureBlobSettings AzureBlob { get; set; } = new();
}

public class AzureBlobSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "familytree";
}
