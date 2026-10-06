using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;

namespace FlowerShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(int? categoryId, int? occasionId, int? flowerTypeId, string color, string priceRange, string sortBy, string search, int page = 1)
        {
            var query = _db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }
            if (occasionId.HasValue)
            {
                query = query.Where(p => p.OccasionId == occasionId.Value);
            }
            if (flowerTypeId.HasValue)
            {
                query = query.Where(p => p.FlowerTypeId == flowerTypeId.Value);
            }
            if (!string.IsNullOrEmpty(color))
            {
                query = query.Where(p => p.ColorTone == color);
            }
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.ShortDescription.Contains(search));
            }

            // Lọc theo khoảng giá
            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "under500":
                        query = query.Where(p => (p.SalePrice ?? p.Price) < 500000);
                        break;
                    case "500to1000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) >= 500000 && (p.SalePrice ?? p.Price) <= 1000000);
                        break;
                    case "1000to2000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) > 1000000 && (p.SalePrice ?? p.Price) <= 2000000);
                        break;
                    case "over2000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) > 2000000);
                        break;
                }
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "priceAsc":
                    query = query.OrderBy(p => p.SalePrice ?? p.Price);
                    break;
                case "priceDesc":
                    query = query.OrderByDescending(p => p.SalePrice ?? p.Price);
                    break;
                case "rating":
                    query = query.OrderByDescending(p => p.RatingAvg);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            int pageSize = 12;
            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            if (totalPages < 1) totalPages = 1;

            var pagedProducts = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new ProductFilterViewModel
            {
                Products = pagedProducts,
                Categories = _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
                Occasions = _db.Occasions.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).ToList(),
                FlowerTypes = _db.FlowerTypes.Where(f => f.IsActive).ToList(),
                SelectedCategoryId = categoryId,
                SelectedOccasionId = occasionId,
                SelectedFlowerTypeId = flowerTypeId,
                ColorTone = color,
                PriceRange = priceRange,
                SortBy = sortBy,
                SearchQuery = search,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalProducts = totalProducts
            };

            return View(viewModel);
        }

        public ActionResult Details(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return RedirectToAction("Index");
            }

            var product = _db.Products
                .Include("Category")
                .Include("Occasion")
                .Include("FlowerType")
                .Include("Images")
                .Include("Reviews")
                .FirstOrDefault(p => p.Slug == slug && p.IsActive);

            if (product == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm hoa này.");
            }

            ViewBag.AvailableAddons = _db.AddonItems.Where(a => a.IsActive).ToList();
            ViewBag.AvailableCards = _db.GreetingCards.Where(c => c.IsActive).ToList();
            ViewBag.RelatedProducts = _db.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToList();

            return View(product);
        }

        [HttpGet]
        public JsonResult SearchAjax(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = false, items = new object[] { } }, JsonRequestBehavior.AllowGet);
            }

            string term = q.Trim().ToLowerInvariant();
            var results = _db.Products
                .Where(p => p.IsActive && p.Name.ToLower().Contains(term))
                .Take(5)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    price = p.SalePrice ?? p.Price,
                    image = p.MainImageUrl
                })
                .ToList();

            return Json(new { success = true, items = results }, JsonRequestBehavior.AllowGet);
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
