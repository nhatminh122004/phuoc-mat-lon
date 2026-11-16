using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Cart;
using FlowerShop.Services.Orders;

namespace FlowerShop.Controllers
{
    public class CustomBouquetController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var model = new CustomBouquetViewModel
            {
                // Chỉ lấy sản phẩm còn hàng để tránh khách chọn hoa đã hết.
                MainFlowers = _db.Products.AsNoTracking()
                    .Where(p => p.IsActive && p.StockQuantity > 0)
                    .OrderByDescending(p => p.RatingAvg)
                    .Take(6)
                    .ToList(),

                SubFlowers = _db.Products.AsNoTracking()
                    .Where(p => p.IsActive && p.FlowerType != null && p.FlowerType.Slug == "hoa-baby")
                    .OrderByDescending(p => p.RatingAvg)
                    .Take(3)
                    .ToList(),

                MainFlowerQuantity = 19
            };

            // Cấu hình giá do server cung cấp -> FE không hardcode (tránh lệch giá).
            ViewBag.WrapCosts = new Dictionary<string, decimal>
            {
                { BouquetBuilderService.WrapBoTron, BouquetBuilderService.WrapCostBoTron },
                { BouquetBuilderService.WrapBoDai, BouquetBuilderService.WrapCostBoDai },
                { BouquetBuilderService.WrapGioHoa, BouquetBuilderService.WrapCostGioHoa },
                { BouquetBuilderService.WrapHopMica, BouquetBuilderService.WrapCostHopMica }
            };

            ViewBag.AllowedStemCounts = BouquetBuilderService.AllowedStemCounts;
            ViewBag.DefaultStemUnitPrice = BouquetBuilderService.DefaultStemUnitPrice;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(string wrapStyle, int mainFlowerId, int mainFlowerQuantity = 19,
            string paperColor = null, string ribbonColor = null, string greetingMessage = null)
        {
            var product = _db.Products.AsNoTracking()
                .Where(p => p.Id == mainFlowerId && p.IsActive)
                .Select(p => new { p.Id, p.Name, p.Slug, p.MainImageUrl, p.Price, p.SalePrice, p.StockQuantity })
                .FirstOrDefault();

            if (product == null)
            {
                TempData["ErrorMessage"] = "Mẫu hoa bạn chọn không còn kinh doanh. Vui lòng chọn mẫu khác.";
                return RedirectToAction("Index");
            }

            if (!BouquetBuilderService.IsValidStemCount(mainFlowerQuantity))
            {
                TempData["ErrorMessage"] = "Số lượng cành chỉ hỗ trợ 9, 19, 33 hoặc 99 cành.";
                return RedirectToAction("Index");
            }

            if (mainFlowerQuantity > product.StockQuantity)
            {
                TempData["ErrorMessage"] = string.Format("Mẫu hoa này chỉ còn {0} cành trong kho.", product.StockQuantity);
                return RedirectToAction("Index");
            }

            decimal basePrice = product.SalePrice ?? product.Price;
            decimal stemUnitPrice = BouquetBuilderService.ResolveStemUnitPrice(basePrice);
            decimal customPrice = BouquetBuilderService.CalculatePrice(wrapStyle, mainFlowerQuantity, stemUnitPrice);

            string lineName = BouquetBuilderService.BuildLineName(product.Name, wrapStyle, mainFlowerQuantity, paperColor);

            var cart = CartService.GetCart(HttpContext);
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = lineName,
                ProductSlug = product.Slug,
                ImageUrl = product.MainImageUrl,
                UnitPrice = customPrice,
                Quantity = 1
            });

            if (!string.IsNullOrWhiteSpace(greetingMessage))
            {
                cart.CardMessage = greetingMessage.Trim();
            }

            // Lưu cấu hình thiết kế vào Session để Checkout ghi kèm vào ghi chú đơn hàng
            // (trước đây cấu hình bị mất, nghệ nhân không biết cắm theo yêu cầu nào).
            cart.OrderNotes = string.Format(
                "Thiết kế riêng: {0} | {1} cành | Giấy {2} | Ruy băng {3} | Đơn giá cành {4:N0}đ",
                BouquetBuilderService.GetWrapDisplayName(wrapStyle),
                mainFlowerQuantity,
                string.IsNullOrWhiteSpace(paperColor) ? "tuỳ chọn" : paperColor,
                string.IsNullOrWhiteSpace(ribbonColor) ? "tuỳ chọn" : ribbonColor,
                stemUnitPrice);

            TempData["SuccessMessage"] = "Đã thêm bó hoa tự thiết kế vào giỏ!";
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
