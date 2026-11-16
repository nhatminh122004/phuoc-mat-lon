using System;
using System.Text;
using System.Text.RegularExpressions;

namespace FlowerShop.Helpers
{
    /// <summary>
    /// Sinh slug / chuẩn hoá chuỗi tiếng Việt. Logic thuần, không phụ thuộc DB/HTTP
    /// nên được unit test trực tiếp.
    /// </summary>
    public static class SlugHelper
    {
        private static readonly Regex NonAlphanumeric = new Regex("[^a-z0-9\\-]+", RegexOptions.Compiled);
        private static readonly Regex MultipleDash = new Regex("-{2,}", RegexOptions.Compiled);

        /// <summary>
        /// "Bó Hoa Hồng Đỏ Ecuador" -> "bo-hoa-hong-do-ecuador"
        /// </summary>
        public static string ToSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            string normalized = RemoveDiacritics(input).ToLowerInvariant();

            // "đ" không bị NFD tách được nên phải xử lý riêng.
            normalized = normalized.Replace('đ', 'd');

            normalized = NonAlphanumeric.Replace(normalized, "-");
            normalized = MultipleDash.Replace(normalized, "-");

            return normalized.Trim('-');
        }

        /// <summary>
        /// Bỏ dấu tiếng Việt nhưng giữ nguyên chữ gốc (dùng cho tìm kiếm không dấu).
        /// </summary>
        public static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            string formD = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);

            foreach (char ch in formD)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            // 'đ'/'Đ' là ký tự dựng sẵn, NFD không tách được nên phải map tay.
            string result = sb.ToString().Normalize(NormalizationForm.FormC);

            return result.Replace('đ', 'd').Replace('Đ', 'D');
        }

        /// <summary>
        /// Chuẩn hoá tông màu về 1 giá trị duy nhất để bộ lọc so khớp tuyệt đối
        /// vẫn đúng dù dữ liệu nhập tay lệch dấu/hoa thường.
        /// </summary>
        public static string NormalizeColorTone(string colorTone)
        {
            if (string.IsNullOrWhiteSpace(colorTone))
            {
                return string.Empty;
            }

            string key = RemoveDiacritics(colorTone).Trim().ToLowerInvariant();

            switch (key)
            {
                case "do":
                case "red":
                    return "Đỏ";
                case "hong":
                case "pink":
                    return "Hồng";
                case "trang":
                case "white":
                    return "Trắng";
                case "vang":
                case "yellow":
                    return "Vàng";
                case "xanh":
                case "blue":
                case "green":
                    return "Xanh";
                case "tim":
                case "purple":
                    return "Tím";
                case "cam":
                case "orange":
                    return "Cam";
                default:
                    return colorTone.Trim();
            }
        }
    }
}
