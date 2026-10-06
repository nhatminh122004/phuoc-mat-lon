using System;
using System.Linq;
using System.Web.Mvc;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;

namespace FlowerShop.Controllers
{
    public class OrderTrackingController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string orderCode, string phone)
        {
            var model = new OrderTrackingViewModel
            {
                OrderCode = orderCode,
                PhoneNumber = phone
            };

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                model.IsSearched = true;
                string cleanCode = orderCode.Trim();

                var order = _db.Orders
                    .Include("OrderDetails")
                    .Include("OrderAddons")
                    .FirstOrDefault(o => o.OrderCode.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));

                if (order != null)
                {
                    // Nếu khách nhập thêm SĐT thì xác thực thêm
                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        string cleanPhone = phone.Trim();
                        if (order.SenderPhone != cleanPhone && order.RecipientPhone != cleanPhone)
                        {
                            model.ErrorMessage = "Số điện thoại không khớp với mã đơn hàng này!";
                            return View(model);
                        }
                    }
                    model.Order = order;
                }
                else
                {
                    model.ErrorMessage = "Không tìm thấy đơn hàng với mã đã nhập. Vui lòng kiểm tra lại mã đơn hàng trong tin nhắn / email.";
                }
            }

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
