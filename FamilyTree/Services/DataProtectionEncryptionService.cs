using Microsoft.AspNetCore.DataProtection;

namespace FamilyTree.Services;

public class DataProtectionEncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public DataProtectionEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("FamilyTree.NotesProtector");
    }

    public string Encrypt(string plainText)
    {
        return string.IsNullOrWhiteSpace(plainText) ? string.Empty : _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return string.IsNullOrWhiteSpace(cipherText) ? string.Empty : _protector.Unprotect(cipherText);
    }
}
