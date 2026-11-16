using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Cart;
using FlowerShop.Services.Email;
using FlowerShop.Services.Logging;
using FlowerShop.Services.Orders;
using FlowerShop.Services.Pricing;

namespace FlowerShop.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var cart = CartService.GetCart(HttpContext);
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                SenderName = Session["FullName"] as string,
                SenderEmail = Session["UserEmail"] as string,
                SenderPhone = Session["UserPhone"] as string,
                AvailableCards = _db.GreetingCards.AsNoTracking().Where(c => c.IsActive).ToList(),
                AvailableAddons = _db.AddonItems.AsNoTracking().Where(a => a.IsActive).ToList(),
                DeliveryDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(CheckoutViewModel model)
        {
            var cart = CartService.GetCart(HttpContext);
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Kiểm tra nghiệp vụ bổ sung (ModelState không bắt được các rule này).
            if (model != null)
            {
                if (model.DeliveryDate.Date < DateTime.Today)
                {
                    ModelState.AddModelError("DeliveryDate", "Ngày giao hoa không được ở quá khứ.");
                }

                if (string.IsNullOrWhiteSpace(model.DeliveryTimeSlot))
                {
                    ModelState.AddModelError("DeliveryTimeSlot", "Vui lòng chọn khung giờ giao hoa.");
                }

                if (!string.IsNullOrWhiteSpace(model.SenderPhone) && !IsVietnamesePhone(model.SenderPhone))
                {
                    ModelState.AddModelError("SenderPhone", "Số điện thoại người đặt chưa đúng định dạng Việt Nam.");
                }

                if (!string.IsNullOrWhiteSpace(model.RecipientPhone) && !IsVietnamesePhone(model.RecipientPhone))
                {
                    ModelState.AddModelError("RecipientPhone", "Số điện thoại người nhận chưa đúng định dạng Việt Nam.");
                }
            }

            if (model == null || !ModelState.IsValid)
            {
                return ReloadCheckoutView(model, cart);
            }

            // 1. Kiểm tra tồn kho trước khi ghi bất cứ thứ gì.
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
            var stockMap = _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.StockQuantity, p.IsActive })
                .ToDictionary(p => p.Id, p => p.IsActive ? p.StockQuantity : 0);

            var stockCheck = StockService.Validate(StockService.BuildRequests(cart.Items, stockMap));
            if (!stockCheck.Ok)
            {
                foreach (string error in stockCheck.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return ReloadCheckoutView(model, cart);
            }

            // 2. Tính tiền lại phía server (không tin số tiền do client gửi lên).
            var pricing = PricingService.CalculateTotals(cart.MerchandiseSubtotal, cart.AppliedVoucher, DateTime.Now);

            string orderCode = OrderCodeGenerator.Generate();

            var adapter = new DbContextAdapter(_db);
            Order order;

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    // 3. Trừ kho nguyên tử - nếu 1 sản phẩm fail thì rollback toàn bộ.
                    foreach (var item in cart.Items)
                    {
                        if (!adapter.TryDecrementStock(item.ProductId, item.Quantity))
                        {
                            transaction.Rollback();
                            ModelState.AddModelError(string.Empty,
                                string.Format("'{0}' vừa hết hàng trong lúc bạn đặt. Vui lòng chọn mẫu khác.", item.ProductName));
                            return ReloadCheckoutView(model, cart);
                        }
                    }

                    order = new Order
                    {
                        OrderCode = orderCode,
                        UserId = Session["UserId"] as int?,
                        SenderName = model.SenderName,
                        SenderPhone = model.SenderPhone,
                        SenderEmail = model.SenderEmail,
                        RecipientName = model.RecipientName,
                        RecipientPhone = model.RecipientPhone,
                        RecipientAddress = model.RecipientAddress,
                        District = model.District,
                        City = string.IsNullOrWhiteSpace(model.City) ? "Hồ Chí Minh" : model.City,
                        DeliveryDate = model.DeliveryDate,
                        DeliveryTimeSlot = model.DeliveryTimeSlot,
                        IsAnonymous = model.IsAnonymous,
                        GreetingCardId = model.SelectedCardId,
                        CardMessage = model.CardMessage,
                        OrderNotes = MergeNotes(model.OrderNotes, cart.OrderNotes),
                        SubTotal = cart.SubTotal,
                        AddonTotal = cart.AddonTotal,
                        ShippingFee = pricing.ShippingFee,
                        DiscountAmount = pricing.DiscountAmount,
                        TotalAmount = pricing.TotalAmount,
                        VoucherCode = cart.AppliedVoucher != null ? cart.AppliedVoucher.Code : null,
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = "Pending",
                        OrderStatus = "Pending",
                        CreatedAt = DateTime.Now
                    };

                    foreach (var item in cart.Items)
                    {
                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            ProductImageUrl = item.ImageUrl,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice
                        });
                    }

                    foreach (var addon in cart.Addons)
                    {
                        order.OrderAddons.Add(new OrderAddon
                        {
                            AddonItemId = addon.AddonId,
                            AddonName = addon.AddonName,
                            UnitPrice = addon.UnitPrice,
                            Quantity = addon.Quantity,
                            TotalPrice = addon.TotalPrice
                        });
                    }

                    _db.Orders.Add(order);

                    // 4. Voucher: COD trừ lượt ngay, cổng online trừ khi thanh toán thành công.
                    if (!string.IsNullOrEmpty(order.VoucherCode) &&
                        VoucherUsageService.ShouldConsumeOnPlacement(order.PaymentMethod))
                    {
                        VoucherUsageService.TryConsume(adapter, order.VoucherCode);
                    }

                    _db.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    AppLogger.Error("PlaceOrder thất bại cho giỏ hàng của " + model.SenderPhone, ex);
                    ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng lúc này. Vui lòng thử lại.");
                    return ReloadCheckoutView(model, cart);
                }
            }

            CartService.ClearCart(HttpContext);

            // 5. Email xác nhận (best-effort, không chặn luồng đặt hàng).
            var emailService = EmailServiceFactory.Create(Server.MapPath("~/"));
            OrderNotificationService.SendOrderConfirmation(emailService, order, BuildTrackingUrl(order.OrderCode));

            switch (model.PaymentMethod)
            {
                case "SEPAY":
                    // Chuyển sang trang thanh toán SePay (QR ngân hàng / thẻ / NAPAS)
                    return RedirectToAction("Checkout", "Payment", new { orderCode = order.OrderCode });

                default: // COD
                    return RedirectToAction("Success", "Payment", new { orderCode = order.OrderCode });
            }
        }

        private ActionResult ReloadCheckoutView(CheckoutViewModel model, ShoppingCart cart)
        {
            if (model == null)
            {
                model = new CheckoutViewModel();
            }

            model.Cart = cart;
            model.AvailableCards = _db.GreetingCards.AsNoTracking().Where(c => c.IsActive).ToList();
            model.AvailableAddons = _db.AddonItems.AsNoTracking().Where(a => a.IsActive).ToList();

            return View("Index", model);
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

        /// <summary>Gộp ghi chú khách nhập + ghi chú cấu hình sản phẩm (bó hoa tự thiết kế).</summary>
        private static string MergeNotes(string customerNotes, string productNotes)
        {
            bool hasCustomer = !string.IsNullOrWhiteSpace(customerNotes);
            bool hasProduct = !string.IsNullOrWhiteSpace(productNotes);

            if (hasCustomer && hasProduct)
            {
                return customerNotes.Trim() + " | " + productNotes.Trim();
            }

            if (hasCustomer)
            {
                return customerNotes.Trim();
            }

            return hasProduct ? productNotes.Trim() : null;
        }

        /// <summary>Số điện thoại Việt Nam: 10 số bắt đầu bằng 0, hoặc +84.</summary>
        private static bool IsVietnamesePhone(string phone)        {
            string digits = System.Text.RegularExpressions.Regex.Replace(phone ?? string.Empty, @"[^\d]", string.Empty);

            if (digits.StartsWith("84"))
            {
                digits = "0" + digits.Substring(2);
            }

            return System.Text.RegularExpressions.Regex.IsMatch(digits, @"^0\d{9}$");
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
