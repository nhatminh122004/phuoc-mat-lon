using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Cart;

namespace FlowerShop.Controllers
{
    public class CustomBouquetController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var model = new CustomBouquetViewModel
            {
                MainFlowers = _db.Products.Where(p => p.IsActive).Take(6).ToList(),
                SubFlowers = _db.Products.Where(p => p.FlowerType.Slug == "hoa-baby" && p.IsActive).Take(3).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult AddToCart(string wrapStyle, int mainFlowerId, int mainFlowerQuantity, string paperColor, string ribbonColor, string greetingMessage)
        {
            var product = _db.Products.Find(mainFlowerId);
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // Tính đơn giá bó hoa tự thiết kế
            decimal baseWrapCost = 80000;
            if (wrapStyle == "GioHoa") baseWrapCost = 150000;
            if (wrapStyle == "HopMica") baseWrapCost = 250000;

            decimal flowerUnitCost = 25000; // Đơn giá mỗi cành hoa tuyển chọn
            decimal customPrice = baseWrapCost + (flowerUnitCost * mainFlowerQuantity);

            var cart = CartService.GetCart(HttpContext);
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = $"[Tự Thiết Kế] {product.Name} ({mainFlowerQuantity} cành - Giấy {paperColor})",
                ProductSlug = product.Slug,
                ImageUrl = product.MainImageUrl,
                UnitPrice = customPrice,
                Quantity = 1
            });

            if (!string.IsNullOrEmpty(greetingMessage))
            {
                cart.CardMessage = greetingMessage;
            }

            return RedirectToAction("Index", "Cart");
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
