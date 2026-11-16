using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FlowerShop.Services.SePay
{
    /// <summary>
    /// Ký & xác thực theo tài liệu SePay:
    ///  - Form checkout: chuỗi ký là các field được phép, ĐÚNG THỨ TỰ khai báo, dạng
    ///    "field1=value1,field2=value2"; bỏ field rỗng / 0 / "0";
    ///    chữ ký = base64(HMAC_SHA256(rawString, secretKey)).
    ///  - Webhook HMAC: chữ ký = HMAC_SHA256(rawBody, secret) (nhận cả base64 và hex).
    /// Toàn bộ là pure function để unit test không cần mạng/DB.
    /// </summary>
    public static class SePaySignature
    {
        /// <summary>
        /// Thứ tự field được ký — KHÔNG được sắp xếp lại (SePay kiểm tra đúng chuỗi này).
        /// </summary>
        public static readonly string[] SignedFieldOrder =
        {
            "order_amount",
            "merchant",
            "currency",
            "operation",
            "order_description",
            "order_invoice_number",
            "customer_id",
            "payment_method",
            "success_url",
            "error_url",
            "cancel_url"
        };

        /// <summary>Bỏ qua field rỗng, "0" hoặc 0 theo đúng quy tắc SePay.</summary>
        public static bool ShouldSign(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            return trimmed != "0";
        }

        public static string BuildSignedString(IDictionary<string, string> fields)
        {
            if (fields == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();

            foreach (string field in SignedFieldOrder)
            {
                string value;
                if (!fields.TryGetValue(field, out value) || !ShouldSign(value))
                {
                    continue;
                }

                parts.Add(field + "=" + value.Trim());
            }

            return string.Join(",", parts);
        }

        /// <summary>base64(HMAC_SHA256(signedString, secretKey)).</summary>
        public static string Sign(IDictionary<string, string> fields, string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentException("Thiếu SePay Secret Key.", "secretKey");
            }

            return ComputeHmacSha256Base64(BuildSignedString(fields), secretKey);
        }

        public static string ComputeHmacSha256Base64(string payload, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
                return Convert.ToBase64String(hash);
            }
        }

        public static string ComputeHmacSha256Hex(string payload, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload ?? string.Empty));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Xác thực chữ ký webhook HMAC-SHA256 (hỗ trợ cả base64 và hex, không phân biệt hoa thường).
        /// </summary>
        public static bool VerifyWebhookHmac(string rawBody, string providedSignature, string secret)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrWhiteSpace(providedSignature))
            {
                return false;
            }

            string provided = providedSignature.Trim();

            return FixedTimeEquals(provided, ComputeHmacSha256Base64(rawBody, secret))
                || FixedTimeEquals(provided.ToLowerInvariant(), ComputeHmacSha256Hex(rawBody, secret));
        }

        /// <summary>
        /// So sánh hằng thời gian (chống timing attack) cho API key / secret header.
        /// </summary>
        public static bool VerifyHeaderEquals(string provided, string expected)
        {
            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided))
            {
                return false;
            }

            return FixedTimeEquals(provided.Trim(), expected.Trim());
        }

        /// <summary>Lấy token từ header "Authorization: Bearer xxx".</summary>
        public static string ExtractBearerToken(string authorizationHeader)
        {
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                return string.Empty;
            }

            string value = authorizationHeader.Trim();

            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return value.Substring(7).Trim();
            }

            return value;
        }

        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            byte[] left = Encoding.UTF8.GetBytes(a);
            byte[] right = Encoding.UTF8.GetBytes(b);

            if (left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
