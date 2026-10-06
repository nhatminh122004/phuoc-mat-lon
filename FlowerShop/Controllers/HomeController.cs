using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;

namespace FlowerShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            ViewBag.Categories = _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
            ViewBag.Occasions = _db.Occasions.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder).ToList();
            ViewBag.BestSellers = _db.Products.Where(p => p.IsActive && p.IsBestSeller).Take(8).ToList();
            ViewBag.FlashSales = _db.Products.Where(p => p.IsActive && p.IsFlashSale).Take(4).ToList();
            ViewBag.FeaturedFlowers = _db.Products.Where(p => p.IsActive && p.IsFeatured).Take(8).ToList();
            ViewBag.FlowerTypes = _db.FlowerTypes.Where(f => f.IsActive).ToList();

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
