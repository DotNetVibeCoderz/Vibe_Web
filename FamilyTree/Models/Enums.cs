namespace FamilyTree.Models;

public enum RelationshipType
{
    Parent,
    Child,
    Sibling,
    Spouse,
    Cousin,
    Grandparent,
    Grandchild,
    Aunt,
    Uncle
}

public enum MediaType
{
    Photo,
    Video,
    Document
}

public enum StorageProvider
{
    FileSystem,
    AzureBlob,
    AwsS3
}
