using System;
using System.Security.Cryptography;
using System.Text;

namespace FlowerShop.Services.Security
{
    /// <summary>
    /// Sinh & băm token reset mật khẩu. Pure logic, không đụng DB/HTTP.
    /// Token chỉ được trả về 1 lần cho người dùng; DB chỉ lưu bản băm SHA-256
    /// nên nếu dump DB bị lộ thì kẻ tấn công cũng không dùng lại được token.
    /// </summary>
    public static class PasswordResetTokenFactory
    {
        public const int TokenByteLength = 32;
        public const int ExpiryMinutes = 30;

        /// <summary>
        /// Sinh token dạng base64url (an toàn cho URL). 32 byte = 256 bit entropy.
        /// </summary>
        public static string CreateToken()
        {
            var bytes = new byte[TokenByteLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return ToBase64Url(bytes);
        }

        public static string HashToken(string rawToken)
        {
            if (string.IsNullOrEmpty(rawToken))
            {
                return string.Empty;
            }

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken.Trim()));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static DateTime GetExpiry(DateTime issuedAt)
        {
            return issuedAt.AddMinutes(ExpiryMinutes);
        }

        public static bool IsExpired(DateTime expiresAt, DateTime now)
        {
            return now > expiresAt;
        }

        /// <summary>
        /// Token phải đúng độ dài sinh ra (chống payload rác) trước khi truy vấn DB.
        /// </summary>
        public static bool IsWellFormed(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return false;
            }

            string token = rawToken.Trim();
            int expectedLength = ToBase64Url(new byte[TokenByteLength]).Length;

            if (token.Length != expectedLength)
            {
                return false;
            }

            foreach (char c in token)
            {
                bool allowed = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                               (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!allowed)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
