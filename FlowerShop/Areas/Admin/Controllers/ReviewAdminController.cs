using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.Areas.Admin.Filters;
using FlowerShop.DAL;

namespace FlowerShop.Areas.Admin.Controllers
{
    /// <summary>
    /// Duyệt đánh giá của khách. Review mới được tạo với IsApproved = false
    /// (chống spam), nên bắt buộc phải có màn hình này để đánh giá hiển thị ra storefront.
    /// </summary>
    [AdminAuthorize]
    public class ReviewController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string status = "pending", int page = 1)
        {
            const int pageSize = 20;
            if (page < 1) page = 1;

            var query = _db.Reviews.AsNoTracking().Include("Product").AsQueryable();

            if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => !r.IsApproved);
            }
            else if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.IsApproved);
            }

            int total = query.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.Status = status;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total = total;
            ViewBag.PendingCount = _db.Reviews.AsNoTracking().Count(r => !r.IsApproved);
            ViewBag.ApprovedCount = _db.Reviews.AsNoTracking().Count(r => r.IsApproved);

            var reviews = query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(reviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id, string returnStatus = "pending", int page = 1)
        {
            var review = _db.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null && !review.IsApproved)
            {
                review.IsApproved = true;
                RecalculateProductRating(review.ProductId);
                TempData["SuccessMessage"] = "Đã duyệt đánh giá và cập nhật điểm trung bình của sản phẩm.";
            }

            return RedirectToAction("Index", new { status = returnStatus, page = page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string returnStatus = "pending", int page = 1)
        {
            var review = _db.Reviews.FirstOrDefault(r => r.Id == id);
            if (review != null)
            {
                int productId = review.ProductId;
                _db.Reviews.Remove(review);
                _db.SaveChanges();

                RecalculateProductRating(productId);
                TempData["SuccessMessage"] = "Đã xoá đánh giá.";
            }

            return RedirectToAction("Index", new { status = returnStatus, page = page });
        }

        /// <summary>
        /// Tính lại RatingAvg / ReviewCount chỉ từ review đã duyệt - tránh điểm ảo do review chờ duyệt.
        /// </summary>
        private void RecalculateProductRating(int productId)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return;
            }

            var approved = _db.Reviews.AsNoTracking()
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Select(r => r.Rating)
                .ToList();

            if (approved.Count == 0)
            {
                product.ReviewCount = 0;
                product.RatingAvg = 0;
            }
            else
            {
                product.ReviewCount = approved.Count;
                product.RatingAvg = Math.Round(approved.Average(), 1);
            }

            _db.SaveChanges();
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
