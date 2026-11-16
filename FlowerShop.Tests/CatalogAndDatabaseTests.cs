using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FlowerShop.DAL;
using FlowerShop.Helpers;
using Xunit;

namespace FlowerShop.Tests
{
    public class SlugHelperTests
    {
        [Theory]
        [InlineData("Bó Hoa Hồng Đỏ Ecuador", "bo-hoa-hong-do-ecuador")]
        [InlineData("Giỏ & Lẵng Hoa", "gio-lang-hoa")]
        [InlineData("Kệ Hoa Khai Trương 'Đại Phát Tài'", "ke-hoa-khai-truong-dai-phat-tai")]
        [InlineData("  Nhiều   khoảng   trắng  ", "nhieu-khoang-trang")]
        [InlineData("ĐỎ", "do")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ToSlug_ProducesUrlSafeSlug(string input, string expected)
        {
            Assert.Equal(expected, SlugHelper.ToSlug(input));
        }

        [Fact]
        public void ToSlug_NeverProducesLeadingOrTrailingDash()
        {
            string slug = SlugHelper.ToSlug("  --Hoa Tươi--  ");

            Assert.False(slug.StartsWith("-"));
            Assert.False(slug.EndsWith("-"));
            Assert.Equal("hoa-tuoi", slug);
        }

        [Theory]
        [InlineData("Đỏ", "Đỏ")]
        [InlineData("do", "Đỏ")]
        [InlineData("ĐỎ", "Đỏ")]
        [InlineData("red", "Đỏ")]
        [InlineData("hồng", "Hồng")]
        [InlineData("PINK", "Hồng")]
        [InlineData("trắng", "Trắng")]
        [InlineData("vang", "Vàng")]
        [InlineData("xanh", "Xanh")]
        [InlineData("tím", "Tím")]
        [InlineData("cam", "Cam")]
        [InlineData("", "")]
        public void NormalizeColorTone_MapsVariantsToOneValue(string input, string expected)
        {
            Assert.Equal(expected, SlugHelper.NormalizeColorTone(input));
        }

        [Fact]
        public void RemoveDiacritics_KeepsBaseLetters()
        {
            Assert.Equal("Hoa Hong Do", SlugHelper.RemoveDiacritics("Hòa Hồng Đỏ"));
        }
    }

    public class SeedCatalogTests
    {
        [Fact]
        public void ExtraProducts_CountMatchesDeclaration()
        {
            Assert.Equal(SeedCatalog.ExtraProductCount, SeedCatalog.ExtraProducts.Count);
            Assert.Equal(24, SeedCatalog.TotalProductCount);
        }

