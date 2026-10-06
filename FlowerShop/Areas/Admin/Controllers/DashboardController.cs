using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;

namespace FlowerShop.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var todayRevenue = _db.Orders
                .Where(o => o.CreatedAt >= today && o.PaymentStatus == "Paid")
                .Select(o => (decimal?)o.TotalAmount)
                .Sum() ?? 0;

            var monthRevenue = _db.Orders
                .Where(o => o.CreatedAt >= thisMonth && o.PaymentStatus == "Paid")
                .Select(o => (decimal?)o.TotalAmount)
                .Sum() ?? 0;

            var pendingOrders = _db.Orders.Count(o => o.OrderStatus == "Pending");
            var urgentDeliveries = _db.Orders.Count(o => o.DeliveryDate == today && (o.OrderStatus == "Confirmed" || o.OrderStatus == "Arranging"));

            var recentOrders = _db.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToList();

            var topProducts = _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.ReviewCount)
                .Take(5)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                PendingOrdersCount = pendingOrders,
                UrgentDeliveriesCount = urgentDeliveries,
                TotalProductsCount = _db.Products.Count(p => p.IsActive),
                RecentOrders = recentOrders,
                TopProducts = topProducts
            };

            return View(model);
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
