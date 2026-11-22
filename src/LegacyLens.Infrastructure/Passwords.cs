using System;
using System.Security.Cryptography;

namespace LegacyLens.Infrastructure
{
    public static class Passwords
    {
        public static string Hash(string password)
        {
            if (password == null || password.Length < 12 || password.Length > 128)
                throw new ArgumentException("密碼長度必須為 12～128 個字元。");
            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                return "100000:" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(derive.GetBytes(32));
        }
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || password.Length > 128 || string.IsNullOrEmpty(stored)) return false;
            var parts = stored.Split(':');
            if (parts.Length != 3 || parts[0] != "100000") return false;
            try
            {
                var salt = Convert.FromBase64String(parts[1]);
                var expected = Convert.FromBase64String(parts[2]);
                if (salt.Length != 16 || expected.Length != 32) return false;
                using (var derive = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    var actual = derive.GetBytes(32);
                    var difference = 0;
                    for (var i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
                    return difference == 0;
                }
            }
            catch (FormatException) { return false; }
        }
    }
}
