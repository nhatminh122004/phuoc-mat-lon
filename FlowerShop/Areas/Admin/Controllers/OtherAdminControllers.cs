using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;

namespace FlowerShop.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string search, int? categoryId)
        {
            var query = _db.Products.Include("Category").Include("Occasion").Include("FlowerType").AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            ViewBag.Categories = _db.Categories.ToList();
            var list = query.OrderByDescending(p => p.Id).ToList();
            return View(list);
        }

        public ActionResult Create()
        {
            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name");
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name");
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name");
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Product product)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = product.Name.ToLower().Replace(" ", "-").Replace("'", "");
                }
                product.CreatedAt = DateTime.Now;
                _db.Products.Add(product);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        public ActionResult Edit(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null) return HttpNotFound();

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                var existing = _db.Products.Find(product.Id);
                if (existing != null)
                {
                    existing.Name = product.Name;
                    existing.Slug = product.Slug;
                    existing.CategoryId = product.CategoryId;
                    existing.OccasionId = product.OccasionId;
                    existing.FlowerTypeId = product.FlowerTypeId;
                    existing.SKU = product.SKU;
                    existing.ShortDescription = product.ShortDescription;
                    existing.FullDescription = product.FullDescription;
                    existing.FlowerCareGuide = product.FlowerCareGuide;
                    existing.ColorTone = product.ColorTone;
                    existing.Price = product.Price;
                    existing.SalePrice = product.SalePrice;
                    existing.StockQuantity = product.StockQuantity;
                    existing.MainImageUrl = product.MainImageUrl;
                    existing.IsFeatured = product.IsFeatured;
                    existing.IsBestSeller = product.IsBestSeller;
                    existing.IsFlashSale = product.IsFlashSale;
                    existing.IsActive = product.IsActive;

                    _db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        [HttpPost]
        public ActionResult ToggleActive(int id)
        {
            var product = _db.Products.Find(id);
            if (product != null)
            {
                product.IsActive = !product.IsActive;
                _db.SaveChanges();
            }
            return RedirectToAction("Index");
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

    public class VoucherController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var list = _db.Vouchers.OrderByDescending(v => v.Id).ToList();
            return View(list);
        }

        public ActionResult Create()
        {
            return View(new Voucher { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(3) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _db.Vouchers.Add(voucher);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(voucher);
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

    public class SettingsController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
