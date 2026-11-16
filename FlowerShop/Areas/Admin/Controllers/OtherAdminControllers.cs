using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using FlowerShop.Areas.Admin.Filters;
using FlowerShop.DAL;
using FlowerShop.Helpers;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Logging;
using FlowerShop.Services.Upload;

namespace FlowerShop.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class ProductController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index(string search, int? categoryId)
        {
            var query = _db.Products.AsNoTracking().Include("Category").Include("Occasion").Include("FlowerType").AsQueryable();

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
        public ActionResult Create(Product product, IEnumerable<HttpPostedFileBase> imageFiles = null, HttpPostedFileBase mainImageFile = null)
        {
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // Slug do server sinh từ Name. Nếu để [Required] chặn thì form Create
            // (không có ô Slug) sẽ luôn invalid -> không bao giờ ghi được vào DB.
            ModelState.Remove("Slug");
            ModelState.Remove("Category");
            ModelState.Remove("Occasion");
            ModelState.Remove("FlowerType");

            product.Name = (product.Name ?? string.Empty).Trim();
            product.SKU = string.IsNullOrWhiteSpace(product.SKU) ? null : product.SKU.Trim();

            if (!string.IsNullOrWhiteSpace(product.Name) && string.IsNullOrWhiteSpace(product.Slug))
            {
                product.Slug = BuildUniqueSlug(SlugHelper.ToSlug(product.Name), null);
            }

            var filesToProcess = new List<HttpPostedFileBase>();
            if (imageFiles != null)
            {
                filesToProcess.AddRange(imageFiles.Where(f => f != null && f.ContentLength > 0));
            }
            if (mainImageFile != null && mainImageFile.ContentLength > 0 && !filesToProcess.Contains(mainImageFile))
            {
                filesToProcess.Add(mainImageFile);
            }

            if (filesToProcess.Any())
            {
                var uploadedUrls = FileUploadService.SaveMultipleImageFiles(filesToProcess, Server, "Products", out var uploadErrors);
                if (uploadedUrls.Any())
                {
                    if (string.IsNullOrWhiteSpace(product.MainImageUrl))
                    {
                        product.MainImageUrl = uploadedUrls[0];
                        ModelState.Remove("MainImageUrl");

                        int orderIdx = 1;
                        foreach (var extraUrl in uploadedUrls.Skip(1))
                        {
                            product.Images.Add(new ProductImage { ImageUrl = extraUrl, DisplayOrder = orderIdx++ });
                        }
                    }
                    else
                    {
                        int orderIdx = 1;
                        foreach (var extraUrl in uploadedUrls)
                        {
                            product.Images.Add(new ProductImage { ImageUrl = extraUrl, DisplayOrder = orderIdx++ });
                        }
                    }
                }

                if (uploadErrors.Any())
                {
                    ModelState.AddModelError("MainImageUrl", string.Join(" | ", uploadErrors));
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    product.CreatedAt = DateTime.Now;
                    _db.Products.Add(product);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã thêm mẫu hoa mới và bộ sưu tập ảnh thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Tạo sản phẩm thất bại: " + product.Name, ex);
                    ModelState.AddModelError(string.Empty, "Không lưu được vào cơ sở dữ liệu: " + ex.Message);
                }
            }

            TempData["ErrorMessage"] = string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception.Message : e.ErrorMessage));

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        /// <summary>Slug duy nhất: nếu trùng thì thêm hậu tố -2, -3...</summary>
        private string BuildUniqueSlug(string desiredSlug, int? excludeProductId)
        {
            string baseSlug = string.IsNullOrWhiteSpace(desiredSlug) ? "mau-hoa" : desiredSlug.Trim();
            string slug = baseSlug;
            int suffix = 2;

            while (_db.Products.Any(p => p.Slug == slug && (!excludeProductId.HasValue || p.Id != excludeProductId.Value)))
            {
                slug = baseSlug + "-" + suffix++;
            }

            return slug;
        }

        public ActionResult Edit(int id)
        {
            var product = _db.Products.Include("Images").FirstOrDefault(p => p.Id == id);
            if (product == null) return HttpNotFound();

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Product product, IEnumerable<HttpPostedFileBase> imageFiles = null, HttpPostedFileBase mainImageFile = null, bool setFirstAsMain = false)
        {
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // Slug: sinh lại từ Name nếu trống (bỏ ràng buộc [Required] để không chặn lưu)
            ModelState.Remove("Slug");
            ModelState.Remove("Category");
            ModelState.Remove("Occasion");
            ModelState.Remove("FlowerType");

            product.Name = (product.Name ?? string.Empty).Trim();
            product.SKU = string.IsNullOrWhiteSpace(product.SKU) ? null : product.SKU.Trim();

            if (string.IsNullOrWhiteSpace(product.Slug))
            {
                // Giữ slug cũ để không làm gãy URL đã chia sẻ; chỉ sinh mới khi SP chưa có slug.
                string currentSlug = _db.Products.AsNoTracking()
                    .Where(p => p.Id == product.Id)
                    .Select(p => p.Slug)
                    .FirstOrDefault();

                product.Slug = string.IsNullOrWhiteSpace(currentSlug)
                    ? BuildUniqueSlug(SlugHelper.ToSlug(product.Name), product.Id)
                    : currentSlug;
            }
            else
            {
                product.Slug = BuildUniqueSlug(SlugHelper.ToSlug(product.Slug), product.Id);
            }

            var filesToProcess = new List<HttpPostedFileBase>();
            if (imageFiles != null)
            {
                filesToProcess.AddRange(imageFiles.Where(f => f != null && f.ContentLength > 0));
            }
            if (mainImageFile != null && mainImageFile.ContentLength > 0 && !filesToProcess.Contains(mainImageFile))
            {
                filesToProcess.Add(mainImageFile);
            }

            var uploadedUrls = new List<string>();
            if (filesToProcess.Any())
            {
                uploadedUrls = FileUploadService.SaveMultipleImageFiles(filesToProcess, Server, "Products", out var uploadErrors);
                if (uploadedUrls.Any())
                {
                    ModelState.Remove("MainImageUrl");
                }
                if (uploadErrors.Any())
                {
                    ModelState.AddModelError("MainImageUrl", string.Join(" | ", uploadErrors));
                }
            }

            if (ModelState.IsValid)
            {
                var existing = _db.Products.Include("Images").FirstOrDefault(p => p.Id == product.Id);
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
                    if (!string.IsNullOrEmpty(product.MainImageUrl))
                    {
                        existing.MainImageUrl = product.MainImageUrl;
                    }
                    existing.IsFeatured = product.IsFeatured;
                    existing.IsBestSeller = product.IsBestSeller;
                    existing.IsFlashSale = product.IsFlashSale;
                    existing.IsActive = product.IsActive;

                    if (uploadedUrls.Any())
                    {
                        if (setFirstAsMain || string.IsNullOrWhiteSpace(existing.MainImageUrl))
                        {
                            existing.MainImageUrl = uploadedUrls[0];
                            int nextOrder = existing.Images.Any() ? existing.Images.Max(i => i.DisplayOrder) + 1 : 1;
                            foreach (var extraUrl in uploadedUrls.Skip(1))
                            {
                                _db.ProductImages.Add(new ProductImage
                                {
                                    ProductId = existing.Id,
                                    ImageUrl = extraUrl,
                                    DisplayOrder = nextOrder++
                                });
                            }
                        }
                        else
                        {
                            int nextOrder = existing.Images.Any() ? existing.Images.Max(i => i.DisplayOrder) + 1 : 1;
                            foreach (var extraUrl in uploadedUrls)
                            {
                                _db.ProductImages.Add(new ProductImage
                                {
                                    ProductId = existing.Id,
                                    ImageUrl = extraUrl,
                                    DisplayOrder = nextOrder++
                                });
                            }
                        }
                    }

                    _db.SaveChanges();
                    TempData["SuccessMessage"] = "Đã cập nhật mẫu hoa & bộ sưu tập ảnh thành công!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Categories = new SelectList(_db.Categories.Where(c => c.IsActive), "Id", "Name", product.CategoryId);
            ViewBag.Occasions = new SelectList(_db.Occasions.Where(o => o.IsActive), "Id", "Name", product.OccasionId);
            ViewBag.FlowerTypes = new SelectList(_db.FlowerTypes.Where(f => f.IsActive), "Id", "Name", product.FlowerTypeId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteImage(int imageId, int productId)
        {
            var img = _db.ProductImages.Find(imageId);
            if (img != null)
            {
                _db.ProductImages.Remove(img);
                _db.SaveChanges();
            }
            return RedirectToAction("Edit", new { id = productId });
        }

        /// <summary>
        /// GET cùng URL: không đổi dữ liệu, chỉ quay về danh sách kèm hướng dẫn.
        /// (Nhờ vậy bookmark/gõ tay/HTML cache cũ không còn trả 404.)
        /// </summary>
        [HttpGet]
        [ActionName("ToggleActive")]
        public ActionResult ToggleActiveView(int id = 0)
        {
            TempData["ErrorMessage"] = "Thao tác Ẩn/Hiện phải bấm từ danh sách mẫu hoa. Hãy tải lại trang (Ctrl+F5) rồi thử lại.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ToggleActive")]
        public ActionResult ToggleActivePost(int id)
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

        [AdminAuthorize]
    public class VoucherController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        public ActionResult Index()
        {
            var list = _db.Vouchers
                .OrderByDescending(v => v.IsActive)
                .ThenByDescending(v => v.Id)
                .ToList();

            return View(list);
        }

        // ------------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------------

        public ActionResult Create()
        {
            return View(new Voucher
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                UsageLimit = 1000,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Voucher voucher)
        {
            if (voucher == null)
            {
                return RedirectToAction("Index");
            }

            NormalizeVoucher(voucher);
            ValidateVoucher(voucher, null);

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Vouchers.Add(voucher);
                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã tạo mã voucher " + voucher.Code + " thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Tạo voucher thất bại: " + voucher.Code, ex);
                    ModelState.AddModelError(string.Empty, "Không lưu được vào cơ sở dữ liệu: " + ex.Message);
                }
            }

            TempData["ErrorMessage"] = JoinErrors();
            return View(voucher);
        }

        // ------------------------------------------------------------------
        // EDIT
        // ------------------------------------------------------------------

        public ActionResult Edit(int id = 0)
        {
            var voucher = _db.Vouchers.FirstOrDefault(v => v.Id == id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher cần sửa.";
                return RedirectToAction("Index");
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Voucher voucher)
        {
            if (voucher == null)
            {
                return RedirectToAction("Index");
            }

            NormalizeVoucher(voucher);

            var existing = _db.Vouchers.FirstOrDefault(v => v.Id == voucher.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Voucher không còn tồn tại (có thể đã bị xoá).";
                return RedirectToAction("Index");
            }

            ValidateVoucher(voucher, voucher.Id);

            if (ModelState.IsValid)
            {
                try
                {
                    existing.Code = voucher.Code;
                    existing.Description = voucher.Description;
                    existing.DiscountPercent = voucher.DiscountPercent;
                    existing.DiscountAmount = voucher.DiscountAmount;
                    existing.MinOrderValue = voucher.MinOrderValue;
                    existing.MaxDiscountAmount = voucher.MaxDiscountAmount;
                    existing.StartDate = voucher.StartDate;
                    existing.EndDate = voucher.EndDate;
                    existing.UsageLimit = voucher.UsageLimit;
                    existing.IsActive = voucher.IsActive;

                    _db.SaveChanges();

                    TempData["SuccessMessage"] = "Đã cập nhật voucher " + existing.Code + ".";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    AppLogger.Error("Cập nhật voucher thất bại: " + voucher.Code, ex);
                    ModelState.AddModelError(string.Empty, "Không lưu được vào cơ sở dữ liệu: " + ex.Message);
                }
            }

            TempData["ErrorMessage"] = JoinErrors();
            return View(voucher);
        }

        // ------------------------------------------------------------------
        // TOGGLE ACTIVE
        // ------------------------------------------------------------------

        /// <summary>GET cùng URL: không đổi dữ liệu (tránh 404 khi mở trực tiếp, tránh CSRF).</summary>
        [HttpGet]
        [ActionName("ToggleActive")]
        public ActionResult ToggleActiveView(int id = 0)
        {
            TempData["ErrorMessage"] = "Thao tác Bật/Tắt phải bấm từ danh sách voucher. Hãy tải lại trang (Ctrl+F5).";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ToggleActive")]
        public ActionResult ToggleActivePost(int id)
        {
            var voucher = _db.Vouchers.FirstOrDefault(v => v.Id == id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                return RedirectToAction("Index");
            }

            voucher.IsActive = !voucher.IsActive;
            _db.SaveChanges();

            TempData["SuccessMessage"] = (voucher.IsActive ? "Đã bật" : "Đã tạm dừng") + " voucher " + voucher.Code + ".";
            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------------

        [HttpGet]
        [ActionName("Delete")]
        public ActionResult DeleteView(int id = 0)
        {
            TempData["ErrorMessage"] = "Thao tác Xoá phải bấm từ danh sách voucher. Hãy tải lại trang (Ctrl+F5).";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public ActionResult DeletePost(int id)
        {
            var voucher = _db.Vouchers.FirstOrDefault(v => v.Id == id);
            if (voucher == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy voucher để xoá.";
                return RedirectToAction("Index");
            }

            // Voucher đã dùng cho đơn hàng thì chỉ tạm dừng, không xoá cứng
            // (giữ lịch sử đơn và tránh mất dữ liệu đối soát).
            bool used = _db.Orders.Any(o => o.VoucherCode == voucher.Code);

            if (used)
            {
                voucher.IsActive = false;
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Voucher " + voucher.Code + " đã được dùng trong đơn hàng nên chỉ tạm dừng (không xoá).";
                return RedirectToAction("Index");
            }

            try
            {
                _db.Vouchers.Remove(voucher);
                _db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xoá voucher " + voucher.Code + ".";
            }
            catch (Exception ex)
            {
                AppLogger.Error("Xoá voucher thất bại: " + voucher.Code, ex);
                TempData["ErrorMessage"] = "Không xoá được voucher: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ------------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------------

        private static void NormalizeVoucher(Voucher voucher)
        {
            voucher.Code = (voucher.Code ?? string.Empty).Trim().ToUpperInvariant();
            voucher.Description = string.IsNullOrWhiteSpace(voucher.Description) ? null : voucher.Description.Trim();

            if (voucher.UsageLimit < 1)
            {
                voucher.UsageLimit = 1000;
            }
        }

        private void ValidateVoucher(Voucher voucher, int? excludeId)
        {
            if (string.IsNullOrWhiteSpace(voucher.Code))
            {
                ModelState.AddModelError("Code", "Vui lòng nhập mã voucher.");
            }
            else if (_db.Vouchers.Any(v => v.Code == voucher.Code && (!excludeId.HasValue || v.Id != excludeId.Value)))
            {
                ModelState.AddModelError("Code", "Mã voucher này đã tồn tại, vui lòng chọn mã khác.");
            }

            if (!voucher.DiscountPercent.HasValue && !voucher.DiscountAmount.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập % giảm hoặc số tiền giảm cụ thể.");
            }

            if (voucher.DiscountPercent.HasValue && (voucher.DiscountPercent < 1 || voucher.DiscountPercent > 100))
            {
                ModelState.AddModelError("DiscountPercent", "Phần trăm giảm phải trong khoảng 1 - 100.");
            }

            if (voucher.StartDate.HasValue && voucher.EndDate.HasValue && voucher.EndDate < voucher.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
            }
        }

        private string JoinErrors()
        {
            return string.Join(" | ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception.Message : e.ErrorMessage));
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

[AdminAuthorize]
    public class SettingsController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            var model = new SystemSettingsViewModel
            {
                SePayEnv = ConfigurationManager.AppSettings["SePay:Env"] ?? "sandbox",
                SePayMode = ConfigurationManager.AppSettings["SePay:Mode"] ?? "gateway",
                SePayMerchantId = ConfigurationManager.AppSettings["SePay:MerchantId"] ?? "",
                SePaySecretKey = ConfigurationManager.AppSettings["SePay:SecretKey"] ?? "",
                SePayIpnSecretKey = ConfigurationManager.AppSettings["SePay:IpnSecretKey"] ?? "",
                SePayPaymentMethod = ConfigurationManager.AppSettings["SePay:PaymentMethod"] ?? "BANK_TRANSFER",
                SePayBank = ConfigurationManager.AppSettings["SePay:Bank"] ?? "MBBank",
                SePayAccountNumber = ConfigurationManager.AppSettings["SePay:AccountNumber"] ?? "",
                SePayAccountHolder = ConfigurationManager.AppSettings["SePay:AccountHolder"] ?? "TIEM HOA HUU PHUOC",
                SePayWebhookHmacSecret = ConfigurationManager.AppSettings["SePay:WebhookHmacSecret"] ?? "",
                SePayWebhookApiKey = ConfigurationManager.AppSettings["SePay:WebhookApiKey"] ?? "",
                SePayPublicBaseUrl = ConfigurationManager.AppSettings["SePay:PublicBaseUrl"] ?? "",
                ChatbotFloristName = ConfigurationManager.AppSettings["Chatbot:FloristName"] ?? "Hương Lan",
                ChatbotGeminiApiKey = ConfigurationManager.AppSettings["Chatbot:GeminiApiKey"] ?? "",

                // Email / SMTP: để trống Host = ghi mail ra App_Data/emails (dev Laragon)
                SmtpHost = ConfigurationManager.AppSettings["Smtp:Host"] ?? "",
                SmtpPort = ConfigurationManager.AppSettings["Smtp:Port"] ?? "1025",
                SmtpUser = ConfigurationManager.AppSettings["Smtp:User"] ?? "",
                SmtpPassword = ConfigurationManager.AppSettings["Smtp:Password"] ?? "",
                SmtpFrom = ConfigurationManager.AppSettings["Smtp:From"] ?? "no-reply@huuphuocflorist.vn",
                SmtpFromName = ConfigurationManager.AppSettings["Smtp:FromName"] ?? "Hữu Phước Florist"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveSettings(SystemSettingsViewModel model)
        {
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
                SetOrUpdateSetting(config, "SePay:Env", model.SePayEnv);
                SetOrUpdateSetting(config, "SePay:Mode", model.SePayMode);
                SetOrUpdateSetting(config, "SePay:MerchantId", model.SePayMerchantId);
                SetOrUpdateSetting(config, "SePay:SecretKey", model.SePaySecretKey);
                SetOrUpdateSetting(config, "SePay:IpnSecretKey", model.SePayIpnSecretKey);
                SetOrUpdateSetting(config, "SePay:PaymentMethod", model.SePayPaymentMethod);
                SetOrUpdateSetting(config, "SePay:Bank", model.SePayBank);
                SetOrUpdateSetting(config, "SePay:AccountNumber", model.SePayAccountNumber);
                SetOrUpdateSetting(config, "SePay:AccountHolder", model.SePayAccountHolder);
                SetOrUpdateSetting(config, "SePay:WebhookHmacSecret", model.SePayWebhookHmacSecret);
                SetOrUpdateSetting(config, "SePay:WebhookApiKey", model.SePayWebhookApiKey);
                SetOrUpdateSetting(config, "SePay:PublicBaseUrl", model.SePayPublicBaseUrl);

                SetOrUpdateSetting(config, "Chatbot:FloristName", model.ChatbotFloristName);
                SetOrUpdateSetting(config, "Chatbot:GeminiApiKey", model.ChatbotGeminiApiKey);

                SetOrUpdateSetting(config, "Smtp:Host", model.SmtpHost);
                SetOrUpdateSetting(config, "Smtp:Port", model.SmtpPort);
                SetOrUpdateSetting(config, "Smtp:User", model.SmtpUser);
                SetOrUpdateSetting(config, "Smtp:Password", model.SmtpPassword);
                SetOrUpdateSetting(config, "Smtp:From", model.SmtpFrom);
                SetOrUpdateSetting(config, "Smtp:FromName", model.SmtpFromName);

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                FlowerShop.Controllers.HomeController.InvalidateCatalogCache();

                TempData["SuccessMessage"] = "Đã lưu cấu hình hệ thống & cổng thanh toán thành công!";
            }
            catch (Exception ex)
            {
                // Fallback cập nhật in-memory nếu file Web.config đang bị khóa ghi
                ConfigurationManager.AppSettings["SePay:Env"] = model.SePayEnv;
                ConfigurationManager.AppSettings["SePay:Mode"] = model.SePayMode;
                ConfigurationManager.AppSettings["SePay:MerchantId"] = model.SePayMerchantId;
                ConfigurationManager.AppSettings["SePay:SecretKey"] = model.SePaySecretKey;
                ConfigurationManager.AppSettings["SePay:IpnSecretKey"] = model.SePayIpnSecretKey;
                ConfigurationManager.AppSettings["SePay:PaymentMethod"] = model.SePayPaymentMethod;
                ConfigurationManager.AppSettings["SePay:Bank"] = model.SePayBank;
                ConfigurationManager.AppSettings["SePay:AccountNumber"] = model.SePayAccountNumber;
                ConfigurationManager.AppSettings["SePay:AccountHolder"] = model.SePayAccountHolder;
                ConfigurationManager.AppSettings["SePay:WebhookHmacSecret"] = model.SePayWebhookHmacSecret;
                ConfigurationManager.AppSettings["SePay:WebhookApiKey"] = model.SePayWebhookApiKey;
                ConfigurationManager.AppSettings["SePay:PublicBaseUrl"] = model.SePayPublicBaseUrl;
                ConfigurationManager.AppSettings["Chatbot:FloristName"] = model.ChatbotFloristName;
                ConfigurationManager.AppSettings["Chatbot:GeminiApiKey"] = model.ChatbotGeminiApiKey;

                ConfigurationManager.AppSettings["Smtp:Host"] = model.SmtpHost;
                ConfigurationManager.AppSettings["Smtp:Port"] = model.SmtpPort;
                ConfigurationManager.AppSettings["Smtp:User"] = model.SmtpUser;
                ConfigurationManager.AppSettings["Smtp:Password"] = model.SmtpPassword;
                ConfigurationManager.AppSettings["Smtp:From"] = model.SmtpFrom;
                ConfigurationManager.AppSettings["Smtp:FromName"] = model.SmtpFromName;

                TempData["SuccessMessage"] = "Đã lưu cấu hình cho phiên chạy hiện tại (" + ex.Message + ").";
            }

            return RedirectToAction("Index");
        }

        private static void SetOrUpdateSetting(Configuration config, string key, string value)
        {
            var settings = config.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value ?? "");
            }
            else
            {
                settings[key].Value = value ?? "";
            }
        }
    }
}
