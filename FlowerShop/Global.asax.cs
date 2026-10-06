using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using FlowerShop.DAL;

namespace FlowerShop
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Đảm bảo DbContext và seed data được khởi tạo
            using (var context = new FlowerShopDbContext())
            {
                try
                {
                    var count = context.Products.Count();
                }
                catch
                {
                    // Chạy trong môi trường MySQL
                }
            }
        }
    }
}
