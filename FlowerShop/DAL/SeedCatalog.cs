using System;
using System.Collections.Generic;
using System.Linq;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Orders;
using FlowerShop.Services.Security;

namespace FlowerShop.DAL
{
    /// <summary>
    /// Bộ dữ liệu seed mở rộng (16 sản phẩm bổ sung + gallery + review + đơn demo),
    /// tách riêng để: (1) initializer gọn, (2) unit test kiểm tra được tính nhất quán
    /// (slug/SKU không trùng, giá hợp lệ, tổng số sản phẩm khớp file dump SQL).
    /// </summary>
    public static class SeedCatalog
    {
        public const int BaseProductCount = 8;
        public const int ExtraProductCount = 16;
        public const int TotalProductCount = BaseProductCount + ExtraProductCount;

        public class ProductSeedSpec
        {
            public string Name { get; set; }
            public string Slug { get; set; }
            public string Sku { get; set; }
            public string CategorySlug { get; set; }
            public string OccasionSlug { get; set; }
            public string FlowerTypeSlug { get; set; }
            public string ShortDescription { get; set; }
            public string FullDescription { get; set; }
            public string CareGuide { get; set; }
            public string ColorTone { get; set; }
            public decimal Price { get; set; }
            public decimal? SalePrice { get; set; }
            public int StockQuantity { get; set; }
            public string MainImageUrl { get; set; }
            public double RatingAvg { get; set; }
            public int ReviewCount { get; set; }
            public bool IsFeatured { get; set; }
            public bool IsBestSeller { get; set; }
            public bool IsFlashSale { get; set; }
        }

