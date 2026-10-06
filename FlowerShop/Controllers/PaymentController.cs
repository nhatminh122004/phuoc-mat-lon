using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Stripe;
using FlowerShop.Services.VietQR;
using FlowerShop.Services.VNPay;

namespace FlowerShop.Controllers
{
    public class PaymentController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        // 1. Nhận phản hồi từ VNPay khi khách hàng hoàn tất thanh toán
        public ActionResult VnpayReturn()
        {
            bool isValidSignature = VnPayService.ValidateCallback(Request, out string orderCode, out string responseCode, out string transactionNo);

            if (!isValidSignature)
            {
                ViewBag.Message = "Chữ ký bảo mật không hợp lệ (Dữ liệu bị can thiệp)!";
                return View("Failed");
            }

            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null)
            {
                ViewBag.Message = "Không tìm thấy thông tin đơn hàng!";
                return View("Failed");
            }

            if (responseCode == "00")
            {
                // Thanh toán thành công
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                _db.PaymentTransactions.Add(new PaymentTransaction
                {
                    OrderId = order.Id,
                    Gateway = "VNPAY",
                    TransactionNo = transactionNo,
                    GatewayResponseCode = responseCode,
                    Amount = order.TotalAmount,
                    Currency = "VND",
                    PayDate = DateTime.Now,
                    RawPayload = Request.QueryString.ToString()
                });

                _db.SaveChanges();
                return RedirectToAction("Success", new { orderCode = order.OrderCode });
            }
            else
            {
                // Giao dịch không thành công hoặc khách hàng hủy
                order.PaymentStatus = "Failed";
                _db.SaveChanges();

                ViewBag.Message = $"Giao dịch VNPay không thành công (Mã lỗi: {responseCode}). Vui lòng thử lại hoặc chọn phương thức thanh toán khác.";
                ViewBag.OrderCode = orderCode;
                return View("Failed");
            }
        }

        // 2. IPN Webhook dành cho Server VNPay gọi ngầm
        [HttpGet]
        public JsonResult VnpayIpn()
        {
            bool isValidSignature = VnPayService.ValidateCallback(Request, out string orderCode, out string responseCode, out string transactionNo);

            if (!isValidSignature)
            {
                return Json(new { RspCode = "97", Message = "Invalid signature" }, JsonRequestBehavior.AllowGet);
            }

            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null)
            {
                return Json(new { RspCode = "01", Message = "Order not found" }, JsonRequestBehavior.AllowGet);
            }

            if (order.PaymentStatus == "Paid")
            {
                return Json(new { RspCode = "02", Message = "Order already confirmed" }, JsonRequestBehavior.AllowGet);
            }

            if (responseCode == "00")
            {
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                _db.PaymentTransactions.Add(new PaymentTransaction
                {
                    OrderId = order.Id,
                    Gateway = "VNPAY",
                    TransactionNo = transactionNo,
                    GatewayResponseCode = responseCode,
                    Amount = order.TotalAmount,
                    Currency = "VND",
                    PayDate = DateTime.Now,
                    RawPayload = Request.QueryString.ToString()
                });

                _db.SaveChanges();
                return Json(new { RspCode = "00", Message = "Confirm Success" }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { RspCode = "00", Message = "Confirm Success" }, JsonRequestBehavior.AllowGet);
        }

        // 3. Trang hiển thị mã VietQR động
        public ActionResult VietQrPay(string orderCode)
        {
            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null) return RedirectToAction("Index", "Home");

            ViewBag.QrCodeUrl = VietQrService.GenerateQrCodeUrl(order);
            return View(order);
        }

        // 4. Trang thanh toán thẻ quốc tế Stripe Elements
        public ActionResult StripeCheckout(string orderCode)
        {
            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null) return RedirectToAction("Index", "Home");

            var paymentIntent = StripePaymentService.CreatePaymentIntent(order);
            ViewBag.ClientSecret = paymentIntent.ClientSecret;
            ViewBag.PublishableKey = StripePaymentService.PublishableKey;

            return View(order);
        }

        [HttpPost]
        public ActionResult StripeConfirm(string orderCode, string paymentIntentId)
        {
            var order = _db.Orders.FirstOrDefault(o => o.OrderCode == orderCode);
            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            if (StripePaymentService.ConfirmPayment(paymentIntentId, out string chargeId))
            {
                order.PaymentStatus = "Paid";
                order.OrderStatus = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                _db.PaymentTransactions.Add(new PaymentTransaction
                {
                    OrderId = order.Id,
                    Gateway = "STRIPE",
                    TransactionNo = paymentIntentId,
                    GatewayResponseCode = "succeeded",
                    Amount = order.TotalAmount,
                    Currency = "USD",
                    PayDate = DateTime.Now,
                    RawPayload = $"ChargeId: {chargeId}"
                });

                _db.SaveChanges();
                return Json(new { success = true, redirectUrl = Url.Action("Success", new { orderCode = order.OrderCode }) });
            }

            return Json(new { success = false, message = "Thanh toán Stripe chưa được hoàn tất!" });
        }

        // 5. Trang Đặt Hàng Thành Công
        public ActionResult Success(string orderCode)
        {
            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .FirstOrDefault(o => o.OrderCode == orderCode);

            if (order == null) return RedirectToAction("Index", "Home");

            return View(order);
        }

        public ActionResult Failed()
        {
            return View();
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
