using System;

namespace QLKS.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Mật khẩu không được để trống.", "password");
            return password;
        }

        public static bool Verify(string password, string storedValue)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedValue)) return false;
            return string.Equals(password, storedValue, StringComparison.Ordinal);
        }

        public static bool IsHash(string value)
        {
            return !string.IsNullOrEmpty(value);
        }
    }
}