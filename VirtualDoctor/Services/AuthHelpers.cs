using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace VirtualDoctor.Services;

/// <summary>
/// Helper autentikasi untuk normalisasi email dan hashing password.
/// </summary>
public static class AuthHelpers
{
    private const string PasswordSalt = "VirtualDoctorSalt";

    public static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + PasswordSalt));
        return Convert.ToHexString(bytes);
    }
}
