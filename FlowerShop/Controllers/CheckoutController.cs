using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Cart;
using FlowerShop.Services.Stripe;
using FlowerShop.Services.VNPay;

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
                AvailableCards = _db.GreetingCards.Where(c => c.IsActive).ToList(),
                AvailableAddons = _db.AddonItems.Where(a => a.IsActive).ToList(),
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

            if (!ModelState.IsValid)
            {
                model.Cart = cart;
                model.AvailableCards = _db.GreetingCards.Where(c => c.IsActive).ToList();
                model.AvailableAddons = _db.AddonItems.Where(a => a.IsActive).ToList();
                return View("Index", model);
            }

            // Tạo mã đơn hàng duy nhất: FL + yyyyMMdd + 4 số ngẫu nhiên
            string orderCode = $"FL{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";

            var order = new Order
            {
                OrderCode = orderCode,
                SenderName = model.SenderName,
                SenderPhone = model.SenderPhone,
                SenderEmail = model.SenderEmail,
                RecipientName = model.RecipientName,
                RecipientPhone = model.RecipientPhone,
                RecipientAddress = model.RecipientAddress,
                District = model.District,
                City = model.City ?? "Hồ Chí Minh",
                DeliveryDate = model.DeliveryDate,
                DeliveryTimeSlot = model.DeliveryTimeSlot,
                IsAnonymous = model.IsAnonymous,
                GreetingCardId = model.SelectedCardId,
                CardMessage = model.CardMessage,
                OrderNotes = model.OrderNotes,
                SubTotal = cart.SubTotal,
                AddonTotal = cart.AddonTotal,
                ShippingFee = cart.ShippingFee,
                DiscountAmount = cart.DiscountAmount,
                TotalAmount = cart.TotalAmount,
                VoucherCode = cart.AppliedVoucher?.Code,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Pending",
                OrderStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            // Thêm các món hoa chính
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

            // Thêm các phụ kiện quà tặng
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

            // Cập nhật số lần sử dụng voucher nếu có
            if (cart.AppliedVoucher != null)
            {
                var v = _db.Vouchers.Find(cart.AppliedVoucher.Id);
                if (v != null) v.UsedCount += 1;
            }

            _db.SaveChanges();

            // Xóa giỏ hàng sau khi tạo đơn
            CartService.ClearCart(HttpContext);

            // Chuyển hướng theo phương thức thanh toán
            switch (model.PaymentMethod)
            {
                case "VNPAY":
                    string vnpayUrl = VnPayService.CreatePaymentUrl(order, HttpContext);
                    return Redirect(vnpayUrl);

                case "VIETQR":
                    return RedirectToAction("VietQrPay", "Payment", new { orderCode = order.OrderCode });

                case "STRIPE":
                    return RedirectToAction("StripeCheckout", "Payment", new { orderCode = order.OrderCode });

                default: // COD
                    return RedirectToAction("Success", "Payment", new { orderCode = order.OrderCode });
            }
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
