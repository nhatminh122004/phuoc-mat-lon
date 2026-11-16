using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;

namespace FlowerShop.Controllers
{
    public class HomeController : Controller
    {
        private const string CatalogCacheKey = "storefront:catalog:v1";
        private static readonly TimeSpan CatalogCacheTtl = TimeSpan.FromMinutes(5);

        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            // Trang chủ là trang có traffic cao nhất: cache 3 bảng danh mục (hầu như không đổi)
            // và chỉ 4 query sản phẩm, tất cả AsNoTracking.
            var catalog = GetCatalog();

            ViewBag.Categories = catalog.Categories;
            ViewBag.Occasions = catalog.Occasions;
            ViewBag.FlowerTypes = catalog.FlowerTypes;

            ViewBag.BestSellers = _db.Products.AsNoTracking()
                .Include("Occasion")
                .Where(p => p.IsActive && p.IsBestSeller)
                .OrderByDescending(p => p.RatingAvg)
                .Take(8)
                .ToList();

            ViewBag.FlashSales = _db.Products.AsNoTracking()
                .Include("Occasion")
                .Where(p => p.IsActive && p.IsFlashSale)
                .OrderByDescending(p => p.SalePrice)
                .Take(4)
                .ToList();

            ViewBag.FeaturedFlowers = _db.Products.AsNoTracking()
                .Include("Occasion")
                .Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.ReviewCount)
                .Take(8)
                .ToList();

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        /// <summary>
        /// Xoá cache danh mục - gọi sau khi admin thêm/sửa/xoá Category/Occasion/FlowerType.
        /// </summary>
        public static void InvalidateCatalogCache()
        {
            MemoryCache.Default.Remove(CatalogCacheKey);
        }

        private static CatalogSnapshot GetCatalog()
        {
            var cached = MemoryCache.Default.Get(CatalogCacheKey) as CatalogSnapshot;
            if (cached != null)
            {
                return cached;
            }

            using (var db = new FlowerShopDbContext())
            {
                var snapshot = new CatalogSnapshot
                {
                    Categories = db.Categories.AsNoTracking()
                        .Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList(),
                    Occasions = db.Occasions.AsNoTracking()
                        .Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).ToList(),
                    FlowerTypes = db.FlowerTypes.AsNoTracking()
                        .Where(f => f.IsActive).ToList()
                };

                MemoryCache.Default.Set(CatalogCacheKey, snapshot,
                    new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(CatalogCacheTtl) });

                return snapshot;
            }
        }

        private class CatalogSnapshot
        {
            public List<Category> Categories { get; set; }
            public List<Occasion> Occasions { get; set; }
            public List<FlowerType> FlowerTypes { get; set; }
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
