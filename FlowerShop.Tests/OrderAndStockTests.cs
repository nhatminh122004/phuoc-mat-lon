using System;
using System.Collections.Generic;
using System.Linq;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Orders;
using Xunit;

namespace FlowerShop.Tests
{
    public class OrderCodeGeneratorTests
    {
        [Fact]
        public void Generate_MatchesPattern()
        {
            for (int i = 0; i < 200; i++)
            {
                string code = OrderCodeGenerator.Generate(new DateTime(2026, 11, 16));

                Assert.StartsWith("FL261116", code);
                Assert.True(OrderCodeGenerator.IsWellFormed(code), "Mã không đúng định dạng: " + code);
                Assert.Equal(13, code.Length);
            }
        }

        [Fact]
        public void Generate_IsRandomEnough()
        {
            var codes = new HashSet<string>();

            for (int i = 0; i < 2000; i++)
            {
                codes.Add(OrderCodeGenerator.Generate(new DateTime(2026, 11, 16)));
            }

            // Nếu dùng Random(1000,9999) như bản cũ thì 2000 lần chỉ ra tối đa 9000 giá trị
            // và gần như chắc chắn trùng nhau rất nhiều.
            Assert.True(codes.Count > 1990, "Mã đơn bị trùng quá nhiều: " + codes.Count);
        }

        [Fact]
        public void RandomSuffix_AvoidsAmbiguousCharacters()
        {
            for (int i = 0; i < 500; i++)
            {
                string suffix = OrderCodeGenerator.RandomSuffix(8);

                Assert.Equal(8, suffix.Length);
                Assert.DoesNotContain("I", suffix);
                Assert.DoesNotContain("O", suffix);
                Assert.DoesNotContain("0", suffix);
                Assert.DoesNotContain("1", suffix);
            }
        }

