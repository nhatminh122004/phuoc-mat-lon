using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;

namespace FlowerShop.Areas.Admin.Controllers
{
    public class OrderController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string status, string search)
        {
            var query = _db.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderCode.Contains(search) 
                                      || o.SenderName.Contains(search) 
                                      || o.RecipientName.Contains(search) 
                                      || o.SenderPhone.Contains(search)
                                      || o.RecipientPhone.Contains(search));
            }

            var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
            ViewBag.CurrentStatus = status;
            ViewBag.SearchTerm = search;

            return View(orders);
        }

        public ActionResult Details(int id)
        {
            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .Include("PaymentTransactions")
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return HttpNotFound();

            return View(order);
        }

        [HttpPost]
        public ActionResult UpdateStatus(int id, string orderStatus, string paymentStatus, string proofPhotoUrl)
        {
            var order = _db.Orders.Find(id);
            if (order != null)
            {
                if (!string.IsNullOrEmpty(orderStatus))
                {
                    order.OrderStatus = orderStatus;
                }
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    order.PaymentStatus = paymentStatus;
                }
                if (!string.IsNullOrEmpty(proofPhotoUrl))
                {
                    order.ProofPhotoUrl = proofPhotoUrl.Trim();
                }
                order.UpdatedAt = DateTime.Now;
                _db.SaveChanges();
            }

            return RedirectToAction("Details", new { id });
        }

        public ActionResult PrintSlip(int id, bool hidePrice = false)
        {
            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .FirstOrDefault(o => o.Id == id);

            if (order == null) return HttpNotFound();

            ViewBag.HidePrice = hidePrice || order.IsAnonymous;
            return View(order);
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
