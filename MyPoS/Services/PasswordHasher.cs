using System;
using System.Security.Cryptography;
using System.Text;

namespace MyPoS.Services
{
    /// <summary>
    /// PBKDF2-SHA256 dengan salt acak per pengguna, disimpan sebagai
    /// <c>pbkdf2.sha256.{iterasi}.{saltBase64}.{hashBase64}</c>.
    ///
    /// Versi sebelumnya menyimpan <c>Convert.ToBase64String(password)</c>, yang bukan hash
    /// sama sekali - siapa pun yang membaca file database bisa langsung membaliknya menjadi
    /// kata sandi asli. <see cref="Verify"/> masih menerima format lama supaya database yang
    /// sudah ada tetap bisa login, dan <see cref="NeedsUpgrade"/> menandai baris yang perlu
    /// ditulis ulang saat pengguna berhasil masuk.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100_000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const string Prefix = "pbkdf2.sha256";

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"{Prefix}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            if (storedHash.StartsWith(Prefix + ".", StringComparison.Ordinal))
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 5) return false;
                if (!int.TryParse(parts[2], out var iterations)) return false;

                try
                {
                    var salt = Convert.FromBase64String(parts[3]);
                    var expected = Convert.FromBase64String(parts[4]);
                    var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            return VerifyLegacyBase64(password, storedHash);
        }

        /// <summary>true bila baris ini masih memakai penyandian lama dan sebaiknya di-hash ulang.</summary>
        public static bool NeedsUpgrade(string storedHash)
            => !storedHash.StartsWith(Prefix + ".", StringComparison.Ordinal);

        private static bool VerifyLegacyBase64(string password, string storedHash)
        {
            try
            {
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(encoded),
                    Encoding.UTF8.GetBytes(storedHash));
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
