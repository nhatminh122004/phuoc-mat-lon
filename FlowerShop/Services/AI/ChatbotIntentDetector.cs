using System;
using System.Text.RegularExpressions;

namespace FlowerShop.Services.AI
{
    public enum ChatIntent
    {
        Unknown = 0,
        TrackOrder = 1,
        WriteCard = 2,
        RecommendBirthday = 3,
        RecommendLove = 4,
        RecommendMother = 5,
        RecommendGrandOpening = 6,
        RecommendBudget = 7
    }

    /// <summary>
    /// Bộ nhận diện ý định của chatbot, tách khỏi ChatbotService (vốn phụ thuộc DbContext)
    /// để kiểm thử được bằng unit test và để sau này thay bằng LLM thật mà không phá vỡ logic.
    /// </summary>
    public static class ChatbotIntentDetector
    {
        private static readonly Regex OrderCodeRegex =
            new Regex(@"(FL[0-9A-Z]{6,20}|[0-9]{8,15})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PhoneRegex =
            new Regex(@"(0|\+84)(\d[\s\.\-]?){8,10}\d", RegexOptions.Compiled);

        public static ChatIntent Detect(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return ChatIntent.Unknown;
            }

            string m = message.Trim().ToLowerInvariant();

            if (m.Contains("đơn hàng") || m.Contains("tra cứu") || m.Contains("vận đơn") ||
                m.Contains("kiểm tra đơn") || OrderCodeRegex.IsMatch(message))
            {
                return ChatIntent.TrackOrder;
            }

            if (m.Contains("lời chúc") || m.Contains("viết thiệp") || m.Contains("câu chúc") || m.Contains("thơ"))
            {
                return ChatIntent.WriteCard;
            }

            if (m.Contains("dưới 500") || m.Contains("500k") || m.Contains("rẻ") || m.Contains("tiết kiệm") ||
                m.Contains("ngân sách"))
            {
                return ChatIntent.RecommendBudget;
            }

            if (m.Contains("sinh nhật") || m.Contains("birthday"))
            {
                return ChatIntent.RecommendBirthday;
            }

            if (m.Contains("người yêu") || m.Contains("bạn gái") || m.Contains("tình yêu") ||
                m.Contains("valentine") || m.Contains("vợ") || m.Contains("người thương"))
            {
                return ChatIntent.RecommendLove;
            }

            if (m.Contains("mẹ") || m.Contains("tri ân") || m.Contains("cô giáo") || m.Contains("thầy cô"))
            {
                return ChatIntent.RecommendMother;
            }

            if (m.Contains("khai trương") || m.Contains("chúc mừng") || m.Contains("đối tác") || m.Contains("sếp"))
            {
                return ChatIntent.RecommendGrandOpening;
            }

            return ChatIntent.Unknown;
        }

        /// <summary>Trích mã đơn hàng người dùng gõ (nếu có).</summary>
        public static string ExtractOrderCode(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var match = OrderCodeRegex.Match(message);
            return match.Success ? match.Value.ToUpperInvariant() : string.Empty;
        }

        /// <summary>Trích & chuẩn hoá số điện thoại Việt Nam (luôn trả về dạng 0xxxxxxxxx).</summary>
        public static string ExtractPhone(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var match = PhoneRegex.Match(message);
            if (!match.Success)
            {
                return string.Empty;
            }

            return NormalizePhone(match.Value);
        }

        /// <summary>
        /// Chuẩn hoá số điện thoại về 10 số dạng 0xxxxxxxxx để so khớp DB
        /// (dữ liệu nhập tay thường có khoảng trắng, dấu chấm, +84).
        /// </summary>
        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return string.Empty;
            }

            string digits = Regex.Replace(phone.Trim(), @"[^\d]", string.Empty);

            if (digits.StartsWith("84") && digits.Length >= 11)
            {
                digits = "0" + digits.Substring(2);
            }
            else if (digits.StartsWith("0") == false && digits.Length == 9)
            {
                digits = "0" + digits;
            }

            return digits;
        }
    }
}
