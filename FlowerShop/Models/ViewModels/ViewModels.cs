using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FlowerShop.Models.Entities;

namespace FlowerShop.Models.ViewModels
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSlug { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CartAddonItem
    {
        public int AddonId { get; set; }
        public string AddonName { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public List<CartAddonItem> Addons { get; set; } = new List<CartAddonItem>();
        public GreetingCard SelectedCard { get; set; }
        public string CardMessage { get; set; }
        public Voucher AppliedVoucher { get; set; }

        public decimal SubTotal => System.Linq.Enumerable.Sum(Items, i => i.TotalPrice);
        public decimal AddonTotal => System.Linq.Enumerable.Sum(Addons, a => a.TotalPrice);

        public decimal DiscountAmount
        {
            get
            {
                if (AppliedVoucher == null) return 0;
                decimal total = SubTotal + AddonTotal;
                if (total < AppliedVoucher.MinOrderValue) return 0;

                decimal discount = 0;
                if (AppliedVoucher.DiscountPercent.HasValue)
                {
                    discount = total * (AppliedVoucher.DiscountPercent.Value / 100m);
                    if (AppliedVoucher.MaxDiscountAmount.HasValue && discount > AppliedVoucher.MaxDiscountAmount.Value)
                    {
                        discount = AppliedVoucher.MaxDiscountAmount.Value;
                    }
                }
                else if (AppliedVoucher.DiscountAmount.HasValue)
                {
                    discount = AppliedVoucher.DiscountAmount.Value;
                }
                return discount;
            }
        }

        public decimal ShippingFee => (SubTotal + AddonTotal >= 500000) ? 0 : 35000;
        public decimal TotalAmount => Math.Max(0, SubTotal + AddonTotal - DiscountAmount + ShippingFee);
        public int TotalItemsCount => System.Linq.Enumerable.Sum(Items, i => i.Quantity);
    }

    public class CheckoutViewModel
    {
        public ShoppingCart Cart { get; set; }

        // Người gửi
        [Required(ErrorMessage = "Vui lòng nhập họ tên người đặt hoa")]
        [Display(Name = "Họ và tên người đặt")]
        public string SenderName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người đặt")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SenderPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email nhận xác nhận đơn")]
        public string SenderEmail { get; set; }

        // Người nhận
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận hoa")]
        [Display(Name = "Họ và tên người nhận")]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại người nhận")]
        [Display(Name = "Số điện thoại người nhận")]
        public string RecipientPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hoa")]
        [Display(Name = "Địa chỉ giao hoa")]
        public string RecipientAddress { get; set; }

        [Display(Name = "Quận / Huyện")]
        public string District { get; set; }

        [Display(Name = "Tỉnh / Thành phố")]
        public string City { get; set; } = "Hồ Chí Minh";

        [Required(ErrorMessage = "Vui lòng chọn ngày giao")]
        [Display(Name = "Ngày giao hoa")]
        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Vui lòng chọn khung giờ giao")]
        [Display(Name = "Khung giờ giao")]
        public string DeliveryTimeSlot { get; set; } = "08:00 - 10:00";

        [Display(Name = "Tặng ẩn danh (Bảo mật thông tin người gửi)")]
        public bool IsAnonymous { get; set; } = false;

        public int? SelectedCardId { get; set; }

        [Display(Name = "Lời chúc ghi trên thiệp")]
        public string CardMessage { get; set; }

        [Display(Name = "Ghi chú đặc biệt cho nghệ nhân cắm hoa")]
        public string OrderNotes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "VNPAY"; // VNPAY, STRIPE, VIETQR, COD

        // Dành cho Stripe Card Element
        public string StripeToken { get; set; }
        public string StripePaymentMethodId { get; set; }

        public List<GreetingCard> AvailableCards { get; set; } = new List<GreetingCard>();
        public List<AddonItem> AvailableAddons { get; set; } = new List<AddonItem>();
    }

    public class ProductFilterViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Occasion> Occasions { get; set; } = new List<Occasion>();
        public List<FlowerType> FlowerTypes { get; set; } = new List<FlowerType>();

        public int? SelectedCategoryId { get; set; }
        public int? SelectedOccasionId { get; set; }
        public int? SelectedFlowerTypeId { get; set; }
        public string ColorTone { get; set; }
        public string PriceRange { get; set; } // under500, 500to1000, 1000to2000, over2000
        public string SortBy { get; set; } // newest, priceAsc, priceDesc, rating
        public string SearchQuery { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalProducts { get; set; }
    }

    public class CustomBouquetViewModel
    {
        public string WrapStyle { get; set; } // BoTron, BoDai, GioHoa, HopMica
        public int MainFlowerId { get; set; }
        public int MainFlowerQuantity { get; set; } = 19;
        public List<int> SelectedSubFlowers { get; set; } = new List<int>();
        public string PaperColor { get; set; } = "HongPastel";
        public string RibbonColor { get; set; } = "KemVang";
        public string GreetingMessage { get; set; }

        public List<Product> MainFlowers { get; set; } = new List<Product>();
        public List<Product> SubFlowers { get; set; } = new List<Product>();
    }

    public class OrderTrackingViewModel
    {
        public string OrderCode { get; set; }
        public string PhoneNumber { get; set; }
        public Order Order { get; set; }
        public bool IsSearched { get; set; } = false;
        public string ErrorMessage { get; set; }
    }

    public class ChatRequestViewModel
    {
        public string Message { get; set; }
        public string SessionId { get; set; }
        public string Intent { get; set; } // advice, love_letter, track_order
    }

    public class ChatResponseViewModel
    {
        public string Reply { get; set; }
        public List<ProductSuggestion> SuggestedProducts { get; set; } = new List<ProductSuggestion>();
        public OrderStatusSummary OrderSummary { get; set; }
    }

    public class ProductSuggestion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string Slug { get; set; }
    }

    public class OrderStatusSummary
    {
        public string OrderCode { get; set; }
        public string Status { get; set; }
        public string StatusName { get; set; }
        public string DeliveryTime { get; set; }
        public string ProofPhotoUrl { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int PendingOrdersCount { get; set; }
        public int UrgentDeliveriesCount { get; set; }
        public int TotalProductsCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<Product> TopProducts { get; set; } = new List<Product>();
    }
}