        [Fact]
        public void SlugsAndSkus_AreUnique()
        {
            var slugs = SeedCatalog.ExtraProducts.Select(p => p.Slug).ToList();
            var skus = SeedCatalog.ExtraProducts.Select(p => p.Sku).ToList();

            Assert.Equal(slugs.Count, slugs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(skus.Count, skus.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void EveryProduct_HasConsistentPricingAndStock()
        {
            foreach (var spec in SeedCatalog.ExtraProducts)
            {
                Assert.False(string.IsNullOrWhiteSpace(spec.Name), "Thiếu tên sản phẩm");
                Assert.True(spec.Price > 0, "Giá phải > 0: " + spec.Slug);

                if (spec.SalePrice.HasValue)
                {
                    Assert.True(spec.SalePrice.Value > 0, "Giá sale phải > 0: " + spec.Slug);
                    Assert.True(spec.SalePrice.Value < spec.Price, "Giá sale phải nhỏ hơn giá gốc: " + spec.Slug);
                }

                Assert.True(spec.StockQuantity > 0, "Tồn kho phải > 0: " + spec.Slug);
                Assert.InRange(spec.RatingAvg, 1.0, 5.0);
                Assert.True(spec.ReviewCount >= 0);
                Assert.StartsWith("https://", spec.MainImageUrl);
            }
        }

        [Fact]
        public void EveryCategoryOccasionFlowerType_ReferencesKnownSlug()
        {
            var knownCategories = new[] { "bo-hoa-tuoi", "gio-lang-hoa", "binh-hoa-cao-cap", "hop-hoa-mica-led", "ke-hoa-khai-truong" };
            var knownOccasions = new[] { "sinh-nhat", "tinh-yeu", "khai-truong", "ky-niem", "tri-an-me" };
            var knownFlowerTypes = new[] { "hoa-hong", "hoa-tulip", "hoa-baby", "lan-ho-diep", "hoa-huong-duong", "cam-tu-cau" };

            foreach (var spec in SeedCatalog.ExtraProducts)
            {
                Assert.Contains(spec.CategorySlug, knownCategories);
                Assert.Contains(spec.OccasionSlug, knownOccasions);
                Assert.Contains(spec.FlowerTypeSlug, knownFlowerTypes);
            }
        }
    }

    public class DbIndexProvisionerTests
    {
        private static readonly string[] KnownTables =
        {
            "Categories", "Occasions", "FlowerTypes", "Products", "ProductImages",
            "AddonItems", "GreetingCards", "Vouchers", "Users", "PasswordResetTokens",
            "Orders", "OrderDetails", "OrderAddons", "PaymentTransactions", "Reviews"
        };

        [Fact]
        public void Catalog_IsLargeAndValid()
        {
            Assert.True(DbIndexProvisioner.Catalog.Count >= 40,
                "Cần tối thiểu 40 chỉ mục cho tải lớn, hiện có " + DbIndexProvisioner.Catalog.Count);

            Assert.Empty(DbIndexProvisioner.ValidateCatalog(KnownTables));
        }

        [Fact]
        public void Catalog_HasNoDuplicateIndexPerTable()
        {
            var keys = DbIndexProvisioner.Catalog.Select(i => i.Table + "." + i.Name).ToList();

            Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public void Catalog_CoversCriticalQueries()
        {
            var names = DbIndexProvisioner.Catalog.Select(i => i.Name).ToList();

            // tra cứu đơn
            Assert.Contains("UX_Orders_OrderCode", names);
            // dashboard doanh thu
            Assert.Contains("IX_Orders_PaymentStatus_CreatedAt", names);
            // danh sách sản phẩm theo danh mục
            Assert.Contains("IX_Products_Category_Active", names);
            // filter màu + flash sale + best seller
            Assert.Contains("IX_Products_Active_Color", names);
            Assert.Contains("IX_Products_Active_FlashSale", names);
            Assert.Contains("IX_Products_Active_BestSeller", names);
            // token reset mật khẩu
            Assert.Contains("UX_PRT_TokenHash", names);
            // chống trùng giao dịch thanh toán
            Assert.Contains("UX_PaymentTransactions_Gateway_Txn", names);
            // review đã duyệt
            Assert.Contains("IX_Reviews_Product_Approved_Created", names);
            // tìm kiếm full-text
            Assert.Contains("FT_Products_Search", names);
        }

        [Fact]
        public void BuildCreateIndexStatement_FormatsEachKind()
        {
            var normal = new IndexDefinition("Products", "IX_A", "`A`,`B`");
            var unique = new IndexDefinition("Products", "UX_A", "`Slug`", true);
            var fulltext = new IndexDefinition("Products", "FT_A", "`Name`", false, true);

            Assert.Equal("CREATE INDEX `IX_A` ON `Products` (`A`,`B`)",
                DbIndexProvisioner.BuildCreateIndexStatement(normal));
            Assert.Equal("CREATE UNIQUE INDEX `UX_A` ON `Products` (`Slug`)",
                DbIndexProvisioner.BuildCreateIndexStatement(unique));
            Assert.Equal("CREATE FULLTEXT INDEX `FT_A` ON `Products` (`Name`)",
                DbIndexProvisioner.BuildCreateIndexStatement(fulltext));
        }

        [Fact]
        public void ValidateCatalog_DetectsUnknownTable()
        {
            var errors = DbIndexProvisioner.ValidateCatalog(new[] { "Products" });

            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("Bảng không tồn tại"));
        }
    }

    /// <summary>
    /// Kiểm tra file SQL dump thật: đủ bảng, đủ chỉ mục, đủ seed.
    /// Đây là "hợp đồng" giữa script Laragon và code EF - lệch là fail ngay.
    /// </summary>
    public class DatabaseScriptTests
    {
        private static string Script
        {
            get
            {
                Assert.True(File.Exists(TestPaths.DatabaseScript), "Không thấy file dump: " + TestPaths.DatabaseScript);
                return File.ReadAllText(TestPaths.DatabaseScript);
            }
        }

        private static readonly string[] ExpectedTables =
        {
            "Categories", "Occasions", "FlowerTypes", "Products", "ProductImages",
            "AddonItems", "GreetingCards", "Vouchers", "Users", "PasswordResetTokens",
            "Orders", "OrderDetails", "OrderAddons", "PaymentTransactions", "Reviews"
        };

        [Fact]
        public void Script_CreatesAllTables()
        {
            string script = Script;

            foreach (string table in ExpectedTables)
            {
                Assert.Contains("CREATE TABLE `" + table + "`", script);
            }
        }

        [Fact]
        public void Script_CreatesEveryIndexFromCatalog()
        {
            string script = Script;

            foreach (var definition in DbIndexProvisioner.Catalog)
            {
                Assert.Contains(definition.Name, script);
            }
        }

        [Fact]
        public void Script_SeedsAtLeast24Products()
        {
            string script = Script;

            int start = script.IndexOf("INSERT INTO `Products`", StringComparison.Ordinal);
            Assert.True(start > 0, "Không có INSERT INTO Products");

            int end = script.IndexOf(";", start, StringComparison.Ordinal);
            string block = script.Substring(start, end - start);

            int rows = Regex.Matches(block, @"^\s*\(\d+,", RegexOptions.Multiline).Count;

            Assert.True(rows >= 24, "Chỉ có " + rows + " sản phẩm seed, cần >= 24");
        }

        [Fact]
        public void Script_SeedsCategoriesOccasionsFlowerTypes()
        {
            Assert.True(Regex.Matches(Script, @"^\s*\(\d+,'[^']+','[a-z\-]+'", RegexOptions.Multiline).Count >= 10,
                "Seed danh mục/dịp/loại hoa chưa đủ");
        }

        /// <summary>
        /// Chặn hồi quy lỗi "mất dấu tiếng Việt": file dump phải là UTF-8 đúng và
        /// không được chứa dấu hiệu bị convert qua charset cũ (dấu hỏi thay cho dấu thanh).
        /// </summary>
        [Fact]
        public void Script_KeepsVietnameseCharactersIntact()
        {
            string script = Script;

            // Có tiếng Việt thật
            Assert.Contains("Bó Hoa Hồng Đỏ Ecuador", script);
            Assert.Contains("Tình Yêu & Valentine", script);
            Assert.Contains("Hộp Hoa & Mica LED", script);

            // Không bị thay bằng '?' (mất dữ liệu vĩnh viễn khi import sai charset)
            Assert.DoesNotContain("B? Hoa", script);
            Assert.DoesNotContain("H?ng", script);
            Assert.DoesNotContain("T??i", script);

            // Không bị double-encode (UTF-8 đọc như Latin-1 rồi lưu lại)
            Assert.DoesNotContain("BÃ³", script);
            Assert.DoesNotContain("Há»“ng", script);
        }

        [Fact]
        public void Script_IsUtf8WithValidEncoding()
        {
            byte[] bytes = File.ReadAllBytes(TestPaths.DatabaseScript);
            var strictUtf8 = new System.Text.UTF8Encoding(false, true);

            string text = strictUtf8.GetString(bytes); // ném exception nếu không phải UTF-8 hợp lệ
            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [Fact]
        public void Script_HasNoDeadPaymentGateways()
        {
            string script = Script;

            Assert.DoesNotContain("VIETQR", script);
            Assert.DoesNotContain("VNPAY", script);
            Assert.DoesNotContain("STRIPE", script);
            Assert.DoesNotContain("PAYOS", script);
            Assert.Contains("SEPAY", script);
        }

        [Fact]
        public void Script_UsesUniqueConstraintsForBusinessKeys()
        {
            string script = Script;

            Assert.Contains("UNIQUE KEY `UX_Products_Slug`", script);
            Assert.Contains("UNIQUE KEY `UX_Orders_OrderCode`", script);
            Assert.Contains("UNIQUE KEY `UX_Users_Username`", script);
            Assert.Contains("UNIQUE KEY `UX_Vouchers_Code`", script);
        }

        [Fact]
        public void Script_DoesNotStorePlaintextPasswords()
        {
            string script = Script;

            int start = script.IndexOf("INSERT INTO `Users`", StringComparison.Ordinal);
            Assert.True(start > 0, "Không có INSERT INTO Users");

            int end = script.IndexOf(";", start, StringComparison.Ordinal);
            string usersBlock = script.Substring(start, end - start);

            // Seed phải là SHA-256 hex (PasswordService tự nâng cấp sang BCrypt khi đăng nhập),
            // không được lưu mật khẩu thô.
            Assert.DoesNotContain("Admin@123", usersBlock);
            Assert.True(Regex.IsMatch(usersBlock, @"'admin','[a-f0-9]{64}'"),
                "Tài khoản admin phải có PasswordHash SHA-256 hex");
        }
    }
}
