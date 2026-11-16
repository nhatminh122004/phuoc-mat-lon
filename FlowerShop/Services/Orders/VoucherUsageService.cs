using System;
using FlowerShop.DAL;

namespace FlowerShop.Services.Orders
{
    /// <summary>
    /// Quản lý lượt dùng voucher. Bản cũ tăng UsedCount ngay khi tạo đơn - kể cả khi
    /// khách thanh toán thất bại, khiến voucher bị "đốt" oan.
    /// Quy tắc hiện tại:
    ///   - COD: coi như cam kết mua -> trừ lượt ngay khi đặt.
    ///   - Cổng SePay: chỉ trừ khi thanh toán thành công.
    /// </summary>
    public static class VoucherUsageService
    {
        public const string Cod = "COD";

        public static bool ShouldConsumeOnPlacement(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return false;
            }

            return string.Equals(paymentMethod.Trim(), Cod, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Kiểm tra phương thức thanh toán có phải cổng online cần chờ xác nhận (hiện chỉ SePay).</summary>
        public static bool IsOnlineGateway(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                return false;
            }

            return string.Equals(paymentMethod.Trim(), "SEPAY", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Trừ lượt voucher nguyên tử (không vượt UsageLimit). An toàn khi gọi trùng
        /// nhờ kiểm tra cờ đã xử lý ở caller (PaymentTransaction unique + order.PaymentStatus).
        /// </summary>
        public static bool TryConsume(DbContextAdapter db, string code)
        {
            if (db == null || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            return db.TryConsumeVoucher(code, 0);
        }
    }
}
