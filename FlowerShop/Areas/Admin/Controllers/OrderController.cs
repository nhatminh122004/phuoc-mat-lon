using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FlowerShop.Areas.Admin.Filters;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Email;
using FlowerShop.Services.Logging;
using FlowerShop.Services.Orders;
using FlowerShop.Services.Upload;

namespace FlowerShop.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class OrderController : Controller
    {
        private const int PageSize = 20;

        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string status, string search, int page = 1)
        {
            if (page < 1) page = 1;

            var query = _db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                string term = search.Trim();
                query = query.Where(o => o.OrderCode.Contains(term)
                                      || o.SenderName.Contains(term)
                                      || o.RecipientName.Contains(term)
                                      || o.SenderPhone.Contains(term)
                                      || o.RecipientPhone.Contains(term));
            }

            // Phân trang phía server: trước đây load toàn bộ đơn vào RAM, sẽ chết khi có vài
            // chục nghìn đơn. Đếm + lấy 1 trang = 2 query có index hỗ trợ.
            int totalOrders = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalOrders / PageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            ViewBag.CurrentStatus = status;
            ViewBag.SearchTerm = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.PageSize = PageSize;

            ViewBag.StatusCounts = _db.Orders.AsNoTracking()
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Status, x => x.Count);

            var orders = query
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(orders);
        }

        public ActionResult Details(int id)
        {
            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .Include("PaymentTransactions")
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(
            int id,
            string orderStatus,
            string paymentStatus,
            string proofPhotoUrl,
            IEnumerable<HttpPostedFileBase> proofPhotoFiles = null,
            HttpPostedFileBase proofPhotoFile = null,
            bool appendPhotos = false)
        {
            var order = _db.Orders.Include("OrderDetails").FirstOrDefault(o => o.Id == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            string previousStatus = order.OrderStatus;
            bool statusChanged = false;

            if (!string.IsNullOrEmpty(orderStatus) && order.OrderStatus != orderStatus)
            {
                if (!IsValidOrderStatus(orderStatus))
                {
                    TempData["ErrorMessage"] = "Trạng thái đơn không hợp lệ.";
                    return RedirectToAction("Details", new { id });
                }

                order.OrderStatus = orderStatus;
                statusChanged = true;
            }

            if (!string.IsNullOrEmpty(paymentStatus) && order.PaymentStatus != paymentStatus)
            {
                order.PaymentStatus = paymentStatus;
            }

            var filesToProcess = new List<HttpPostedFileBase>();
            if (proofPhotoFiles != null)
            {
                filesToProcess.AddRange(proofPhotoFiles.Where(f => f != null && f.ContentLength > 0));
            }
            if (proofPhotoFile != null && proofPhotoFile.ContentLength > 0 && !filesToProcess.Contains(proofPhotoFile))
            {
                filesToProcess.Add(proofPhotoFile);
            }

            if (filesToProcess.Any())
            {
                List<string> uploadErrors;
                var uploadedUrls = FileUploadService.SaveMultipleImageFiles(filesToProcess, Server, "Proofs", out uploadErrors);

                if (uploadedUrls.Any())
                {
                    if (appendPhotos && !string.IsNullOrWhiteSpace(order.ProofPhotoUrl))
                    {
                        var combined = FileUploadService.SplitImageUrls(order.ProofPhotoUrl);
                        combined.AddRange(uploadedUrls);
                        order.ProofPhotoUrl = FileUploadService.JoinImageUrls(combined);
                    }
                    else
                    {
                        order.ProofPhotoUrl = FileUploadService.JoinImageUrls(uploadedUrls);
                    }
                }

                if (uploadErrors.Any())
                {
                    TempData["ErrorMessage"] = string.Join(" | ", uploadErrors);
                }
            }
            else if (proofPhotoUrl != null)
            {
                var cleanUrls = FileUploadService.SplitImageUrls(proofPhotoUrl);
                order.ProofPhotoUrl = cleanUrls.Any() ? FileUploadService.JoinImageUrls(cleanUrls) : null;
            }

            // Huỷ đơn -> hoàn tồn kho (bản cũ không hoàn nên kho bị trừ vĩnh viễn).
            if (statusChanged && orderStatus == "Cancelled" && previousStatus != "Cancelled")
            {
                var adapter = new DbContextAdapter(_db);
                foreach (var detail in order.OrderDetails)
                {
                    adapter.IncreaseStock(detail.ProductId, detail.Quantity);
                }
            }

            order.UpdatedAt = DateTime.Now;
            _db.SaveChanges();

            if (statusChanged)
            {
                var emailService = EmailServiceFactory.Create(Server.MapPath("~/"));
                OrderNotificationService.SendStatusChanged(emailService, order, null, BuildTrackingUrl(order.OrderCode));
            }

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn hàng & bộ ảnh thực tế thành công!";
            return RedirectToAction("Details", new { id });
        }

        public ActionResult PrintSlip(int id, bool hidePrice = false)
        {
            var order = _db.Orders
                .Include("OrderDetails")
                .Include("OrderAddons")
                .AsNoTracking()
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            ViewBag.HidePrice = hidePrice || order.IsAnonymous;
            return View(order);
        }

        private static bool IsValidOrderStatus(string status)
        {
            switch (status)
            {
                case "Pending":
                case "Confirmed":
                case "Arranging":
                case "ProofReady":
                case "Shipping":
                case "Delivered":
                case "Cancelled":
                    return true;
                default:
                    return false;
            }
        }

        private string BuildTrackingUrl(string orderCode)
        {
            if (Request != null && Request.Url != null)
            {
                return string.Format("{0}://{1}/tra-cuu-don-hang?orderCode={2}",
                    Request.Url.Scheme, Request.Url.Authority, Server.UrlEncode(orderCode));
            }

            return "/tra-cuu-don-hang?orderCode=" + orderCode;
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
