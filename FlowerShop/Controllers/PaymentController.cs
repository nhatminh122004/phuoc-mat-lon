using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Email;
using FlowerShop.Services.Logging;
using FlowerShop.Services.Orders;
using FlowerShop.Services.SePay;

namespace FlowerShop.Controllers
{
    /// <summary>
    /// Thanh toán qua SePay (https://docs.sepay.vn):
    ///  - Chế độ "gateway": ký form và POST sang pay.sepay.vn/v1/checkout/init (QR/thẻ/NAPAS),
    ///    xác nhận bằng IPN (header X-Secret-Key).
    ///  - Chế độ "qr": hiện QR qr.sepay.vn ngay trên site, xác nhận bằng webhook biến động số dư.
    /// Ngoài ra COD xử lý ở CheckoutController.
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        // ------------------------------------------------------------------
        // KHỞI TẠO THANH TOÁN
        // ------------------------------------------------------------------

        /// <summary>
        /// Entry point: tự chọn trang thanh toán phù hợp theo cấu hình.
        /// </summary>
        [HttpGet]
        public ActionResult Checkout(string orderCode)
        {
            var order = FindOrder(orderCode);
            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (order.PaymentStatus == "Paid")
            {
                return RedirectToAction("Success", new { orderCode = order.OrderCode });
            }

            if (SePayOptions.Mode == SePayOptions.ModeQr || !SePayOptions.IsGatewayConfigured)
            {
                return RedirectToAction("Qr", new { orderCode = order.OrderCode });
            }

            try
            {
                var form = SePayPaymentService.BuildCheckoutForm(order, BuildPublicBaseUrl());
                return View("SePayCheckout", form);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Không tạo được form SePay cho đơn " + order.OrderCode, ex);
                ViewBag.Message = "Cổng thanh toán SePay chưa được cấu hình. Vui lòng chọn thanh toán khi nhận hoa (COD).";
                return View("Failed");
            }
        }

        /// <summary>Hiện QR chuyển khoản SePay (chế độ qr).</summary>
        [HttpGet]
        public ActionResult Qr(string orderCode)
        {
            var order = FindOrder(orderCode);
            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (order.PaymentStatus == "Paid")
            {
                return RedirectToAction("Success", new { orderCode = order.OrderCode });
            }

            ViewBag.QrUrl = SePayPaymentService.BuildQrUrl(order);
            ViewBag.Bank = SePayOptions.Bank;
            ViewBag.AccountNumber = SePayOptions.AccountNumber;
            ViewBag.AccountHolder = SePayOptions.AccountHolder;
            ViewBag.TransferContent = order.OrderCode;
            ViewBag.IsSandbox = SePayOptions.IsSandbox;

            return View(order);
        }

        // ------------------------------------------------------------------
        // IPN (cổng thanh toán) — POST JSON + X-Secret-Key
        // ------------------------------------------------------------------

        [HttpPost]
        public ActionResult Ipn()
        {
            string rawBody = ReadRequestBody();

            if (!VerifyIpnSecret())
            {
                AppLogger.Warn("SePay IPN bị từ chối: X-Secret-Key không khớp.");
                Response.StatusCode = 401;
                return Json(new { success = false, message = "Unauthorized" });
            }

            var payload = SePayPaymentService.ParseIpn(rawBody);

            if (payload == null || payload.Order == null)
            {
                AppLogger.Warn("SePay IPN payload không hợp lệ: " + Truncate(rawBody, 500));
                return Json(new { success = false, message = "Invalid payload" });
            }

            string invoiceNumber = OrderCodeGenerator.Normalize(payload.Order.OrderInvoiceNumber);
            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == invoiceNumber);

            if (order == null)
            {
                AppLogger.Warn("SePay IPN cho đơn không tồn tại: " + invoiceNumber);
                return Json(new { success = true, message = "Order not found, ignored" });
            }

            if (SePayPaymentService.IsVoidNotification(payload))
            {
                if (order.PaymentStatus == "Paid")
                {
                    order.PaymentStatus = "Refunded";
                    order.UpdatedAt = DateTime.Now;
                    _db.SaveChanges();
                }

                return Json(new { success = true, message = "Void acknowledged" });
            }

            if (!SePayPaymentService.IsSuccessfulNotification(payload))
            {
                AppLogger.Warn("SePay IPN bỏ qua: notification_type=" + payload.NotificationType
                    + ", transaction_status=" + (payload.Transaction != null ? payload.Transaction.TransactionStatus : "n/a"));
                return Json(new { success = true, message = "Ignored" });
            }

            if (!SePayPaymentService.AmountMatches(order.TotalAmount, payload.Order.OrderAmount)
                && (payload.Transaction == null
                    || !SePayPaymentService.AmountMatches(order.TotalAmount, payload.Transaction.TransactionAmount)))
            {
                AppLogger.Warn(string.Format("SePay IPN lệch số tiền đơn {0}: order_amount={1}, transaction_amount={2}, expected={3}",
                    order.OrderCode, payload.Order.OrderAmount,
                    payload.Transaction != null ? payload.Transaction.TransactionAmount : "n/a",
                    order.TotalAmount));
                return Json(new { success = false, message = "Amount mismatch" });
            }

