using System.Security.Cryptography;
using Planova.Application.Common.Interfaces;

namespace Planova.Infrastructure.Security
{
    public sealed class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100_000;
        private const char Delimiter = ';';

        public string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var hash = GetPbkdf2Bytes(password, salt, Iterations, KeySize);

            return $"{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}{Delimiter}{Iterations}";
        }

        public bool Verify(string password, string hash)
        {
            var parts = hash.Split(Delimiter);
            if (parts.Length != 3)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hashToCompare = Convert.FromBase64String(parts[1]);
            var iterations = int.Parse(parts[2]);

            var computedHash = GetPbkdf2Bytes(password, salt, iterations, hashToCompare.Length);

            return CryptographicOperations.FixedTimeEquals(computedHash, hashToCompare);
        }

        private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int length)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(length);
        }
    }
}