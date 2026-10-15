
using System;
using System.Security.Cryptography;
using System.Text;
using Armelle.Data;
using Armelle.Models;

namespace Armelle.Services
{
    public static class PasswordSecurity
    {
        private const int Iterations = 210000;

        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException(
                    "Password cannot be empty.",
                    nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                32);

            return $"pbkdf2-sha256${Iterations}$" +
                   $"{Convert.ToBase64String(salt)}$" +
                   $"{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyAndUpgrade(
            AppUser user,
            string password,
            ClinicDb db)
        {
            if (user == null ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(user.Password))
            {
                return false;
            }

            if (user.Password.StartsWith(
                "pbkdf2-sha256$",
                StringComparison.Ordinal))
            {
                string[] parts = user.Password.Split('$');

                if (parts.Length != 4 ||
                    !int.TryParse(parts[1], out int iterations) ||
                    iterations < 100000 ||
                    iterations > 1000000)
                {
                    return false;
                }

                try
                {
                    byte[] salt =
                        Convert.FromBase64String(parts[2]);

                    byte[] expected =
                        Convert.FromBase64String(parts[3]);

                    if (expected.Length != 32 ||
                        salt.Length < 16)
                    {
                        return false;
                    }

                    byte[] actual =
                        Rfc2898DeriveBytes.Pbkdf2(
                            password,
                            salt,
                            iterations,
                            HashAlgorithmName.SHA256,
                            expected.Length);

                    return CryptographicOperations
                        .FixedTimeEquals(expected, actual);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            // Legacy plaintext password migration.
            // A successful login upgrades it to PBKDF2.
            byte[] stored =
                Encoding.UTF8.GetBytes(user.Password);

            byte[] entered =
                Encoding.UTF8.GetBytes(password);

            if (!CryptographicOperations.FixedTimeEquals(
                stored, entered))
            {
                return false;
            }

            user.Password = Hash(password);
            db.SaveChanges();

            return true;
        }
    }
}
