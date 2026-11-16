using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowerShop.Services.Orders
{
    /// <summary>
    /// Sinh mã đơn hàng khó đoán. Bản cũ dùng <c>new Random().Next(1000, 9999)</c> nên chỉ có
    /// 9.000 khả năng/ngày và trùng nhau khi 2 request cùng mili-giây -> vừa dò được đơn người khác
    /// vừa có nguy cơ vi phạm UNIQUE(OrderCode).
    /// Định dạng: FL + yyMMdd + 5 ký tự Base32 (bỏ I, O, 0, 1) => 33.554.432 tổ hợp/ngày.
    /// </summary>
    public static class OrderCodeGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int SuffixLength = 5;
        private static readonly Regex OrderCodePattern =
            new Regex("^FL[0-9]{6}[A-HJ-NP-Z2-9]{" + SuffixLength + "}$", RegexOptions.Compiled);

        public static string Generate()
        {
            return Generate(DateTime.Now);
        }

        public static string Generate(DateTime timestamp)
        {
            return "FL" + timestamp.ToString("yyMMdd") + RandomSuffix(SuffixLength);
        }

        /// <summary>
        /// Sinh chuỗi ngẫu nhiên mật mã học (RNGCryptoServiceProvider) - không dùng Random.
        /// </summary>
        public static string RandomSuffix(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            var bytes = new byte[length];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(Alphabet[bytes[i] % Alphabet.Length]);
            }

            return sb.ToString();
        }

        public static bool IsWellFormed(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return false;
            }

            return OrderCodePattern.IsMatch(orderCode.Trim().ToUpperInvariant());
        }

        /// <summary>
        /// Chuẩn hoá mã khách nhập (bỏ khoảng trắng, viết hoa) để tra cứu ổn định.
        /// </summary>
        public static string Normalize(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return string.Empty;
            }

            return orderCode.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        }
    }
}
