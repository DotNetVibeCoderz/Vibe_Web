using System.Security.Cryptography;
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
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return string.Empty;
        }

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch (CryptographicException)
        {
            // Jika payload tidak valid (misal karena kunci lama), anggap data sudah plain text.
            return cipherText;
        }
        catch (FormatException)
        {
            // Handle data yang formatnya tidak sesuai (bukan hasil protect).
            return cipherText;
        }
    }
}
