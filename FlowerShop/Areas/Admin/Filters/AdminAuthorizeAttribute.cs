using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FlowerShop.DAL;

namespace FlowerShop.Areas.Admin.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var session = httpContext.Session;

            string userRole = session?["UserRole"] as string;

            // Khôi phục phiên từ Auth Cookie nếu Session vừa bị làm mới
            if (string.IsNullOrEmpty(userRole) && httpContext.User?.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                string identityName = httpContext.User.Identity.Name;
                if (!string.IsNullOrEmpty(identityName))
                {
                    try
                    {
                        using (var db = new FlowerShopDbContext())
                        {
                            var user = db.Users.FirstOrDefault(u =>
                                u.IsActive && (u.Username == identityName || u.Email == identityName));
                            if (user != null && session != null)
                            {
                                session["UserId"] = user.Id;
                                session["Username"] = user.Username;
                                session["FullName"] = user.FullName;
                                session["UserEmail"] = user.Email;
                                session["UserPhone"] = user.Phone;
                                session["UserRole"] = user.Role;
                                userRole = user.Role;
                            }
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu kết nối DB chưa sẵn sàng
                    }
                }
            }

            if (!string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (httpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.StatusCode = 403;
                    filterContext.Result = new JsonResult
                    {
                        Data = new { success = false, message = "Bạn không có quyền truy cập khu vực Quản trị (Admin)." },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    string returnUrl = httpContext.Request.RawUrl ?? "/Admin/Dashboard";
                    filterContext.Controller.TempData["AuthError"] = "Vui lòng đăng nhập bằng tài khoản Quản Trị Viên (Admin) để truy cập trang quản lý.";
                    filterContext.Result = new RedirectResult($"/Account/Login?returnUrl={HttpUtility.UrlEncode(returnUrl)}");
                }
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
