using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Services.Cart;

namespace FlowerShop.Controllers
{
    public class CartController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var cart = CartService.GetCart(HttpContext);
            ViewBag.AvailableAddons = _db.AddonItems.Where(a => a.IsActive).ToList();
            ViewBag.AvailableCards = _db.GreetingCards.Where(c => c.IsActive).ToList();
            return View(cart);
        }

        [HttpPost]
        public ActionResult AddToCart(int productId, int quantity = 1, bool isAjax = false)
        {
            CartService.AddProduct(HttpContext, productId, quantity);
            if (isAjax)
            {
                var cart = CartService.GetCart(HttpContext);
                return Json(new
                {
                    success = true,
                    totalItems = cart.TotalItemsCount,
                    subTotal = cart.SubTotal.ToString("N0") + " đ"
                });
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateQuantity(int productId, int quantity)
        {
            CartService.UpdateQuantity(HttpContext, productId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult RemoveItem(int productId)
        {
            CartService.RemoveProduct(HttpContext, productId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult AddAddon(int addonId, int quantity = 1)
        {
            CartService.AddAddon(HttpContext, addonId, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult RemoveAddon(int addonId)
        {
            CartService.RemoveAddon(HttpContext, addonId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ApplyVoucher(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã ưu đãi!";
                return RedirectToAction("Index");
            }

            if (CartService.ApplyVoucher(HttpContext, code.Trim(), out string message))
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