        public static readonly IReadOnlyList<ProductSeedSpec> ExtraProducts = new List<ProductSeedSpec>
        {
            new ProductSeedSpec { Name = "Bó Hoa Cẩm Tú Cầu Xanh Pastel 'Cloud Nine'", Slug = "bo-hoa-cam-tu-cau-xanh-pastel-cloud-nine", Sku = "FL-HYD-09", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "sinh-nhat", FlowerTypeSlug = "cam-tu-cau", ColorTone = "Xanh", Price = 890000m, SalePrice = 790000m, StockQuantity = 40, RatingAvg = 4.7, ReviewCount = 14, IsBestSeller = true, ShortDescription = "Cẩm tú cầu xanh pastel kết hợp baby trắng trong lớp giấy Hàn Quốc mềm mại", FullDescription = "Sắc xanh dịu của cẩm tú cầu tượng trưng cho sự bình yên và lòng biết ơn. Thiết kế tối giản phong cách Hàn Quốc, phù hợp tặng sinh nhật hoặc trang trí không gian sống.", CareGuide = "Ngâm chân hoa vào nước ấm 30 giây rồi chưng bình mát, thay nước mỗi 2 ngày.", MainImageUrl = "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Giỏ Hoa Hướng Dương 'Sunrise Fortune'", Slug = "gio-hoa-huong-duong-sunrise-fortune", Sku = "FL-SUN-10", CategorySlug = "gio-lang-hoa", OccasionSlug = "khai-truong", FlowerTypeSlug = "hoa-huong-duong", ColorTone = "Vàng", Price = 1450000m, StockQuantity = 25, RatingAvg = 4.9, ReviewCount = 22, IsBestSeller = true, ShortDescription = "Giỏ mây đan 9 bông hướng dương rực rỡ, biểu tượng tài lộc và niềm tin", FullDescription = "Hướng dương luôn hướng về phía mặt trời - biểu trưng cho ý chí vươn lên và vận may. Giỏ hoa 2 tầng thích hợp mừng khai trương, tân gia hoặc chúc mừng thăng chức.", CareGuide = "Giữ nước sạch, tránh gió lạnh trực tiếp, cắt gốc 45 độ mỗi 2 ngày.", MainImageUrl = "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Kệ Hoa Lan Hồ Điệp 'Royal Prosperity'", Slug = "ke-hoa-lan-ho-diep-royal-prosperity", Sku = "FL-ORC-11", CategorySlug = "ke-hoa-khai-truong", OccasionSlug = "khai-truong", FlowerTypeSlug = "lan-ho-diep", ColorTone = "Tím", Price = 3200000m, SalePrice = 2950000m, StockQuantity = 12, RatingAvg = 5.0, ReviewCount = 31, IsFeatured = true, ShortDescription = "Kệ 3 tầng lan hồ điệp tím hoàng gia, món quà khai trương đẳng cấp", FullDescription = "Lan hồ điệp tím tượng trưng cho sự sung túc và vương giả. Kệ 3 tầng kết hợp cẩm tú cầu trắng và lá trâu bạc, phù hợp sảnh văn phòng hoặc khai trương cửa hàng.", CareGuide = "Tưới sương 2 lần/tuần, đặt nơi thoáng mát có ánh sáng gián tiếp.", MainImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Hộp Hoa Mica LED 'Starlight'", Slug = "hop-hoa-mica-led-starlight", Sku = "FL-LED-12", CategorySlug = "hop-hoa-mica-led", OccasionSlug = "tinh-yeu", FlowerTypeSlug = "hoa-hong", ColorTone = "Hồng", Price = 1150000m, StockQuantity = 30, RatingAvg = 4.8, ReviewCount = 19, IsFlashSale = true, ShortDescription = "Hộp mica trong suốt đính LED đom đóm cùng 19 bông hồng Ohara", FullDescription = "Hộp quà bí mật nắp mở, đèn LED đom đóm tạo hiệu ứng lung linh khi mở nắp. Bên trong là 19 bông hồng Ohara phối baby trắng.", CareGuide = "Tháo đế xốp giữ ẩm, chưng bình mát, tránh ánh nắng trực tiếp.", MainImageUrl = "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bó Hoa Tulip Trắng 'Pure Elegance'", Slug = "bo-hoa-tulip-trang-pure-elegance", Sku = "FL-TUL-13", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "ky-niem", FlowerTypeSlug = "hoa-tulip", ColorTone = "Trắng", Price = 980000m, StockQuantity = 35, RatingAvg = 4.6, ReviewCount = 11, ShortDescription = "25 cành tulip trắng Hà Lan thanh khiết, gói giấy lụa kem tối giản", FullDescription = "Tulip trắng là lời tỏ tình thuần khiết và lời chúc bình an. Phong cách tối giản Bắc Âu với giấy lụa kem và ruy băng satin ivory.", CareGuide = "Tulip ưa nước mát, thêm viên đá lạnh mỗi sáng để giữ cành cứng cáp.", MainImageUrl = "https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bó Hoa Baby Trắng Mini 'Little Angel'", Slug = "bo-hoa-baby-trang-mini-little-angel", Sku = "FL-BAB-14", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "tri-an-me", FlowerTypeSlug = "hoa-baby", ColorTone = "Trắng", Price = 390000m, SalePrice = 350000m, StockQuantity = 80, RatingAvg = 4.5, ReviewCount = 26, IsFlashSale = true, ShortDescription = "Baby trắng bó mini trong giấy kraft, quà tặng nhỏ xinh dưới 500k", FullDescription = "Món quà nhỏ nhưng đầy ý nghĩa cho mẹ, cô giáo hay bạn thân. Baby trắng tượng trưng cho tình cảm trong sáng và sự trân trọng.", CareGuide = "Baby khô tự nhiên đẹp nhất, giữ nơi khô ráo, tránh nước đọng.", MainImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bình Hoa Cẩm Tú Cầu Hồng 'Blush Bloom'", Slug = "binh-hoa-cam-tu-cau-hong-blush-bloom", Sku = "FL-HYD-15", CategorySlug = "binh-hoa-cao-cap", OccasionSlug = "ky-niem", FlowerTypeSlug = "cam-tu-cau", ColorTone = "Hồng", Price = 1250000m, StockQuantity = 18, RatingAvg = 4.8, ReviewCount = 9, IsFeatured = true, ShortDescription = "Bình gốm trắng cao cấp cắm cẩm tú cầu hồng và hồng Juliet", FullDescription = "Bình gốm trắng nhập khẩu kết hợp cẩm tú cầu hồng phấn và hồng Juliet cao cấp. Thiết kế sang trọng cho phòng khách, bàn làm việc hoặc quà tặng đối tác.", CareGuide = "Thay nước mỗi 2 ngày, cắt gốc 45 độ, giữ xa hơi nóng.", MainImageUrl = "https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Giỏ Hoa Cưới Trắng Xanh 'Wedding Whisper'", Slug = "gio-hoa-cuoi-trang-xanh-wedding-whisper", Sku = "FL-WED-16", CategorySlug = "gio-lang-hoa", OccasionSlug = "ky-niem", FlowerTypeSlug = "hoa-hong", ColorTone = "Trắng", Price = 1850000m, StockQuantity = 15, RatingAvg = 4.9, ReviewCount = 12, ShortDescription = "Giỏ hoa cưới tone trắng xanh, phối hồng trắng và lan hồ điệp", FullDescription = "Thiết kế dành cho lễ cưới, lễ đính hôn hoặc kỷ niệm ngày cưới. Tone trắng xanh thanh lịch, phối hồng trắng, lan hồ điệp và lá bạc.", CareGuide = "Giữ ẩm đế xốp, đặt nơi mát, tránh nắng trực tiếp.", MainImageUrl = "https://images.unsplash.com/photo-1519225421980-715cb0215aed?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bó Hoa Hồng Vàng 'Golden Wish'", Slug = "bo-hoa-hong-vang-golden-wish", Sku = "FL-ROM-17", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "khai-truong", FlowerTypeSlug = "hoa-hong", ColorTone = "Vàng", Price = 780000m, SalePrice = 720000m, StockQuantity = 45, RatingAvg = 4.4, ReviewCount = 8, ShortDescription = "33 bông hồng vàng Ecuador - lời chúc thịnh vượng và khởi đầu may mắn", FullDescription = "Hồng vàng tượng trưng cho tình bạn bền vững và lời chúc thành công. Phù hợp mừng khai trương, chúc mừng dự án mới.", CareGuide = "Cắt gốc, thay nước sạch mỗi ngày, tránh ánh nắng gắt.", MainImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Hộp Hoa Hồng Đỏ 'Love Letter'", Slug = "hop-hoa-hong-do-love-letter", Sku = "FL-BOX-18", CategorySlug = "hop-hoa-mica-led", OccasionSlug = "tinh-yeu", FlowerTypeSlug = "hoa-hong", ColorTone = "Đỏ", Price = 1350000m, StockQuantity = 22, RatingAvg = 4.9, ReviewCount = 24, IsBestSeller = true, ShortDescription = "Hộp nhung đỏ 24 bông hồng đỏ Ecuador kèm thiệp viết tay", FullDescription = "Hộp nhung đỏ sang trọng chứa 24 bông hồng đỏ Ecuador, kèm thiệp ép kim viết tay theo yêu cầu. Món quà kinh điển cho Valentine, kỷ niệm và cầu hôn.", CareGuide = "Giữ hộp nơi mát, mở nắp để hoa thoáng khí 2 lần/ngày.", MainImageUrl = "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bó Hoa Lan Hồ Điệp Trắng 'Snow Queen'", Slug = "bo-hoa-lan-ho-diep-trang-snow-queen", Sku = "FL-ORC-19", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "tri-an-me", FlowerTypeSlug = "lan-ho-diep", ColorTone = "Trắng", Price = 1650000m, SalePrice = 1490000m, StockQuantity = 20, RatingAvg = 4.9, ReviewCount = 16, IsFeatured = true, ShortDescription = "Bó lan hồ điệp trắng 7 nhánh - món quà tri ân sang trọng cho Mẹ", FullDescription = "Lan hồ điệp trắng biểu trưng cho sự trường thọ và lòng biết ơn. Bó 7 nhánh được nghệ nhân cắm theo dáng thác nước mềm mại.", CareGuide = "Tưới sương nhẹ 2 lần/tuần, tránh nước đọng ở nách lá.", MainImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Giỏ Hoa Baby Trắng Pastel 'Cloud Basket'", Slug = "gio-hoa-baby-trang-pastel-cloud-basket", Sku = "FL-BAS-20", CategorySlug = "gio-lang-hoa", OccasionSlug = "sinh-nhat", FlowerTypeSlug = "hoa-baby", ColorTone = "Trắng", Price = 690000m, StockQuantity = 28, RatingAvg = 4.3, ReviewCount = 7, ShortDescription = "Giỏ mây nhỏ cắm baby trắng và cẩm tú cầu xanh nhạt", FullDescription = "Giỏ mây tự nhiên đựng baby trắng, cẩm tú cầu xanh nhạt và lá bạc - món quà nhẹ nhàng cho sinh nhật, chúc mừng tốt nghiệp.", CareGuide = "Giữ khô thoáng, tránh tưới trực tiếp lên hoa.", MainImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bình Hoa Hướng Dương Văn Phòng 'Office Sun'", Slug = "binh-hoa-huong-duong-van-phong-office-sun", Sku = "FL-SUN-21", CategorySlug = "binh-hoa-cao-cap", OccasionSlug = "khai-truong", FlowerTypeSlug = "hoa-huong-duong", ColorTone = "Vàng", Price = 560000m, SalePrice = 520000m, StockQuantity = 33, RatingAvg = 4.2, ReviewCount = 6, IsFlashSale = true, ShortDescription = "Bình thủy tinh cắm 5 hướng dương nhỏ để bàn làm việc", FullDescription = "Thiết kế gọn cho bàn làm việc, mang năng lượng tích cực mỗi ngày. Bình thủy tinh trong suốt kèm sỏi trắng trang trí.", CareGuide = "Thay nước 2 ngày/lần, cắt gốc 45 độ.", MainImageUrl = "https://images.unsplash.com/photo-1612196808214-b8e1d6145a8c?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Bó Hoa Cẩm Tú Cầu Tím 'Lavender Dream'", Slug = "bo-hoa-cam-tu-cau-tim-lavender-dream", Sku = "FL-HYD-22", CategorySlug = "bo-hoa-tuoi", OccasionSlug = "sinh-nhat", FlowerTypeSlug = "cam-tu-cau", ColorTone = "Tím", Price = 820000m, StockQuantity = 26, RatingAvg = 4.6, ReviewCount = 10, ShortDescription = "Cẩm tú cầu tím phối baby trắng trong giấy lụa Hàn Quốc", FullDescription = "Sắc tím lãng mạn của cẩm tú cầu kết hợp baby trắng tạo nên bó hoa nhẹ nhàng, phù hợp tặng bạn gái hoặc trang trí tiệc.", CareGuide = "Chưng nước sạch, tránh nắng trực tiếp, cắt gốc mỗi 2 ngày.", MainImageUrl = "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Kệ Hoa Chúc Mừng 2 Tầng 'Success Bloom'", Slug = "ke-hoa-chuc-mung-2-tang-success-bloom", Sku = "FL-KE2-23", CategorySlug = "ke-hoa-khai-truong", OccasionSlug = "khai-truong", FlowerTypeSlug = "hoa-hong", ColorTone = "Đỏ", Price = 2450000m, StockQuantity = 10, RatingAvg = 4.7, ReviewCount = 13, ShortDescription = "Kệ 2 tầng hồng đỏ và lan hồ điệp, kèm băng rôn chúc mừng theo yêu cầu", FullDescription = "Kệ hoa 2 tầng cỡ đại cho khai trương, in băng rôn theo tên công ty. Phối hồng đỏ, lan hồ điệp và lá trâu bạc.", CareGuide = "Tưới ẩm đế xốp 1 lần/ngày, đặt nơi thoáng mát.", MainImageUrl = "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80" },
            new ProductSeedSpec { Name = "Hộp Quà Hoa & Chocolate 'Sweet Combo'", Slug = "hop-qua-hoa-chocolate-sweet-combo", Sku = "FL-COM-24", CategorySlug = "hop-hoa-mica-led", OccasionSlug = "tinh-yeu", FlowerTypeSlug = "hoa-hong", ColorTone = "Hồng", Price = 990000m, SalePrice = 890000m, StockQuantity = 24, RatingAvg = 4.5, ReviewCount = 15, ShortDescription = "Combo hộp hoa hồng Ohara + 16 viên chocolate Ferrero", FullDescription = "Combo quà tặng hoàn hảo: 12 bông hồng Ohara trong hộp kraft cao cấp kèm hộp chocolate Ferrero Rocher 16 viên.", CareGuide = "Bảo quản chocolate nơi khô mát, hoa chưng bình riêng sau khi nhận.", MainImageUrl = "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80" }
        };

        /// <summary>
        /// Thêm 16 sản phẩm + 2 ảnh gallery/sản phẩm + review mẫu.
        /// </summary>
        public static void Apply(FlowerShopDbContext context,
            IDictionary<string, int> categoryIds,
            IDictionary<string, int> occasionIds,
            IDictionary<string, int> flowerTypeIds)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (context.Products.Any(p => p.SKU == ExtraProducts[0].Sku))
            {
                return; // đã seed
            }

            var created = new List<Product>();

            foreach (var spec in ExtraProducts)
            {
                var product = new Product
                {
                    CategoryId = Resolve(categoryIds, spec.CategorySlug),
                    OccasionId = ResolveNullable(occasionIds, spec.OccasionSlug),
                    FlowerTypeId = ResolveNullable(flowerTypeIds, spec.FlowerTypeSlug),
                    Name = spec.Name,
                    Slug = spec.Slug,
                    SKU = spec.Sku,
                    ShortDescription = spec.ShortDescription,
                    FullDescription = spec.FullDescription,
                    FlowerCareGuide = spec.CareGuide,
                    ColorTone = spec.ColorTone,
                    Price = spec.Price,
                    SalePrice = spec.SalePrice,
                    StockQuantity = spec.StockQuantity,
                    MainImageUrl = spec.MainImageUrl,
                    RatingAvg = spec.RatingAvg,
                    ReviewCount = spec.ReviewCount,
                    IsFeatured = spec.IsFeatured,
                    IsBestSeller = spec.IsBestSeller,
                    IsFlashSale = spec.IsFlashSale,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                product.Images.Add(new ProductImage { ImageUrl = spec.MainImageUrl, DisplayOrder = 0 });
                product.Images.Add(new ProductImage
                {
                    ImageUrl = "https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80",
                    DisplayOrder = 1
                });

                created.Add(product);
            }

            context.Products.AddRange(created);
            context.SaveChanges();

            SeedReviews(context, created);
        }

        private static void SeedReviews(FlowerShopDbContext context, IList<Product> products)
        {
            var reviewers = new[]
            {
                new { Name = "Nguyễn Thu Hà", Rating = 5, Comment = "Hoa tươi lâu, đóng gói rất kỹ. Shipper giao đúng khung giờ đã chọn." },
                new { Name = "Trần Minh Quân", Rating = 5, Comment = "Bó hoa đẹp hơn ảnh, người nhận rất thích. Sẽ đặt lại lần nữa." },
                new { Name = "Lê Phương Thảo", Rating = 4, Comment = "Hoa đẹp, mùi thơm nhẹ. Nhân viên tư vấn nhiệt tình." }
            };

            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                var reviewer = reviewers[i % reviewers.Length];

                context.Reviews.Add(new Review
                {
                    ProductId = product.Id,
                    CustomerName = reviewer.Name,
                    Rating = reviewer.Rating,
                    Comment = reviewer.Comment,
                    IsApproved = true,
                    CreatedAt = DateTime.Now.AddDays(-(i % 20) - 1)
                });
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Tạo 2 đơn demo (1 đơn đang chờ duyệt ảnh, 1 đơn đã giao) để
        /// Dashboard / OrderTracking / Chatbot có dữ liệu thật khi demo.
        /// </summary>
        public static void SeedDemoOrders(FlowerShopDbContext context, int greetingCardId)
        {
            if (context.Orders.Any())
            {
                return;
            }

            var product = context.Products.OrderBy(p => p.Id).FirstOrDefault();
            if (product == null)
            {
                return;
            }

            decimal unitPrice = product.SalePrice ?? product.Price;

            var pendingOrder = new Order
            {
                OrderCode = OrderCodeGenerator.Generate(DateTime.Now.AddDays(-3)),
                SenderName = "Nguyễn Văn An",
                SenderPhone = "0377415147",
                SenderEmail = "an.nguyen@example.com",
                RecipientName = "Trần Ngọc Mai",
                RecipientPhone = "0909123456",
                RecipientAddress = "601/8/17 Cách Mạng Tháng 8, Phường Hoà Hưng, Quận 10",
                District = "Quận 10",
                City = "Hồ Chí Minh",
                DeliveryDate = DateTime.Today.AddDays(1),
                DeliveryTimeSlot = "10:00 - 12:00",
                IsAnonymous = false,
                GreetingCardId = greetingCardId > 0 ? (int?)greetingCardId : null,
                CardMessage = "Chúc em một ngày thật rực rỡ và nhiều niềm vui!",
                OrderNotes = "Gọi trước khi giao 15 phút.",
                SubTotal = unitPrice,
                AddonTotal = 0m,
                ShippingFee = 0m,
                DiscountAmount = 0m,
                TotalAmount = unitPrice,
                PaymentMethod = "COD",
                PaymentStatus = "Pending",
                OrderStatus = "ProofReady",
                ProofPhotoUrl = product.MainImageUrl,
                CreatedAt = DateTime.Now.AddDays(-3)
            };

            pendingOrder.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.MainImageUrl,
                UnitPrice = unitPrice,
                Quantity = 1,
                TotalPrice = unitPrice
            });

            var deliveredOrder = new Order
            {
                OrderCode = OrderCodeGenerator.Generate(DateTime.Now.AddDays(-9)),
                SenderName = "Lý Bảo Ngọc",
                SenderPhone = "0912345678",
                SenderEmail = "ngoc.ly@example.com",
                RecipientName = "Phạm Thanh Tùng",
                RecipientPhone = "0987654321",
                RecipientAddress = "23 Nguyễn Huệ, Phường Bến Nghé, Quận 1",
                District = "Quận 1",
                City = "Hồ Chí Minh",
                DeliveryDate = DateTime.Today.AddDays(-8),
                DeliveryTimeSlot = "18:00 - 20:00",
                IsAnonymous = true,
                CardMessage = "Secret admirer của bạn 🕊️",
                SubTotal = unitPrice,
                AddonTotal = 0m,
                ShippingFee = 0m,
                DiscountAmount = 0m,
                TotalAmount = unitPrice,
                PaymentMethod = "SEPAY",
                PaymentStatus = "Paid",
                OrderStatus = "Delivered",
                CreatedAt = DateTime.Now.AddDays(-9),
                UpdatedAt = DateTime.Now.AddDays(-8)
            };

            deliveredOrder.OrderDetails.Add(new OrderDetail
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductImageUrl = product.MainImageUrl,
                UnitPrice = unitPrice,
                Quantity = 1,
                TotalPrice = unitPrice
            });

