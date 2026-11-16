using System;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Pricing;
using Xunit;

namespace FlowerShop.Tests
{
    public class PricingServiceTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(100000, 35000)]
        [InlineData(499999, 35000)]
        [InlineData(500000, 0)]
        [InlineData(1200000, 0)]
        public void CalculateShippingFee_FollowsThreshold(double merchandise, double expected)
        {
            Assert.Equal((decimal)expected, PricingService.CalculateShippingFee((decimal)merchandise));
        }

        [Fact]
        public void AmountToFreeShipping_ReturnsGapOnlyBelowThreshold()
        {
            Assert.Equal(500000m, PricingService.AmountToFreeShipping(0m));
            Assert.Equal(1m, PricingService.AmountToFreeShipping(499999m));
            Assert.Equal(0m, PricingService.AmountToFreeShipping(500000m));
            Assert.Equal(0m, PricingService.AmountToFreeShipping(5000000m));
        }

        [Fact]
        public void CalculateVoucherDiscount_PercentIsCappedByMaxDiscount()
        {
            var voucher = new Voucher { Code = "BLOOM10", DiscountPercent = 10, MaxDiscountAmount = 100000m, MinOrderValue = 0m };

            // 10% của 2.000.000 = 200.000 -> bị chặn ở 100.000
            Assert.Equal(100000m, PricingService.CalculateVoucherDiscount(voucher, 2000000m));

            // 10% của 600.000 = 60.000 -> không chạm trần
            Assert.Equal(60000m, PricingService.CalculateVoucherDiscount(voucher, 600000m));
        }

        [Fact]
        public void CalculateVoucherDiscount_AmountTypeAndFloor()
        {
            var voucher = new Voucher { Code = "VIPLOVE", DiscountAmount = 150000m };

            Assert.Equal(150000m, PricingService.CalculateVoucherDiscount(voucher, 1200000m));

            // Giảm giá không bao giờ vượt quá giá trị hàng hoá
            Assert.Equal(100000m, PricingService.CalculateVoucherDiscount(voucher, 100000m));
        }

        [Fact]
        public void CalculateVoucherDiscount_RespectsMinOrderValue()
        {
            var voucher = new Voucher { Code = "WELCOME50", DiscountAmount = 50000m, MinOrderValue = 400000m };

            Assert.Equal(0m, PricingService.CalculateVoucherDiscount(voucher, 399999m));
            Assert.Equal(50000m, PricingService.CalculateVoucherDiscount(voucher, 400000m));
        }

        [Fact]
        public void EvaluateVoucher_ReportsEachFailureReason()
        {
            var now = new DateTime(2026, 6, 15, 10, 0, 0);

            Assert.Equal(VoucherValidationStatus.NotFound, PricingService.EvaluateVoucher(null, 1000000m, now).Status);

            Assert.Equal(VoucherValidationStatus.Inactive,
                PricingService.EvaluateVoucher(new Voucher { Code = "X", IsActive = false }, 1000000m, now).Status);

            Assert.Equal(VoucherValidationStatus.NotStarted,
                PricingService.EvaluateVoucher(new Voucher { Code = "X", IsActive = true, StartDate = now.AddDays(1) }, 1000000m, now).Status);

            Assert.Equal(VoucherValidationStatus.Expired,
                PricingService.EvaluateVoucher(new Voucher { Code = "X", IsActive = true, EndDate = now.AddDays(-1) }, 1000000m, now).Status);

            Assert.Equal(VoucherValidationStatus.UsageLimitReached,
                PricingService.EvaluateVoucher(new Voucher { Code = "X", IsActive = true, UsageLimit = 10, UsedCount = 10 }, 1000000m, now).Status);

            Assert.Equal(VoucherValidationStatus.MinOrderNotMet,
                PricingService.EvaluateVoucher(new Voucher { Code = "X", IsActive = true, MinOrderValue = 2000000m }, 1000000m, now).Status);
        }

        [Fact]
        public void EvaluateVoucher_ValidReturnsDiscountAndMessage()
        {
            var now = new DateTime(2026, 6, 15);
            var voucher = new Voucher
            {
                Code = "WELCOME50",
                IsActive = true,
                DiscountAmount = 50000m,
                MinOrderValue = 400000m,
                StartDate = now.AddDays(-1),
                EndDate = now.AddDays(1),
                UsageLimit = 100,
                UsedCount = 5
            };

            var result = PricingService.EvaluateVoucher(voucher, 650000m, now);

            Assert.True(result.IsValid);
            Assert.Equal(50000m, result.Discount);
            Assert.Contains("WELCOME50", result.Message);
        }

        [Fact]
        public void CalculateTotals_CombinesMerchandiseShippingDiscount()
        {
            var now = new DateTime(2026, 6, 15);
            var voucher = new Voucher { Code = "WELCOME50", IsActive = true, DiscountAmount = 50000m, MinOrderValue = 400000m };

            var pricing = PricingService.CalculateTotals(450000m, voucher, now);

            Assert.Equal(450000m, pricing.MerchandiseSubtotal);
            Assert.Equal(35000m, pricing.ShippingFee);
            Assert.Equal(50000m, pricing.DiscountAmount);
            Assert.Equal(435000m, pricing.TotalAmount);
            Assert.False(pricing.FreeShipping);
        }

        [Fact]
        public void CalculateTotals_FreeShippingWhenMerchandiseAboveThreshold()
        {
            var pricing = PricingService.CalculateTotals(850000m, null, DateTime.Now);

            Assert.Equal(0m, pricing.ShippingFee);
            Assert.True(pricing.FreeShipping);
            Assert.Equal(850000m, pricing.TotalAmount);
        }

        [Fact]
        public void CalculateTotals_NeverNegative()
        {
            // Giảm giá bị chặn ở giá trị hàng hoá, nhưng phí ship vẫn được cộng -> tổng = ship
            var voucher = new Voucher { Code = "BIG", IsActive = true, DiscountAmount = 900000m, MinOrderValue = 0m };

            var pricing = PricingService.CalculateTotals(100000m, voucher, DateTime.Now);

            Assert.Equal(100000m, pricing.DiscountAmount);
            Assert.Equal(35000m, pricing.ShippingFee);
            Assert.Equal(35000m, pricing.TotalAmount);
            Assert.True(pricing.TotalAmount >= 0m);
        }
    }

    public class ShoppingCartTests
    {
        [Fact]
        public void Totals_DelegateToPricingService()
        {
            var cart = new ShoppingCart();
            cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Hoa", UnitPrice = 300000m, Quantity = 2 });

            Assert.Equal(600000m, cart.SubTotal);
            Assert.Equal(600000m, cart.MerchandiseSubtotal);
            Assert.Equal(0m, cart.ShippingFee); // 600k >= 500k
            Assert.Equal(600000m, cart.TotalAmount);
            Assert.Equal(2, cart.TotalItemsCount);
        }

        [Fact]
        public void Totals_IncludeAddonsAndVoucher()
        {
            var cart = new ShoppingCart();
            cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Hoa", UnitPrice = 400000m, Quantity = 1 });
            cart.Addons.Add(new CartAddonItem { AddonId = 1, AddonName = "Chocolate", UnitPrice = 100000m, Quantity = 1 });
            cart.AppliedVoucher = new Voucher
            {
                Code = "WELCOME50",
                IsActive = true,
                DiscountAmount = 50000m,
                MinOrderValue = 400000m,
                UsageLimit = 100
            };

            Assert.Equal(500000m, cart.MerchandiseSubtotal);
            Assert.Equal(50000m, cart.DiscountAmount);
            Assert.Equal(0m, cart.ShippingFee);
            Assert.Equal(450000m, cart.TotalAmount);
        }

        [Fact]
        public void Totals_ExpiredVoucherIsIgnored()
        {
            var cart = new ShoppingCart();
            cart.Items.Add(new CartItem { ProductId = 1, ProductName = "Hoa", UnitPrice = 700000m, Quantity = 1 });
            cart.AppliedVoucher = new Voucher
            {
                Code = "OLD",
                IsActive = true,
                DiscountAmount = 100000m,
                EndDate = DateTime.Now.AddDays(-1)
            };

            Assert.Equal(0m, cart.DiscountAmount);
            Assert.Equal(700000m, cart.TotalAmount);
        }
    }
}
