using System.Web.Mvc;
using System.Web.Routing;

namespace FlowerShop
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route xem chi tiết hoa theo Slug: /hoa/{slug}
            routes.MapRoute(
                name: "ProductDetails",
                url: "hoa/{slug}",
                defaults: new { controller = "Product", action = "Details" },
                namespaces: new[] { "FlowerShop.Controllers" }
            );

            // Route danh mục hoa: /danh-muc/{slug}
            routes.MapRoute(
                name: "ProductCategory",
                url: "danh-muc/{slug}",
                defaults: new { controller = "Product", action = "Index" },
                namespaces: new[] { "FlowerShop.Controllers" }
            );

            // Route tra cứu đơn hàng: /tra-cuu-don-hang
            routes.MapRoute(
                name: "OrderTracking",
                url: "tra-cuu-don-hang",
                defaults: new { controller = "OrderTracking", action = "Index" },
                namespaces: new[] { "FlowerShop.Controllers" }
            );

            // Route tự cắm hoa: /tu-thiet-ke-bo-hoa
            routes.MapRoute(
                name: "CustomBouquet",
                url: "tu-thiet-ke-bo-hoa",
                defaults: new { controller = "CustomBouquet", action = "Index" },
                namespaces: new[] { "FlowerShop.Controllers" }
            );

            // Route mặc định
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "FlowerShop.Controllers" }
            );
        }
    }
}
