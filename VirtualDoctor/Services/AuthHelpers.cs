using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

/// <summary>Hasil pemeriksaan kata sandi.</summary>
public enum PasswordCheck
{
    /// <summary>Sandi salah.</summary>
    Failed,
    /// <summary>Sandi benar dan hash sudah memakai algoritma terbaru.</summary>
    Success,
    /// <summary>Sandi benar tetapi hash masih format lama — tulis ulang setelah login.</summary>
    SuccessNeedsRehash
}

/// <summary>
/// Helper autentikasi: normalisasi email dan hashing kata sandi.
///
/// Hash baru memakai <see cref="PasswordHasher{TUser}"/> bawaan ASP.NET Core
/// (PBKDF2-HMAC-SHA256, salt acak per kata sandi). Hash lama berupa SHA-256 tanpa salt
/// masih bisa diverifikasi agar akun lama tetap dapat login, lalu ditulis ulang
/// dengan algoritma baru pada login berikutnya.
/// </summary>
public static class AuthHelpers
{
    private const string LegacySalt = "VirtualDoctorSalt";
    private static readonly PasswordHasher<ApplicationUser> Hasher = new();
    private static readonly ApplicationUser HashSubject = new(); // tidak dipakai algoritma, hanya syarat API

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

    /// <summary>Buat hash baru untuk disimpan.</summary>
    public static string HashPassword(string password) => Hasher.HashPassword(HashSubject, password);

    /// <summary>Periksa kata sandi terhadap hash tersimpan, apa pun formatnya.</summary>
    public static PasswordCheck VerifyPassword(string? storedHash, string? password)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(password))
            return PasswordCheck.Failed;

        if (IsLegacyHash(storedHash))
        {
            // Perbandingan waktu tetap agar tidak membocorkan informasi lewat durasi.
            var candidate = LegacyHash(password);
            var match = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(candidate),
                Encoding.ASCII.GetBytes(storedHash));
            return match ? PasswordCheck.SuccessNeedsRehash : PasswordCheck.Failed;
        }

        return Hasher.VerifyHashedPassword(HashSubject, storedHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessNeedsRehash,
            _ => PasswordCheck.Failed
        };
    }

    /// <summary>Hash lama: SHA-256 tanpa salt, keluaran 64 karakter heksadesimal.</summary>
    private static bool IsLegacyHash(string hash) =>
        hash.Length == 64 && hash.All(Uri.IsHexDigit);

    private static string LegacyHash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + LegacySalt));
        return Convert.ToHexString(bytes);
    }

    /// <summary>Aturan minimal kata sandi. Mengembalikan null bila lolos.</summary>
    public static string? ValidatePasswordRules(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return "Kata sandi wajib diisi.";
        if (password.Length < 8) return "Kata sandi minimal 8 karakter.";
        if (!password.Any(char.IsLetter)) return "Kata sandi harus memuat huruf.";
        if (!password.Any(char.IsDigit)) return "Kata sandi harus memuat angka.";
        return null;
    }
}