            string transactionNo = payload.Transaction != null && !string.IsNullOrEmpty(payload.Transaction.TransactionId)
                ? payload.Transaction.TransactionId
                : payload.Order.Id;

            MarkOrderPaid(order, transactionNo, "ORDER_PAID", order.TotalAmount, rawBody);

            return Json(new { success = true, message = "OK" });
        }

        // ------------------------------------------------------------------
        // WEBHOOK biến động số dư (chế độ qr)
        // ------------------------------------------------------------------

        [HttpPost]
        public ActionResult Webhook()
        {
            string rawBody = ReadRequestBody();

            if (!VerifyWebhookAuth(rawBody))
            {
                AppLogger.Warn("SePay webhook bị từ chối: sai API key/HMAC.");
                Response.StatusCode = 401;
                return Json(new { success = false, message = "Unauthorized" });
            }

            var payload = SePayPaymentService.ParseBankWebhook(rawBody);

            if (payload == null)
            {
                return Json(new { success = false, message = "Invalid payload" });
            }

            // Chỉ quan tâm tiền vào.
            if (!string.Equals(payload.TransferType, "in", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = true, message = "Ignored (transfer out)" });
            }

            string orderCode = SePayPaymentService.ExtractOrderCode(payload.Code, payload.Content);
            if (string.IsNullOrEmpty(orderCode))
            {
                AppLogger.Info("SePay webhook không tìm thấy mã đơn trong nội dung: " + Truncate(payload.Content, 200));
                return Json(new { success = true, message = "No order code, ignored" });
            }

            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return Json(new { success = true, message = "Order not found, ignored" });
            }

            if (order.PaymentStatus == "Paid")
            {
                return Json(new { success = true, message = "Order already paid" });
            }

            if (!SePayPaymentService.AmountMatches(order.TotalAmount, payload.TransferAmount))
            {
                AppLogger.Warn(string.Format("SePay webhook lệch tiền đơn {0}: nhận {1}, cần {2}",
                    order.OrderCode, payload.TransferAmount, order.TotalAmount));
                return Json(new { success = true, message = "Amount mismatch, ignored" });
            }

            MarkOrderPaid(order, "BANK-" + payload.Id, "in", payload.TransferAmount, rawBody);

            return Json(new { success = true, message = "OK" });
        }

        // ------------------------------------------------------------------
        // RETURN / ERROR / CANCEL (redirect từ SePay)
        // ------------------------------------------------------------------

        /// <summary>
        /// success_url: KHÔNG tin tham số trên URL. Chỉ hiển thị thành công khi DB đã Paid
        /// (do IPN/webhook cập nhật); nếu chưa thì hiện trang chờ + polling.
        /// </summary>
        [HttpGet]
        public ActionResult Return(string orderCode, string order_invoice_number)
        {
            string code = OrderCodeGenerator.Normalize(!string.IsNullOrEmpty(orderCode) ? orderCode : order_invoice_number);
            var order = FindOrder(code);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (order.PaymentStatus == "Paid")
            {
                return RedirectToAction("Success", new { orderCode = order.OrderCode });
            }

            ViewBag.OrderCode = order.OrderCode;
            return View("Pending");
        }

        [HttpGet]
        public ActionResult Error(string orderCode, string order_invoice_number)
        {
            string code = OrderCodeGenerator.Normalize(!string.IsNullOrEmpty(orderCode) ? orderCode : order_invoice_number);
            var order = FindOrder(code);

            if (order != null && order.PaymentStatus != "Paid")
            {
                order.PaymentStatus = "Failed";
                order.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }

            ViewBag.Message = "Thanh toán qua SePay không thành công. Bạn có thể thử lại hoặc chọn thanh toán khi nhận hoa (COD).";
            ViewBag.OrderCode = code;
            return View("Failed");
        }

        [HttpGet]
        public ActionResult Cancel(string orderCode, string order_invoice_number)
        {
            string code = OrderCodeGenerator.Normalize(!string.IsNullOrEmpty(orderCode) ? orderCode : order_invoice_number);

            ViewBag.Message = "Bạn đã huỷ thanh toán. Đơn hoa vẫn được giữ và có thể thanh toán lại bất cứ lúc nào.";
            ViewBag.OrderCode = code;
            return View("Failed");
        }

        // ------------------------------------------------------------------
        // TRẠNG THÁI / KẾT QUẢ
        // ------------------------------------------------------------------

        /// <summary>Polling trạng thái thanh toán cho trang chờ QR/Return.</summary>
        [HttpGet]
        public JsonResult Status(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return Json(new { success = false, message = "Thiếu mã đơn hàng." }, JsonRequestBehavior.AllowGet);
            }

            string code = OrderCodeGenerator.Normalize(orderCode);

            var data = _db.Orders.AsNoTracking()
                .Where(o => o.OrderCode == code)
                .Select(o => new
                {
                    o.OrderCode,
                    o.PaymentStatus,
                    o.OrderStatus,
                    o.TotalAmount
                })
                .FirstOrDefault();

            if (data == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                orderCode = data.OrderCode,
                paymentStatus = data.PaymentStatus,
                orderStatus = data.OrderStatus,
                statusName = OrderNotificationService.GetStatusDisplayName(data.OrderStatus),
                isPaid = data.PaymentStatus == "Paid",
                totalAmount = data.TotalAmount
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Success(string orderCode)
        {
            // Chuẩn hoá TRƯỚC khi vào predicate: EF không dịch được OrderCodeGenerator.Normalize() sang SQL.
            string code = OrderCodeGenerator.Normalize(orderCode);

            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .AsNoTracking()
                .FirstOrDefault(o => o.OrderCode == code);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        [HttpGet]
        public ActionResult Failed()
        {
            return View();
        }

        // ------------------------------------------------------------------
        // HELPER
        // ------------------------------------------------------------------

        private Order FindOrder(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                return null;
            }

            string code = OrderCodeGenerator.Normalize(orderCode);
            return _db.Orders.FirstOrDefault(o => o.OrderCode == code);
        }

        private bool VerifyIpnSecret()
        {
            // Chưa cấu hình (dev/sandbox) thì bỏ qua để tiện thử; production bắt buộc điền.
            if (string.IsNullOrEmpty(SePayOptions.IpnSecretKey))
            {
                AppLogger.Warn("SePay IPN: chưa cấu hình SePay:IpnSecretKey — bỏ qua kiểm tra X-Secret-Key.");
                return true;
            }

            string provided = Request.Headers["X-Secret-Key"];
            return SePaySignature.VerifyHeaderEquals(provided, SePayOptions.IpnSecretKey);
        }

        private bool VerifyWebhookAuth(string rawBody)
        {
            if (!string.IsNullOrEmpty(SePayOptions.WebhookHmacSecret))
            {
                string signature = Request.Headers["X-Signature"] ?? Request.Headers["X-Hmac-Signature"];
                return SePaySignature.VerifyWebhookHmac(rawBody, signature, SePayOptions.WebhookHmacSecret);
            }

            if (!string.IsNullOrEmpty(SePayOptions.WebhookApiKey))
            {
                string token = SePaySignature.ExtractBearerToken(Request.Headers["Authorization"]);
                return SePaySignature.VerifyHeaderEquals(token, SePayOptions.WebhookApiKey);
            }

            AppLogger.Warn("SePay webhook: chưa cấu hình xác thực — chỉ nên dùng ở môi trường dev.");
            return true;
        }

        private string ReadRequestBody()
        {
            try
            {
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream, System.Text.Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Không đọc được body SePay callback", ex);
                return string.Empty;
            }
        }

        private string BuildPublicBaseUrl()
        {
            if (Request != null && Request.Url != null)
            {
                return SePayOptions.PublicBaseUrl(Request.Url.Scheme, Request.Url.Authority);
            }

            return SePayOptions.PublicBaseUrl("http", "localhost");
        }

        /// <summary>
        /// Đánh dấu đã thanh toán (idempotent): cập nhật trạng thái, ghi PaymentTransaction
        /// (chặn trùng bằng UNIQUE(Gateway, TransactionNo)), trừ lượt voucher, gửi mail.
        /// </summary>
        private void MarkOrderPaid(Order order, string transactionNo, string responseCode, decimal amount, string rawPayload)
        {
            if (order == null || order.PaymentStatus == "Paid")
            {
                return;
            }

            order.PaymentStatus = "Paid";
            order.OrderStatus = "Confirmed";
            order.UpdatedAt = DateTime.Now;

            bool duplicated = !string.IsNullOrEmpty(transactionNo)
                && _db.PaymentTransactions.Any(t => t.Gateway == "SEPAY" && t.TransactionNo == transactionNo);

            if (!duplicated)
            {
                _db.PaymentTransactions.Add(new PaymentTransaction
                {
                    OrderId = order.Id,
                    Gateway = "SEPAY",
                    TransactionNo = transactionNo,
                    GatewayResponseCode = responseCode,
                    Amount = amount,
                    Currency = "VND",
                    PayDate = DateTime.Now,
                    RawPayload = rawPayload
                });
            }

            _db.SaveChanges();

            if (!string.IsNullOrEmpty(order.VoucherCode))
            {
                try
                {
                    VoucherUsageService.TryConsume(new DbContextAdapter(_db), order.VoucherCode);
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Không trừ được lượt voucher " + order.VoucherCode + ": " + ex.Message);
                }
            }

            var emailService = EmailServiceFactory.Create(Server.MapPath("~/"));
            OrderNotificationService.SendStatusChanged(emailService, order,
                "Thanh toán đã được xác nhận qua SePay.", BuildTrackingUrl(order.OrderCode));
        }

        private string BuildTrackingUrl(string orderCode)
        {
            if (Request != null && Request.Url != null)
            {
                return string.Format("{0}://{1}/tra-cuu-don-hang?orderCode={2}",
                    Request.Url.Scheme, Request.Url.Authority, Server.UrlEncode(orderCode));
            }

            return "/tra-cuu-don-hang?orderCode=" + orderCode;
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= max ? value : value.Substring(0, max);
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