        [Fact]
        public void RandomSuffix_RejectsNonPositiveLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => OrderCodeGenerator.RandomSuffix(0));
        }

        [Theory]
        [InlineData(" fl260101ab2cd ", "FL260101AB2CD")]
        [InlineData("FL 260101 AB2CD", "FL260101AB2CD")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Normalize_TrimsSpacesAndUppercases(string input, string expected)
        {
            Assert.Equal(expected, OrderCodeGenerator.Normalize(input));
        }

        [Theory]
        [InlineData("FL260101AB2CD", true)]
        [InlineData("FL202609088831", false)]  // mã cũ 14 số không còn hợp lệ
        [InlineData("FL260101AB2C", false)]    // thiếu 1 ký tự
        [InlineData("XX260101AB2CD", false)]
        [InlineData("", false)]
        public void IsWellFormed_ValidatesFormat(string code, bool expected)
        {
            Assert.Equal(expected, OrderCodeGenerator.IsWellFormed(code));
        }
    }

    public class StockServiceTests
    {
        private static List<CartItem> Items(params int[] quantities)
        {
            var items = new List<CartItem>();
            for (int i = 0; i < quantities.Length; i++)
            {
                items.Add(new CartItem { ProductId = i + 1, ProductName = "Hoa " + (i + 1), Quantity = quantities[i] });
            }
            return items;
        }

        [Fact]
        public void BuildRequests_MapsAvailableStock()
        {
            var stock = new Dictionary<int, int> { { 1, 10 }, { 2, 3 } };

            var requests = StockService.BuildRequests(Items(2, 5), stock);

            Assert.Equal(2, requests.Count);
            Assert.Equal(10, requests[0].Available);
            Assert.Equal(3, requests[1].Available);
        }

        [Fact]
        public void BuildRequests_MissingStockBecomesZero()
        {
            var requests = StockService.BuildRequests(Items(1), new Dictionary<int, int>());

            Assert.Equal(0, requests[0].Available);
        }

        [Fact]
        public void Validate_PassesWhenAllInStock()
        {
            var result = StockService.Validate(new List<StockRequest>
            {
                new StockRequest { ProductId = 1, ProductName = "A", Requested = 2, Available = 10 },
                new StockRequest { ProductId = 2, ProductName = "B", Requested = 3, Available = 3 }
            });

            Assert.True(result.Ok);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_DetectsInsufficientStock()
        {
            var result = StockService.Validate(new List<StockRequest>
            {
                new StockRequest { ProductId = 1, ProductName = "Hoa Hồng", Requested = 5, Available = 2 }
            });

            Assert.False(result.Ok);
            Assert.Single(result.Errors);
            Assert.Contains("Hoa Hồng", result.Errors[0]);
        }

        [Fact]
        public void Validate_DetectsOutOfStock()
        {
            var result = StockService.Validate(new List<StockRequest>
            {
                new StockRequest { ProductId = 1, ProductName = "Lan", Requested = 1, Available = 0 }
            });

            Assert.False(result.Ok);
            Assert.Contains("hết hàng", result.Errors[0]);
        }

        [Fact]
        public void Validate_RejectsInvalidQuantity()
        {
            var result = StockService.Validate(new List<StockRequest>
            {
                new StockRequest { ProductId = 1, ProductName = "Tulip", Requested = 0, Available = 10 }
            });

            Assert.False(result.Ok);
            Assert.Contains("không hợp lệ", result.Errors[0]);
        }
    }

    public class VoucherUsageServiceTests
    {
        [Theory]
        [InlineData("COD", true)]
        [InlineData("cod", true)]
        [InlineData("SEPAY", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void ShouldConsumeOnPlacement_OnlyForCod(string method, bool expected)
        {
            Assert.Equal(expected, VoucherUsageService.ShouldConsumeOnPlacement(method));
        }

        [Theory]
        [InlineData("SEPAY", true)]
        [InlineData("sepay", true)]
        [InlineData("COD", false)]
        [InlineData("STRIPE", false)]
        public void IsOnlineGateway_OnlySePay(string method, bool expected)
        {
            Assert.Equal(expected, VoucherUsageService.IsOnlineGateway(method));
        }
    }

    public class BouquetBuilderServiceTests
    {
        [Theory]
        [InlineData(9, true)]
        [InlineData(19, true)]
        [InlineData(33, true)]
        [InlineData(99, true)]
        [InlineData(10, false)]
        [InlineData(0, false)]
        public void IsValidStemCount_AllowsOnlySignatureCounts(int count, bool expected)
        {
            Assert.Equal(expected, BouquetBuilderService.IsValidStemCount(count));
        }

        [Fact]
        public void CalculatePrice_AddsWrapCostAndStems()
        {
            decimal price = BouquetBuilderService.CalculatePrice(BouquetBuilderService.WrapBoTron, 19, 25000m);

            Assert.Equal(80000m + (19 * 25000m), price);
        }

        [Fact]
        public void CalculatePrice_UnknownWrapFallsBackToBoTron()
        {
            Assert.Equal(
                BouquetBuilderService.CalculatePrice(BouquetBuilderService.WrapBoTron, 9, 20000m),
                BouquetBuilderService.CalculatePrice("KhongTonTai", 9, 20000m));
        }

        [Fact]
        public void ResolveStemUnitPrice_RoundsToThousandsAndHasFloor()
        {
            // 2.950.000 / 19 = 155.263 -> làm tròn nghìn = 155.000
            Assert.Equal(155000m, BouquetBuilderService.ResolveStemUnitPrice(2950000m));

            // Giá quá nhỏ -> dùng đơn giá mặc định thay vì bán lỗ
            Assert.Equal(BouquetBuilderService.DefaultStemUnitPrice, BouquetBuilderService.ResolveStemUnitPrice(20000m));
            Assert.Equal(BouquetBuilderService.DefaultStemUnitPrice, BouquetBuilderService.ResolveStemUnitPrice(0m));
        }

        [Fact]
        public void BuildLineName_IncludesConfiguration()
        {
            string name = BouquetBuilderService.BuildLineName("Hồng Đỏ Ecuador", BouquetBuilderService.WrapGioHoa, 33, "Hồng Pastel");

            Assert.Contains("Tự Thiết Kế", name);
            Assert.Contains("33 cành", name);
            Assert.Contains("Hồng Pastel", name);
        }

        [Theory]
        [InlineData(BouquetBuilderService.WrapGioHoa, "Giỏ hoa")]
        [InlineData(BouquetBuilderService.WrapHopMica, "Hộp mica")]
        [InlineData(BouquetBuilderService.WrapBoDai, "Bó dài")]
        [InlineData(BouquetBuilderService.WrapBoTron, "Bó tròn")]
        public void GetWrapDisplayName_MapsAllStyles(string style, string expected)
        {
            Assert.Equal(expected, BouquetBuilderService.GetWrapDisplayName(style));
        }
    }
}
