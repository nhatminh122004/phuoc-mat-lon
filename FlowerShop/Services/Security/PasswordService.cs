using System;
using System.Security.Cryptography;
using System.Text;

namespace FlowerShop.Services.Security
{
    public class PasswordValidationResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; }

        public PasswordValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }
    }

    /// <summary>
    /// Hash/verify mật khẩu tập trung ở một chỗ (trước đây logic nằm rải trong AccountController
    /// và có fallback so sánh plaintext, khiến tài khoản admin trong file dump SQL đăng nhập được
    /// bằng mật khẩu thô).
    /// </summary>
    public static class PasswordService
    {
        public const int MinLength = 8;
        public const int MaxLength = 72; // giới hạn của BCrypt
        private const int WorkFactor = 11;

        public static string Hash(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                throw new ArgumentException("Mật khẩu không được để trống.", "plainPassword");
            }

            string trimmed = plainPassword.Length > MaxLength
                ? plainPassword.Substring(0, MaxLength)
                : plainPassword;

            return BCrypt.Net.BCrypt.HashPassword(trimmed, WorkFactor);
        }

        /// <summary>
        /// Verify hỗ trợ: BCrypt ($2a/$2b/$2y), SHA-256 hex (dữ liệu cũ) và plaintext legacy.
        /// Legacy chỉ để tương thích dữ liệu cũ; <see cref="NeedsRehash"/> cho biết phải nâng cấp hash.
        /// </summary>
        public static bool Verify(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            if (IsBCryptHash(storedHash))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(
                        inputPassword.Length > MaxLength ? inputPassword.Substring(0, MaxLength) : inputPassword,
                        storedHash);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return false;
                }
            }

            if (IsSha256Hex(storedHash))
            {
                return string.Equals(storedHash, Sha256Hex(inputPassword), StringComparison.OrdinalIgnoreCase);
            }

            // Legacy plaintext (chỉ dữ liệu seed cũ). So sánh hằng thời gian.
            return FixedTimeEquals(storedHash, inputPassword);
        }

        public static bool NeedsRehash(string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
            {
                return true;
            }

            if (!IsBCryptHash(storedHash))
            {
                return true;
            }

            try
            {
                int workFactor = BCrypt.Net.BCrypt.PasswordNeedsRehash(storedHash, WorkFactor) ? 0 : 1;
                return workFactor == 0;
            }
            catch
            {
                return true;
            }
        }

        public static bool IsBCryptHash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 7)
            {
                return false;
            }

            return value.StartsWith("$2a$", StringComparison.Ordinal)
                || value.StartsWith("$2b$", StringComparison.Ordinal)
                || value.StartsWith("$2y$", StringComparison.Ordinal);
        }

        public static bool IsSha256Hex(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
            {
                return false;
            }

            foreach (char c in value)
            {
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        public static string Sha256Hex(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Chính sách mật khẩu: >= 8 ký tự, có chữ và số. Trả message tiếng Việt cho view.
        /// </summary>
        public static PasswordValidationResult Validate(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return new PasswordValidationResult(false, "Vui lòng nhập mật khẩu.");
            }

            if (password.Length < MinLength)
            {
                return new PasswordValidationResult(false,
                    string.Format("Mật khẩu phải có ít nhất {0} ký tự.", MinLength));
            }

            if (password.Length > MaxLength)
            {
                return new PasswordValidationResult(false,
                    string.Format("Mật khẩu không được vượt quá {0} ký tự.", MaxLength));
            }

            bool hasLetter = false;
            bool hasDigit = false;
            foreach (char c in password)
            {
                if (char.IsLetter(c)) hasLetter = true;
                if (char.IsDigit(c)) hasDigit = true;
            }

            if (!hasLetter || !hasDigit)
            {
                return new PasswordValidationResult(false, "Mật khẩu phải bao gồm cả chữ và số.");
            }

            if (confirmPassword != null && !string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                return new PasswordValidationResult(false, "Mật khẩu xác nhận không trùng khớp.");
            }

            return new PasswordValidationResult(true, string.Empty);
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            byte[] ba = Encoding.UTF8.GetBytes(a);
            byte[] bb = Encoding.UTF8.GetBytes(b);

            if (ba.Length != bb.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < ba.Length; i++)
            {
                diff |= ba[i] ^ bb[i];
            }

            return diff == 0;
        }
    }
}
