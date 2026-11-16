using System;
using System.Collections.Generic;
using FlowerShop.Services.Pricing;

namespace FlowerShop.Services.Email
{
    /// <summary>
    /// Template email. Tách khỏi service gửi để unit test được nội dung
    /// (link reset phải chứa token, không được rò thông tin nhạy cảm).
    /// </summary>
    public static class EmailTemplates
    {
        public static string PasswordResetSubject()
        {
            return "Đặt lại mật khẩu - Hữu Phước Florist";
        }

        public static string PasswordResetBody(string customerName, string resetUrl, DateTime expiresAt)
        {
            string name = string.IsNullOrWhiteSpace(customerName) ? "bạn" : customerName.Trim();

            return string.Format(
@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1f2937;"">
  <h2 style=""color:#0ea5e9;margin:0 0 16px;"">Hữu Phước Florist</h2>
  <p>Xin chào <strong>{0}</strong>,</p>
  <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Bấm nút bên dưới để tạo mật khẩu mới:</p>
  <p style=""text-align:center;margin:28px 0;"">
    <a href=""{1}"" style=""background:#0ea5e9;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;"">Đặt lại mật khẩu</a>
  </p>
  <p style=""font-size:13px;color:#6b7280;"">Link chỉ sử dụng được một lần và hết hạn lúc <strong>{2:HH:mm dd/MM/yyyy}</strong>.</p>
  <p style=""font-size:13px;color:#6b7280;"">Nếu không phải bạn yêu cầu, hãy bỏ qua email này - mật khẩu hiện tại vẫn an toàn.</p>
  <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0;"" />
  <p style=""font-size:12px;color:#9ca3af;"">Email tự động, vui lòng không trả lời. Hotline: 0377 415 147</p>
</div>",
                WebUtilitySafe(name),
                resetUrl,
                expiresAt);
        }

        public static string OrderConfirmationSubject(string orderCode)
        {
            return string.Format("[Hữu Phước Florist] Đã nhận đơn hoa #{0}", orderCode);
        }

        public static string OrderConfirmationBody(string customerName, string orderCode, decimal totalAmount,
            string recipientName, DateTime deliveryDate, string deliverySlot, string trackingUrl)
        {
            string name = string.IsNullOrWhiteSpace(customerName) ? "Quý khách" : customerName.Trim();

            return string.Format(
@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1f2937;"">
  <h2 style=""color:#0ea5e9;margin:0 0 16px;"">Đơn hoa đã được ghi nhận 🌸</h2>
  <p>Cảm ơn <strong>{0}</strong> đã tin tưởng Hữu Phước Florist.</p>
  <table style=""width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;"">
    <tr><td style=""padding:6px 0;color:#6b7280;"">Mã đơn</td><td style=""padding:6px 0;font-weight:700;"">{1}</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280;"">Người nhận</td><td style=""padding:6px 0;"">{2}</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280;"">Thời gian giao</td><td style=""padding:6px 0;"">{3:dd/MM/yyyy} ({4})</td></tr>
    <tr><td style=""padding:6px 0;color:#6b7280;"">Tổng thanh toán</td><td style=""padding:6px 0;font-weight:700;color:#0ea5e9;"">{5:N0} đ</td></tr>
  </table>
  <p>Bạn có thể theo dõi tiến độ cắm hoa và duyệt ảnh thực tế tại:</p>
  <p><a href=""{6}"" style=""color:#0ea5e9;"">{6}</a></p>
  <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0;"" />
  <p style=""font-size:12px;color:#9ca3af;"">Hotline 0377 415 147 - Giao hỏa tốc 60-120 phút nội thành.</p>
</div>",
                WebUtilitySafe(name), orderCode, WebUtilitySafe(recipientName), deliveryDate,
                WebUtilitySafe(deliverySlot), totalAmount, trackingUrl);
        }

        public static string OrderStatusChangedSubject(string orderCode, string statusName)
        {
            return string.Format("[Hữu Phước Florist] Đơn {0}: {1}", orderCode, statusName);
        }

        public static string OrderStatusChangedBody(string customerName, string orderCode, string statusName,
            string note, string trackingUrl)
        {
            string name = string.IsNullOrWhiteSpace(customerName) ? "Quý khách" : customerName.Trim();

            return string.Format(
@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1f2937;"">
  <p>Xin chào <strong>{0}</strong>,</p>
  <p>Đơn hoa <strong>{1}</strong> vừa được cập nhật: <strong style=""color:#0ea5e9;"">{2}</strong></p>
  {3}
  <p><a href=""{4}"" style=""color:#0ea5e9;"">Xem chi tiết đơn hàng</a></p>
  <p style=""font-size:12px;color:#9ca3af;"">Hotline 0377 415 147</p>
</div>",
                WebUtilitySafe(name), orderCode, WebUtilitySafe(statusName),
                string.IsNullOrWhiteSpace(note) ? string.Empty : "<p>" + WebUtilitySafe(note) + "</p>",
                trackingUrl);
        }

        /// <summary>
        /// Encode các ký tự nguy hiểm để chặn HTML injection nhưng GIỮ NGUYÊN tiếng Việt.
        /// (WebUtility.HtmlEncode của .NET Framework biến "à" thành "&amp;#224;" -> mail đọc rất xấu.)
        /// </summary>
        private static string WebUtilitySafe(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}