            deliveredOrder.PaymentTransactions.Add(new PaymentTransaction
            {
                Gateway = "SEPAY",
                TransactionNo = "DEMO-SEPAY-00001",
                GatewayResponseCode = "00",
                Amount = unitPrice,
                Currency = "VND",
                PayDate = DateTime.Now.AddDays(-8),
                RawPayload = "seed-demo"
            });

            context.Orders.Add(pendingOrder);
            context.Orders.Add(deliveredOrder);
            context.SaveChanges();
        }

        /// <summary>Tạo tài khoản khách hàng mẫu (mật khẩu BCrypt, không lưu plaintext).</summary>
        public static void SeedCustomers(FlowerShopDbContext context)
        {
            if (context.Users.Any(u => u.Username == "customer"))
            {
                return;
            }

            context.Users.Add(new User
            {
                Username = "customer",
                PasswordHash = PasswordService.Hash("Customer@123"),
                FullName = "Khách Hàng Mẫu",
                Email = "customer@flowershop.vn",
                Phone = "0901234567",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            context.Users.Add(new User
            {
                Username = "florist",
                PasswordHash = PasswordService.Hash("Florist@123"),
                FullName = "Nghệ Nhân Cắm Hoa",
                Email = "florist@flowershop.vn",
                Phone = "0902345678",
                Role = "Florist",
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            context.SaveChanges();
        }

        private static int Resolve(IDictionary<string, int> map, string key)
        {
            int id;
            if (map != null && !string.IsNullOrEmpty(key) && map.TryGetValue(key, out id))
            {
                return id;
            }

            return map != null && map.Count > 0 ? map.Values.First() : 1;
        }

        private static int? ResolveNullable(IDictionary<string, int> map, string key)
        {
            int id;
            if (map != null && !string.IsNullOrEmpty(key) && map.TryGetValue(key, out id))
            {
                return id;
            }

            return null;
        }
    }
}
