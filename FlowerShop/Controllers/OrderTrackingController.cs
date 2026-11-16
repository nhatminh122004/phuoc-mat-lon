using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.AI;
using FlowerShop.Services.Orders;

namespace FlowerShop.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        /// <summary>
        /// Tra cứu đơn: bắt buộc mã đơn + số điện thoại (người gửi hoặc người nhận).
        /// Bản cũ cho phép tra chỉ bằng mã đơn nên có thể dò đơn của người khác.
        /// Khách đã đăng nhập và là chủ đơn thì không cần nhập lại số điện thoại.
        /// </summary>
        public ActionResult Index(string orderCode, string phone)
        {
            var model = new OrderTrackingViewModel
            {
                OrderCode = orderCode,
                PhoneNumber = phone
            };

            int? currentUserId = Session["UserId"] as int?;
            string currentUserEmail = Session["UserEmail"] as string;
            string currentUserPhone = Session["UserPhone"] as string;

            if (currentUserId.HasValue || !string.IsNullOrEmpty(currentUserEmail))
            {
                var myOrders = _db.Orders.AsNoTracking()
                    .Where(o => (currentUserId.HasValue && o.UserId == currentUserId.Value)
                             || (!string.IsNullOrEmpty(currentUserEmail) && o.SenderEmail == currentUserEmail))
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderCode,
                        o.OrderStatus,
                        o.TotalAmount,
                        o.CreatedAt,
                        o.RecipientName,
                        o.DeliveryDate
                    })
                    .ToList();

                ViewBag.MyOrders = myOrders;
            }

            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return View(model);
            }

            model.IsSearched = true;
            string cleanCode = OrderCodeGenerator.Normalize(orderCode);

            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .AsNoTracking()
                .FirstOrDefault(o => o.OrderCode == cleanCode);

            if (order == null)
            {
                model.ErrorMessage = "Không tìm thấy đơn hàng với mã đã nhập. Vui lòng kiểm tra lại mã trong email/SMS xác nhận.";
                return View(model);
            }

            bool isOwner = (currentUserId.HasValue && order.UserId == currentUserId.Value)
                        || (!string.IsNullOrEmpty(currentUserEmail) &&
                            string.Equals(order.SenderEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase));

            if (!isOwner)
            {
                string cleanPhone = ChatbotIntentDetector.NormalizePhone(phone);

                if (string.IsNullOrWhiteSpace(cleanPhone))
                {
                    model.ErrorMessage = "Vì lý do bảo mật, vui lòng nhập thêm số điện thoại đặt hàng để xem đơn này.";
                    return View(model);
                }

                string senderPhone = ChatbotIntentDetector.NormalizePhone(order.SenderPhone);
                string recipientPhone = ChatbotIntentDetector.NormalizePhone(order.RecipientPhone);

                if (!string.Equals(senderPhone, cleanPhone, StringComparison.Ordinal)
                    && !string.Equals(recipientPhone, cleanPhone, StringComparison.Ordinal))
                {
                    model.ErrorMessage = "Số điện thoại không khớp với mã đơn hàng này.";
                    return View(model);
                }
            }

            model.Order = order;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveProof(string orderCode, string phone)
        {
            return MutateOrder(orderCode, phone, "Approve");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RequestRevision(string orderCode, string phone, string feedback)
        {
            if (string.IsNullOrWhiteSpace(feedback))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung yêu cầu chỉnh sửa bó hoa." });
            }

            return MutateOrder(orderCode, phone, "Revise", feedback.Trim());
        }

        private JsonResult MutateOrder(string orderCode, string phone, string action, string feedback = null)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return Json(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            string cleanCode = OrderCodeGenerator.Normalize(orderCode);

            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == cleanCode);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            int? currentUserId = Session["UserId"] as int?;
            string currentUserEmail = Session["UserEmail"] as string;

            bool isOwner = (currentUserId.HasValue && order.UserId == currentUserId.Value)
                        || (!string.IsNullOrEmpty(currentUserEmail) &&
                            string.Equals(order.SenderEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase));

            if (!isOwner)
            {
                string cleanPhone = ChatbotIntentDetector.NormalizePhone(phone);
                string senderPhone = ChatbotIntentDetector.NormalizePhone(order.SenderPhone);
                string recipientPhone = ChatbotIntentDetector.NormalizePhone(order.RecipientPhone);

                if (string.IsNullOrWhiteSpace(cleanPhone)
                    || (!string.Equals(senderPhone, cleanPhone, StringComparison.Ordinal)
                        && !string.Equals(recipientPhone, cleanPhone, StringComparison.Ordinal)))
                {
                    return Json(new { success = false, message = "Số điện thoại không khớp với đơn hàng này." });
                }
            }

            if (order.OrderStatus == "Delivered" || order.OrderStatus == "Cancelled")
            {
                return Json(new { success = false, message = "Đơn hàng đã kết thúc, không thể thay đổi được nữa." });
            }

            string stamp = string.Format("[{0} lúc {1:HH:mm dd/MM/yyyy}]", action == "Approve"
                ? "Khách đã duyệt ảnh thực tế"
                : "Yêu cầu chỉnh sửa: " + feedback, DateTime.Now);

            if (action == "Approve")
            {
                order.OrderStatus = "Shipping";
            }
            else
            {
                order.OrderStatus = "Arranging";
            }

            order.OrderNotes = string.IsNullOrEmpty(order.OrderNotes)
                ? stamp
                : order.OrderNotes + " | " + stamp;

            order.UpdatedAt = DateTime.Now;
            _db.SaveChanges();

            string message = action == "Approve"
                ? "Cảm ơn bạn đã duyệt ảnh! Bó hoa đang được bàn giao cho shipper đi giao ngay."
                : "Đã gửi yêu cầu chỉnh sửa tới nghệ nhân cắm hoa. Chúng tôi sẽ cập nhật ảnh mới sớm nhất!";

            return Json(new { success = true, newStatus = order.OrderStatus, statusName = OrderNotificationService.GetStatusDisplayName(order.OrderStatus), message = message });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
