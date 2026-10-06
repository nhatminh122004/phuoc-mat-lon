using System.Linq;
using System.Web;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;

namespace FlowerShop.Services.Cart
{
    public class CartService
    {
        private const string CartSessionKey = "FlowerShop_SessionCart";

        public static ShoppingCart GetCart(HttpContextBase context)
        {
            var cart = context.Session[CartSessionKey] as ShoppingCart;
            if (cart == null)
            {
                cart = new ShoppingCart();
                context.Session[CartSessionKey] = cart;
            }
            return cart;
        }

        public static void AddProduct(HttpContextBase context, int productId, int quantity = 1)
        {
            var cart = GetCart(context);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                using (var db = new FlowerShopDbContext())
                {
                    var product = db.Products.Find(productId);
                    if (product != null)
                    {
                        cart.Items.Add(new CartItem
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductSlug = product.Slug,
                            ImageUrl = product.MainImageUrl,
                            UnitPrice = product.SalePrice ?? product.Price,
                            Quantity = quantity
                        });
                    }
                }
            }
            context.Session[CartSessionKey] = cart;
        }

        public static void UpdateQuantity(HttpContextBase context, int productId, int quantity)
        {
            var cart = GetCart(context);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
            context.Session[CartSessionKey] = cart;
        }

        public static void RemoveProduct(HttpContextBase context, int productId)
        {
            var cart = GetCart(context);
            cart.Items.RemoveAll(i => i.ProductId == productId);
            context.Session[CartSessionKey] = cart;
        }

        public static void AddAddon(HttpContextBase context, int addonId, int quantity = 1)
        {
            var cart = GetCart(context);
            var addon = cart.Addons.FirstOrDefault(a => a.AddonId == addonId);
            if (addon != null)
            {
                addon.Quantity += quantity;
            }
            else
            {
                using (var db = new FlowerShopDbContext())
                {
                    var addonEntity = db.AddonItems.Find(addonId);
                    if (addonEntity != null)
                    {
                        cart.Addons.Add(new CartAddonItem
                        {
                            AddonId = addonEntity.Id,
                            AddonName = addonEntity.Name,
                            ImageUrl = addonEntity.ImageUrl,
                            UnitPrice = addonEntity.Price,
                            Quantity = quantity
                        });
                    }
                }
            }
            context.Session[CartSessionKey] = cart;
        }

        public static void RemoveAddon(HttpContextBase context, int addonId)
        {
            var cart = GetCart(context);
            cart.Addons.RemoveAll(a => a.AddonId == addonId);
            context.Session[CartSessionKey] = cart;
        }

        public static bool ApplyVoucher(HttpContextBase context, string code, out string message)
        {
            var cart = GetCart(context);
            using (var db = new FlowerShopDbContext())
            {
                var voucher = db.Vouchers.FirstOrDefault(v => v.Code.Equals(code, System.StringComparison.OrdinalIgnoreCase) && v.IsActive);
                if (voucher == null)
                {
                    message = "Mã giảm giá không tồn tại hoặc đã hết hạn!";
                    return false;
                }
                if (cart.SubTotal + cart.AddonTotal < voucher.MinOrderValue)
                {
                    message = $"Mã này chỉ áp dụng cho đơn hàng từ {voucher.MinOrderValue:N0} đ trở lên!";
                    return false;
                }
                cart.AppliedVoucher = voucher;
                context.Session[CartSessionKey] = cart;
                message = "Áp dụng mã giảm giá thành công!";
                return true;
            }
        }

        public static void ClearCart(HttpContextBase context)
        {
            context.Session[CartSessionKey] = new ShoppingCart();
        }
    }
}
