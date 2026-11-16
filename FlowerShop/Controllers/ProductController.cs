using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Helpers;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Upload;

namespace FlowerShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(int? categoryId, int? occasionId, int? flowerTypeId, string color, string priceRange, string sortBy, string search, int page = 1)
        {
            if (page < 1) page = 1;

            // AsNoTracking + Include: bộ lọc chạy 1 query duy nhất; view cần product.Occasion.Name
            // nên phải Include tường minh (lazy loading đã tắt để chống N+1).
            var query = _db.Products.AsNoTracking()
                .Include("Occasion")
                .Where(p => p.IsActive);

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
                // Chuẩn hoá để khớp dữ liệu nhập tay ("do", "Đỏ", "ĐỎ" -> "Đỏ")
                string normalizedColor = SlugHelper.NormalizeColorTone(color);
                query = query.Where(p => p.ColorTone == normalizedColor);
            }
            if (!string.IsNullOrEmpty(search))
            {
                string term = search.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.ShortDescription.Contains(term));
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                switch (priceRange)
                {
                    case "under500":
                        query = query.Where(p => (p.SalePrice ?? p.Price) < 500000m);
                        break;
                    case "500to1000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) >= 500000m && (p.SalePrice ?? p.Price) <= 1000000m);
                        break;
                    case "1000to2000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) > 1000000m && (p.SalePrice ?? p.Price) <= 2000000m);
                        break;
                    case "over2000":
                        query = query.Where(p => (p.SalePrice ?? p.Price) > 2000000m);
                        break;
                }
            }

            switch (sortBy)
            {
                case "priceAsc":
                    query = query.OrderBy(p => p.SalePrice ?? p.Price);
                    break;
                case "priceDesc":
                    query = query.OrderByDescending(p => p.SalePrice ?? p.Price);
                    break;
                case "rating":
                    query = query.OrderByDescending(p => p.RatingAvg).ThenByDescending(p => p.ReviewCount);
                    break;
                default:
                    query = query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
                    break;
            }

            const int pageSize = 12;
            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedProducts = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new ProductFilterViewModel
            {
                Products = pagedProducts,
                Categories = _db.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
                Occasions = _db.Occasions.AsNoTracking().Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).ToList(),
                FlowerTypes = _db.FlowerTypes.AsNoTracking().Where(f => f.IsActive).ToList(),
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
                .AsNoTracking()
                .FirstOrDefault(p => p.Slug == slug && p.IsActive);

            if (product == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm hoa này.");
            }

            ViewBag.AvailableAddons = _db.AddonItems.AsNoTracking().Where(a => a.IsActive).ToList();
            ViewBag.AvailableCards = _db.GreetingCards.AsNoTracking().Where(c => c.IsActive).ToList();
            ViewBag.RelatedProducts = _db.Products.AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .OrderByDescending(p => p.RatingAvg)
                .Take(4)
                .ToList();

            ViewBag.ApprovedReviews = product.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToList();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(
            int productId,
            string customerName,
            int rating = 5,
            string comment = "",
            IEnumerable<System.Web.HttpPostedFileBase> reviewImageFiles = null,
            System.Web.HttpPostedFileBase reviewImageFile = null)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                customerName = Session["FullName"] as string ?? "Khách Hàng Yêu Hoa";
            }

            if (rating < 1) rating = 1;
            if (rating > 5) rating = 5;

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] = "Vui lòng nhập nội dung nhận xét của bạn.";
                return Redirect("/hoa/" + product.Slug + "#reviews-section");
            }

            var filesToProcess = new List<System.Web.HttpPostedFileBase>();
            if (reviewImageFiles != null)
            {
                filesToProcess.AddRange(reviewImageFiles.Where(f => f != null && f.ContentLength > 0));
            }
            if (reviewImageFile != null && reviewImageFile.ContentLength > 0 && !filesToProcess.Contains(reviewImageFile))
            {
                filesToProcess.Add(reviewImageFile);
            }

            string uploadedImgUrl = null;
            if (filesToProcess.Any())
            {
                List<string> errors;
                var savedList = FileUploadService.SaveMultipleImageFiles(filesToProcess, Server, "Reviews", out errors);
                uploadedImgUrl = FileUploadService.JoinImageUrls(savedList);
            }

            // Review mới cần admin duyệt (IsApproved = false) để chặn spam.
            _db.Reviews.Add(new Review
            {
                ProductId = product.Id,
                CustomerName = customerName.Trim(),
                Rating = rating,
                Comment = comment.Trim(),
                ImageUrl = uploadedImgUrl,
                IsApproved = false,
                CreatedAt = DateTime.Now
            });

            _db.SaveChanges();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá! Nhận xét sẽ hiển thị sau khi tiệm duyệt.";
            return Redirect("/hoa/" + product.Slug + "#reviews-section");
        }

        [HttpGet]
        public JsonResult SearchAjax(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = false, items = new object[] { } }, JsonRequestBehavior.AllowGet);
            }

            string term = q.Trim();

            var results = _db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Name.Contains(term))
                .OrderByDescending(p => p.IsBestSeller)
                .ThenByDescending(p => p.RatingAvg)
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
