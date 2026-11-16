using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Cart;

namespace FlowerShop.Controllers
{
    public class CartController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var cart = CartService.GetCart(HttpContext);
            ViewBag.AvailableAddons = _db.AddonItems.AsNoTracking().Where(a => a.IsActive).ToList();
            ViewBag.AvailableCards = _db.GreetingCards.AsNoTracking().Where(c => c.IsActive).ToList();
            return View(cart);
        }

        [HttpGet]
        public JsonResult GetCartJson()
        {
            var cart = CartService.GetCart(HttpContext);
            return Json(BuildCartResponse(cart), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int productId, int quantity = 1, bool isAjax = false)
        {
            CartService.AddProduct(HttpContext, productId, quantity);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã thêm sản phẩm vào giỏ hoa!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantity(int productId, int quantity, bool isAjax = false)
        {
            CartService.UpdateQuantity(HttpContext, productId, quantity);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã cập nhật số lượng!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int productId, bool isAjax = false)
        {
            CartService.RemoveProduct(HttpContext, productId);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã xóa sản phẩm khỏi giỏ!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart(bool isAjax = false)
        {
            CartService.ClearCart(HttpContext);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã làm trống giỏ hàng!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAddon(int addonId, int quantity = 1, bool isAjax = false)
        {
            CartService.AddAddon(HttpContext, addonId, quantity);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã thêm phụ kiện quà tặng!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveAddon(int addonId, bool isAjax = false)
        {
            CartService.RemoveAddon(HttpContext, addonId);

            if (isAjax || Request.IsAjaxRequest())
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(cart, true, "Đã xóa phụ kiện quà tặng!"));
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyVoucher(string code, bool isAjax = false)
        {
            string message;
            bool ok = CartService.ApplyVoucher(HttpContext, code, out message);

            if (isAjax || Request.IsAjaxRequest())
            {
                var currentCart = CartService.GetCart(HttpContext);
                return Json(BuildCartResponse(currentCart, ok, message));
            }

            if (ok)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult GetMiniCart()
        {
            var cart = CartService.GetCart(HttpContext);
            return PartialView("_MiniCart", cart);
        }

        /// <summary>
        /// Dữ liệu giỏ cho FE: mọi con số tiền lấy từ server (PricingService),
        /// FE chỉ hiển thị - không tự tính lại để tránh lệch.
        /// </summary>
        private object BuildCartResponse(ShoppingCart cart, bool success = true, string message = null)
        {
            var pricing = cart.Pricing;

            return new
            {
                success = success,
                message = message,
                totalItems = cart.TotalItemsCount,
                subTotal = cart.SubTotal,
                addonTotal = cart.AddonTotal,
                merchandiseSubtotal = cart.MerchandiseSubtotal,
                shippingFee = pricing.ShippingFee,
                discountAmount = pricing.DiscountAmount,
                voucherCode = cart.AppliedVoucher != null ? cart.AppliedVoucher.Code : "",
                totalAmount = pricing.TotalAmount,
                freeShipping = pricing.FreeShipping,
                amountToFreeShipping = pricing.AmountToFreeShipping,
                freeShippingThreshold = Services.Pricing.PricingService.FreeShippingThreshold,
                items = cart.Items.Select(i => new
                {
                    productId = i.ProductId,
                    name = i.ProductName,
                    slug = i.ProductSlug,
                    price = i.UnitPrice,
                    qty = i.Quantity,
                    image = i.ImageUrl,
                    totalPrice = i.TotalPrice
                }).ToList(),
                addons = cart.Addons.Select(a => new
                {
                    addonId = a.AddonId,
                    name = a.AddonName,
                    price = a.UnitPrice,
                    qty = a.Quantity,
                    image = a.ImageUrl,
                    totalPrice = a.TotalPrice
                }).ToList()
            };
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
