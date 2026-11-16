using System;
using FlowerShop.Models.Entities;

namespace FlowerShop.Services.Pricing
{
    public enum VoucherValidationStatus
    {
        Valid = 0,
        NotFound = 1,
        Inactive = 2,
        NotStarted = 3,
        Expired = 4,
        UsageLimitReached = 5,
        MinOrderNotMet = 6
    }

    public class VoucherEvaluation
    {
        public VoucherValidationStatus Status { get; set; }
        public string Message { get; set; }
        public decimal Discount { get; set; }

        public bool IsValid
        {
            get { return Status == VoucherValidationStatus.Valid; }
        }

        public VoucherEvaluation(VoucherValidationStatus status, string message, decimal discount = 0m)
        {
            Status = status;
            Message = message;
            Discount = discount < 0m ? 0m : discount;
        }
    }

    public class CartPricing
    {
        public decimal MerchandiseSubtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool FreeShipping { get; set; }
        public decimal AmountToFreeShipping { get; set; }
    }

    /// <summary>
    /// Nguồn sự thật duy nhất cho mọi phép tính tiền (giỏ hàng, checkout, JS hiển thị).
    /// Trước đây phí ship và công thức voucher bị lặp ở 3 nơi (ShoppingCart, CartService,
    /// custom-bouquet.js) nên rất dễ lệch. Toàn bộ logic ở đây là pure function -> unit test.
    /// </summary>
    public static class PricingService
    {
        /// <summary>Đơn từ 500.000đ được miễn phí giao trong nội thành.</summary>
        public const decimal FreeShippingThreshold = 500000m;

        public const decimal DefaultShippingFee = 35000m;

        /// <summary>Giá cố định cho khung giao hỏa tốc (dùng chung FE/BE).</summary>
        public const decimal ExpressSurcharge = 0m;

        public static decimal CalculateShippingFee(decimal merchandiseSubtotal)
        {
            if (merchandiseSubtotal <= 0m)
            {
                return 0m;
            }

            return merchandiseSubtotal >= FreeShippingThreshold ? 0m : DefaultShippingFee;
        }

        public static decimal AmountToFreeShipping(decimal merchandiseSubtotal)
        {
            if (merchandiseSubtotal >= FreeShippingThreshold)
            {
                return 0m;
            }

            decimal gap = FreeShippingThreshold - merchandiseSubtotal;
            return gap < 0m ? 0m : gap;
        }

        /// <summary>
        /// Giá trị giảm giá của voucher trên một giá trị hàng hoá (chưa gồm ship).
        /// Không kiểm tra điều kiện hiệu lực - dùng <see cref="EvaluateVoucher"/> cho luồng đầy đủ.
        /// </summary>
        public static decimal CalculateVoucherDiscount(Voucher voucher, decimal merchandiseSubtotal)
        {
            if (voucher == null || merchandiseSubtotal <= 0m)
            {
                return 0m;
            }

            if (merchandiseSubtotal < voucher.MinOrderValue)
            {
                return 0m;
            }

            decimal discount = 0m;

            if (voucher.DiscountPercent.HasValue)
            {
                decimal percent = voucher.DiscountPercent.Value;
                if (percent > 100m)
                {
                    percent = 100m;
                }

                discount = merchandiseSubtotal * (percent / 100m);

                if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                {
                    discount = voucher.MaxDiscountAmount.Value;
                }
            }
            else if (voucher.DiscountAmount.HasValue)
            {
                discount = voucher.DiscountAmount.Value;
            }

            if (discount > merchandiseSubtotal)
            {
                discount = merchandiseSubtotal;
            }

            return discount < 0m ? 0m : discount;
        }

        /// <summary>
        /// Kiểm tra đầy đủ: tồn tại, đang bật, trong khoảng thời gian, còn lượt, đủ đơn tối thiểu.
        /// </summary>
        public static VoucherEvaluation EvaluateVoucher(Voucher voucher, decimal merchandiseSubtotal, DateTime now)
        {
            if (voucher == null)
            {
                return new VoucherEvaluation(VoucherValidationStatus.NotFound,
                    "Mã giảm giá không tồn tại hoặc đã ngừng áp dụng!");
            }

            if (!voucher.IsActive)
            {
                return new VoucherEvaluation(VoucherValidationStatus.Inactive,
                    "Mã giảm giá này đang tạm ngưng áp dụng!");
            }

            if (voucher.StartDate.HasValue && now < voucher.StartDate.Value)
            {
                return new VoucherEvaluation(VoucherValidationStatus.NotStarted,
                    "Mã giảm giá này chưa đến thời gian có hiệu lực!");
            }

            if (voucher.EndDate.HasValue && now > voucher.EndDate.Value)
            {
                return new VoucherEvaluation(VoucherValidationStatus.Expired,
                    "Mã giảm giá này đã hết hạn sử dụng!");
            }

            if (voucher.UsageLimit > 0 && voucher.UsedCount >= voucher.UsageLimit)
            {
                return new VoucherEvaluation(VoucherValidationStatus.UsageLimitReached,
                    "Mã giảm giá này đã hết lượt sử dụng!");
            }

            if (merchandiseSubtotal < voucher.MinOrderValue)
            {
                return new VoucherEvaluation(VoucherValidationStatus.MinOrderNotMet,
                    string.Format("Mã {0} chỉ áp dụng cho đơn hàng từ {1:N0} đ trở lên!",
                        voucher.Code, voucher.MinOrderValue));
            }

            decimal discount = CalculateVoucherDiscount(voucher, merchandiseSubtotal);

            return new VoucherEvaluation(VoucherValidationStatus.Valid,
                string.Format("Áp dụng mã {0} thành công (Giảm {1:N0} đ)!", voucher.Code, discount),
                discount);
        }

        /// <summary>
        /// Tính toàn bộ đơn hàng từ giá trị hàng hoá + voucher (nếu có).
        /// </summary>
        public static CartPricing CalculateTotals(decimal merchandiseSubtotal, Voucher voucher, DateTime now)
        {
            if (merchandiseSubtotal < 0m)
            {
                merchandiseSubtotal = 0m;
            }

            decimal discount = 0m;
            if (voucher != null)
            {
                var evaluation = EvaluateVoucher(voucher, merchandiseSubtotal, now);
                if (evaluation.IsValid)
                {
                    discount = evaluation.Discount;
                }
            }

            decimal shipping = CalculateShippingFee(merchandiseSubtotal);
            decimal total = merchandiseSubtotal - discount + shipping;
            if (total < 0m)
            {
                total = 0m;
            }

            return new CartPricing
            {
                MerchandiseSubtotal = merchandiseSubtotal,
                ShippingFee = shipping,
                DiscountAmount = discount,
                TotalAmount = total,
                FreeShipping = shipping == 0m,
                AmountToFreeShipping = AmountToFreeShipping(merchandiseSubtotal)
            };
        }
    }
}
