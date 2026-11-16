using System;
using System.Linq;
using System.Web;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Pricing;

namespace FlowerShop.Services.Cart
{
    /// <summary>
    /// Giỏ hàng lưu trong Session (khách vãng lai vẫn đặt được).
    /// Toàn bộ phép tính tiền do <see cref="PricingService"/> quyết định để BE/FE không lệch nhau.
    /// </summary>
    public class CartService
    {
        private const string CartSessionKey = "FlowerShop_SessionCart";
        private const int MaxQuantityPerProduct = 50;

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
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var cart = GetCart(context);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity = Math.Min(item.Quantity + quantity, MaxQuantityPerProduct);
            }
            else
            {
                using (var db = new FlowerShopDbContext())
                {
                    var product = db.Products.AsNoTracking()
                        .Where(p => p.Id == productId && p.IsActive)
                        .Select(p => new { p.Id, p.Name, p.Slug, p.MainImageUrl, p.Price, p.SalePrice })
                        .FirstOrDefault();

                    if (product != null)
                    {
                        cart.Items.Add(new CartItem
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductSlug = product.Slug,
                            ImageUrl = product.MainImageUrl,
                            UnitPrice = product.SalePrice ?? product.Price,
                            Quantity = Math.Min(quantity, MaxQuantityPerProduct)
                        });
                    }
                }
            }
            context.Session[CartSessionKey] = cart;
        }

        /// <summary>Thêm dòng hàng đã tính giá sẵn (bó hoa tự thiết kế, combo...).</summary>
        public static void AddCustomLine(HttpContextBase context, CartItem item)
        {
            if (item == null)
            {
                return;
            }

            var cart = GetCart(context);
            cart.Items.Add(item);
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
                    item.Quantity = Math.Min(quantity, MaxQuantityPerProduct);
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
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var cart = GetCart(context);
            var addon = cart.Addons.FirstOrDefault(a => a.AddonId == addonId);
            if (addon != null)
            {
                addon.Quantity = Math.Min(addon.Quantity + quantity, MaxQuantityPerProduct);
            }
            else
            {
                using (var db = new FlowerShopDbContext())
                {
                    var addonEntity = db.AddonItems.AsNoTracking()
                        .Where(a => a.Id == addonId && a.IsActive)
                        .Select(a => new { a.Id, a.Name, a.ImageUrl, a.Price })
                        .FirstOrDefault();

                    if (addonEntity != null)
                    {
                        cart.Addons.Add(new CartAddonItem
                        {
                            AddonId = addonEntity.Id,
                            AddonName = addonEntity.Name,
                            ImageUrl = addonEntity.ImageUrl,
                            UnitPrice = addonEntity.Price,
                            Quantity = Math.Min(quantity, MaxQuantityPerProduct)
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

            if (string.IsNullOrWhiteSpace(code))
            {
                cart.AppliedVoucher = null;
                context.Session[CartSessionKey] = cart;
                message = "Đã gỡ mã giảm giá.";
                return true;
            }

            using (var db = new FlowerShopDbContext())
            {
                string cleanCode = code.Trim();

                var voucher = db.Vouchers.AsNoTracking()
                    .FirstOrDefault(v => v.Code == cleanCode);

                var evaluation = PricingService.EvaluateVoucher(voucher, cart.MerchandiseSubtotal, DateTime.Now);

                if (!evaluation.IsValid)
                {
                    message = evaluation.Message;
                    return false;
                }

                cart.AppliedVoucher = voucher;
                context.Session[CartSessionKey] = cart;
                message = evaluation.Message;
                return true;
            }
        }

        public static void ClearCart(HttpContextBase context)
        {
            context.Session[CartSessionKey] = new ShoppingCart();
        }
    }
}
