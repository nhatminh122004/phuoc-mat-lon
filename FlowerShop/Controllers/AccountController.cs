using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Email;
using FlowerShop.Services.Security;

namespace FlowerShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly FlowerShopDbContext _db = new FlowerShopDbContext();

        // ------------------------------------------------------------------
        // ĐĂNG NHẬP
        // ------------------------------------------------------------------

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (TempData["AuthError"] != null)
            {
                ViewBag.AuthError = TempData["AuthError"].ToString();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password, bool rememberMe = true, string returnUrl = null)
        {
            string identifier = (email ?? "").Trim();

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                return LoginFailure("Vui lòng nhập đầy đủ Email/Tên đăng nhập và mật khẩu.", returnUrl);
            }

            string throttleKey = LoginThrottle.BuildKey(GetClientIp(), identifier);
            DateTime nowUtc = DateTime.UtcNow;

            if (LoginThrottle.IsBlocked(throttleKey, nowUtc))
            {
                return LoginFailure(
                    "Tài khoản tạm thời bị khoá do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.",
                    returnUrl);
            }

            try
            {
                string lowerId = identifier.ToLowerInvariant();
                var user = _db.Users.FirstOrDefault(u =>
                    u.IsActive &&
                    (u.Email.ToLower() == lowerId || u.Username.ToLower() == lowerId));

                if (user == null || !PasswordService.Verify(password, user.PasswordHash))
                {
                    LoginThrottle.RegisterFailure(throttleKey, nowUtc);
                    int remaining = LoginThrottle.GetRemainingAttempts(throttleKey, nowUtc);

                    string message = remaining > 0
                        ? string.Format("Tài khoản hoặc mật khẩu không chính xác. Bạn còn {0} lần thử.", remaining)
                        : "Bạn đã nhập sai quá 5 lần. Tài khoản tạm khoá 15 phút.";

                    return LoginFailure(message, returnUrl);
                }

                // Nâng cấp hash cũ (SHA-256 / plaintext của dữ liệu seed) lên BCrypt.
                if (PasswordService.NeedsRehash(user.PasswordHash))
                {
                    user.PasswordHash = PasswordService.Hash(password);
                    _db.SaveChanges();
                }

                LoginThrottle.Reset(throttleKey);
                SetUserSession(user, rememberMe);

                string targetUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? "/Admin/Dashboard" : "/");

                if (IsJsonRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công!",
                        redirectUrl = targetUrl,
                        user = new
                        {
                            id = user.Id,
                            username = user.Username,
                            name = user.FullName ?? user.Username,
                            email = user.Email,
                            phone = user.Phone,
                            role = user.Role
                        }
                    });
                }

                return Redirect(targetUrl);
            }
            catch (Exception ex)
            {
                return LoginFailure("Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau. (" + ex.Message + ")", returnUrl);
            }
        }

        // ------------------------------------------------------------------
        // ĐĂNG KÝ
        // ------------------------------------------------------------------

        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string fullName, string email, string phone, string password, string confirmPassword)
        {
            fullName = (fullName ?? "").Trim();
            email = (email ?? "").Trim();
            phone = (phone ?? "").Trim();

            if (string.IsNullOrEmpty(fullName))
                return RegisterFailure("Vui lòng nhập họ và tên của bạn.");

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return RegisterFailure("Địa chỉ email không hợp lệ.");

            var passwordCheck = PasswordService.Validate(password, confirmPassword);
            if (!passwordCheck.IsValid)
                return RegisterFailure(passwordCheck.Message);

            try
            {
                string lowerEmail = email.ToLowerInvariant();
                bool exists = _db.Users.Any(u => u.Email.ToLower() == lowerEmail || u.Username.ToLower() == lowerEmail);
                if (exists)
                {
                    return RegisterFailure("Địa chỉ email này đã được đăng ký. Vui lòng đăng nhập hoặc dùng email khác.");
                }

                string baseUsername = email.Split('@')[0].ToLowerInvariant();
                string username = baseUsername;
                int suffix = 1;
                while (_db.Users.Any(u => u.Username.ToLower() == username))
                {
                    username = baseUsername + (suffix++);
                }

                var newUser = new User
                {
                    Username = username,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    PasswordHash = PasswordService.Hash(password),
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                SetUserSession(newUser, true);

                if (IsJsonRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = "Tạo tài khoản thành công!",
                        redirectUrl = "/",
                        user = new
                        {
                            id = newUser.Id,
                            username = newUser.Username,
                            name = newUser.FullName,
                            email = newUser.Email,
                            phone = newUser.Phone,
                            role = newUser.Role
                        }
                    });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                return RegisterFailure("Không thể tạo tài khoản: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // QUÊN MẬT KHẨU (bước 1: gửi link) + ĐẶT LẠI (bước 2: dùng token)
        // ------------------------------------------------------------------

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            var emailService = EmailServiceFactory.Create(Server.MapPath("~/"));

            var outcome = PasswordResetService.RequestReset(
                _db,
                emailService,
                email,
                GetClientIp(),
                token => BuildAbsoluteResetUrl(token),
                DateTime.Now);

            if (IsJsonRequest())
            {
                return Json(new
                {
                    success = outcome.Success,
                    message = outcome.Message,
                    emailDelivered = outcome.EmailDelivered,
                    devResetUrl = outcome.DevResetUrl
                });
            }

            ViewBag.OutcomeMessage = outcome.Message;
            ViewBag.OutcomeSuccess = outcome.Success;
            ViewBag.DevResetUrl = outcome.DevResetUrl;

            return View(new ForgotPasswordViewModel { Email = email });
        }

        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.ErrorMessage = "Link đặt lại mật khẩu không hợp lệ.";
                return View(new ResetPasswordViewModel());
            }

            if (!PasswordResetService.IsTokenUsable(_db, token, DateTime.Now))
            {
                ViewBag.ErrorMessage = "Link đặt lại mật khẩu không tồn tại, đã được sử dụng hoặc đã hết hạn.";
                return View(new ResetPasswordViewModel());
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Token))
            {
                ViewBag.ErrorMessage = "Link đặt lại mật khẩu không hợp lệ.";
                return View(new ResetPasswordViewModel());
            }

            if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không trùng khớp.";
                return View(model);
            }

            var outcome = PasswordResetService.CompleteReset(_db, model.Token, model.Password, DateTime.Now);

            if (!outcome.Success)
            {
                ViewBag.ErrorMessage = outcome.Message;
                return View(model);
            }

            TempData["AuthSuccess"] = outcome.Message;
            return RedirectToAction("Login");
        }

        // ------------------------------------------------------------------
        // ĐĂNG XUẤT / PHIÊN HIỆN TẠI
        // ------------------------------------------------------------------

        [HttpGet]
        public ActionResult Logout()
        {
            ClearUserSession();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogoutAjax()
        {
            ClearUserSession();
            return Json(new { success = true, message = "Đã đăng xuất tài khoản thành công!" });
        }

        [HttpGet]
        public JsonResult CurrentUser()
        {
            if (Session["UserId"] != null)
            {
                return Json(new
                {
                    authenticated = true,
                    user = new
                    {
                        id = Session["UserId"],
                        username = Session["Username"],
                        name = Session["FullName"],
                        email = Session["UserEmail"],
                        phone = Session["UserPhone"],
                        role = Session["UserRole"]
                    }
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { authenticated = false }, JsonRequestBehavior.AllowGet);
        }

        // ------------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------------

        private string BuildAbsoluteResetUrl(string rawToken)
        {
            string path = Url.Action("ResetPassword", "Account", new { token = rawToken });

            if (Request != null && Request.Url != null)
            {
                return string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, path);
            }

            return path;
        }

        private void SetUserSession(User user, bool rememberMe)
        {
            Session["UserId"] = user.Id;
            Session["Username"] = user.Username;
            Session["FullName"] = user.FullName ?? user.Username;
            Session["UserEmail"] = user.Email;
            Session["UserPhone"] = user.Phone;
            Session["UserRole"] = user.Role ?? "Customer";

            FormsAuthentication.SetAuthCookie(user.Username, rememberMe);
        }

        private void ClearUserSession()
        {
            Session.Remove("UserId");
            Session.Remove("Username");
            Session.Remove("FullName");
            Session.Remove("UserEmail");
            Session.Remove("UserPhone");
            Session.Remove("UserRole");
            FormsAuthentication.SignOut();
        }

        private string GetClientIp()
        {
            try
            {
                string forwarded = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrEmpty(forwarded))
                {
                    return forwarded.Split(',')[0].Trim();
                }

                return Request.ServerVariables["REMOTE_ADDR"] ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private bool IsJsonRequest()
        {
            if (Request == null)
            {
                return false;
            }

            if (Request.IsAjaxRequest())
            {
                return true;
            }

            var accept = Request.AcceptTypes;
            if (accept == null)
            {
                return false;
            }

            foreach (string type in accept)
            {
                if (!string.IsNullOrEmpty(type) && type.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private ActionResult LoginFailure(string message, string returnUrl)
        {
            if (IsJsonRequest())
            {
                return Json(new { success = false, message = message });
            }

            ViewBag.AuthError = message;
            ViewBag.ReturnUrl = returnUrl;
            return View("Login");
        }

        private ActionResult RegisterFailure(string message)
        {
            if (IsJsonRequest())
            {
                return Json(new { success = false, message = message });
            }

            ViewBag.AuthError = message;
            return View("Register");
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
