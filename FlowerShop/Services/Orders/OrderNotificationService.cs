using System;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Email;
using FlowerShop.Services.Logging;

namespace FlowerShop.Services.Orders
{
    /// <summary>
    /// Gửi thông báo email theo trạng thái đơn. Mọi lỗi SMTP đều được nuốt + ghi log:
    /// khách đặt hoa thành công không được phép fail vì mail server.
    /// </summary>
    public static class OrderNotificationService
    {
        public static string GetStatusDisplayName(string orderStatus)
        {
            switch (orderStatus)
            {
                case "Pending": return "Chờ xác nhận";
                case "Confirmed": return "Đã xác nhận";
                case "Arranging": return "Nghệ nhân đang cắm hoa";
                case "ProofReady": return "Đã cắm xong - Chờ khách duyệt ảnh";
                case "Shipping": return "Shipper đang giao hàng";
                case "Delivered": return "Đã giao hoa thành công";
                case "Cancelled": return "Đã huỷ đơn";
                default: return orderStatus;
            }
        }

        public static bool SendOrderConfirmation(IEmailService emailService, Order order, string trackingUrl)
        {
            if (emailService == null || order == null || string.IsNullOrWhiteSpace(order.SenderEmail))
            {
                return false;
            }

            string error;
            bool ok = emailService.Send(
                order.SenderEmail,
                EmailTemplates.OrderConfirmationSubject(order.OrderCode),
                EmailTemplates.OrderConfirmationBody(
                    order.SenderName,
                    order.OrderCode,
                    order.TotalAmount,
                    order.RecipientName,
                    order.DeliveryDate,
                    order.DeliveryTimeSlot,
                    trackingUrl),
                out error);

            if (!ok)
            {
                AppLogger.Warn("Gửi email xác nhận đơn " + order.OrderCode + " thất bại: " + error);
            }

            return ok;
        }

        public static bool SendStatusChanged(IEmailService emailService, Order order, string note, string trackingUrl)
        {
            if (emailService == null || order == null || string.IsNullOrWhiteSpace(order.SenderEmail))
            {
                return false;
            }

            string error;
            bool ok = emailService.Send(
                order.SenderEmail,
                EmailTemplates.OrderStatusChangedSubject(order.OrderCode, GetStatusDisplayName(order.OrderStatus)),
                EmailTemplates.OrderStatusChangedBody(
                    order.SenderName,
                    order.OrderCode,
                    GetStatusDisplayName(order.OrderStatus),
                    note,
                    trackingUrl),
                out error);

            if (!ok)
            {
                AppLogger.Warn("Gửi email trạng thái đơn " + order.OrderCode + " thất bại: " + error);
            }

            return ok;
        }
    }
}
