using System;
using System.Collections.Generic;
using System.Configuration;

namespace FlowerShop.Services.Orders
{
    /// <summary>
    /// Công thức giá cho bó hoa tự thiết kế. Trước đây giá nằm hardcode ở CẢ server
    /// (CustomBouquetController) và client (custom-bouquet.js) -> sửa một bên là lệch.
    /// Nay server là nguồn duy nhất, FE đọc qua data-attribute.
    /// </summary>
    public static class BouquetBuilderService
    {
        public const string WrapBoTron = "BoTron";
        public const string WrapBoDai = "BoDai";
        public const string WrapGioHoa = "GioHoa";
        public const string WrapHopMica = "HopMica";

        public static readonly IReadOnlyList<int> AllowedStemCounts = new List<int> { 9, 19, 33, 99 };

        public static decimal WrapCostBoTron
        {
            get { return ReadDecimal("Bouquet:WrapCostBoTron", 80000m); }
        }

        public static decimal WrapCostBoDai
        {
            get { return ReadDecimal("Bouquet:WrapCostBoDai", 120000m); }
        }

        public static decimal WrapCostGioHoa
        {
            get { return ReadDecimal("Bouquet:WrapCostGioHoa", 150000m); }
        }

        public static decimal WrapCostHopMica
        {
            get { return ReadDecimal("Bouquet:WrapCostHopMica", 250000m); }
        }

        /// <summary>Đơn giá mỗi cành hoa chủ đạo (fallback khi sản phẩm không có giá).</summary>
        public static decimal DefaultStemUnitPrice
        {
            get { return ReadDecimal("Bouquet:StemUnitPrice", 25000m); }
        }

        public static decimal GetWrapCost(string wrapStyle)
        {
            if (string.IsNullOrWhiteSpace(wrapStyle))
            {
                return WrapCostBoTron;
            }

            switch (wrapStyle.Trim())
            {
                case WrapGioHoa: return WrapCostGioHoa;
                case WrapHopMica: return WrapCostHopMica;
                case WrapBoDai: return WrapCostBoDai;
                default: return WrapCostBoTron;
            }
        }

        public static bool IsValidStemCount(int stemCount)
        {
            foreach (int allowed in AllowedStemCounts)
            {
                if (allowed == stemCount)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Giá bó hoa = phí dáng cắm + (số cành × đơn giá cành).
        /// </summary>
        public static decimal CalculatePrice(string wrapStyle, int stemCount, decimal stemUnitPrice)
        {
            if (stemCount <= 0)
            {
                stemCount = 9;
            }

            if (stemUnitPrice <= 0m)
            {
                stemUnitPrice = DefaultStemUnitPrice;
            }

            return GetWrapCost(wrapStyle) + (stemUnitPrice * stemCount);
        }

        /// <summary>
        /// Đơn giá mỗi cành suy ra từ giá sản phẩm (quy đổi theo bó tham chiếu 19 cành),
        /// làm tròn nghìn để giá hiển thị gọn. Tránh việc hoa 2.950.000đ và hoa 350.000đ
        /// cùng bị tính 25.000đ/cành.
        /// </summary>
        public static decimal ResolveStemUnitPrice(decimal productUnitPrice)
        {
            if (productUnitPrice <= 0m)
            {
                return DefaultStemUnitPrice;
            }

            decimal perStem = Math.Round(productUnitPrice / 19m / 1000m, MidpointRounding.AwayFromZero) * 1000m;

            return perStem < 5000m ? DefaultStemUnitPrice : perStem;
        }

        /// <summary>Tên dòng hàng hiển thị trong giỏ/đơn (lưu snapshot, không phụ thuộc cấu hình sau này).</summary>
        public static string BuildLineName(string baseProductName, string wrapStyle, int stemCount, string paperColor)
        {
            return string.Format("[Tự Thiết Kế] {0} ({1} cành - Giấy {2})",
                baseProductName,
                stemCount,
                string.IsNullOrWhiteSpace(paperColor) ? "tuỳ chọn" : paperColor);
        }

        public static string GetWrapDisplayName(string wrapStyle)
        {
            switch (wrapStyle)
            {
                case WrapGioHoa: return "Giỏ hoa";
                case WrapHopMica: return "Hộp mica";
                case WrapBoDai: return "Bó dài";
                default: return "Bó tròn";
            }
        }

        private static decimal ReadDecimal(string key, decimal fallback)
        {
            string raw = ConfigurationManager.AppSettings[key];
            decimal value;

            if (!string.IsNullOrWhiteSpace(raw) && decimal.TryParse(raw, out value) && value >= 0m)
            {
                return value;
            }

            return fallback;
        }
    }
}
