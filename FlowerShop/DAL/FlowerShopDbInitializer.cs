using System;
using System.Collections.Generic;
using System.Data.Entity;
using FlowerShop.Models.Entities;

namespace FlowerShop.DAL
{
    public class FlowerShopDbInitializer : CreateDatabaseIfNotExists<FlowerShopDbContext>
    {
        protected override void Seed(FlowerShopDbContext context)
        {
            SeedDatabase(context);
            base.Seed(context);
        }

        public static void SeedDatabase(FlowerShopDbContext context)
        {
            // 1. Categories
            var catBoHoa = new Category
            {
                Name = "Bó Hoa Tươi",
                Slug = "bo-hoa-tuoi",
                Description = "Những bó hoa nghệ thuật thiết kế tinh tế từ các nghệ nhân cắm hoa hàng đầu",
                ImageUrl = "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 1
            };
            var catGioHoa = new Category
            {
                Name = "Giỏ & Lẵng Hoa",
                Slug = "gio-lang-hoa",
                Description = "Giỏ hoa gỗ, mây tre đan sang trọng phù hợp trưng bày phòng khách và văn phòng",
                ImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 2
            };
            var catBinhHoa = new Category
            {
                Name = "Bình Hoa Cao Cấp",
                Slug = "binh-hoa-cao-cap",
                Description = "Bình gốm sứ & pha lê nghệ thuật cắm sẵn sang trọng quý phái",
                ImageUrl = "https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 3
            };
            var catHopHoa = new Category
            {
                Name = "Hộp Hoa & Mica LED",
                Slug = "hop-hoa-mica-led",
                Description = "Hộp quà hoa bí mật nắp mở đính kèm dây đèn LED đom đóm lãng mạn",
                ImageUrl = "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 4
            };
            var catKeKhaiTruong = new Category
            {
                Name = "Kệ Hoa Khai Trương",
                Slug = "ke-hoa-khai-truong",
                Description = "Kệ hoa 2-3 tầng đại tài lộc mừng hồng phát, tân gia và kỷ niệm thành lập",
                ImageUrl = "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=600&q=80",
                DisplayOrder = 5
            };

            context.Categories.AddRange(new[] { catBoHoa, catGioHoa, catBinhHoa, catHopHoa, catKeKhaiTruong });
            context.SaveChanges();

            // 2. Occasions
            var occBirthday = new Occasion { Name = "Hoa Sinh Nhật", Slug = "sinh-nhat", IconClass = "fa-cake-candles", Description = "Trao gửi lời chúc tuổi mới rạng rỡ và tràn đầy may mắn" };
            var occLove = new Occasion { Name = "Tình Yêu & Valentine", Slug = "tinh-yeu", IconClass = "fa-heart", Description = "Bày tỏ tình cảm chân thành, kỷ niệm ngày yêu và lời cầu hôn ngọt ngào" };
            var occKhaiTruong = new Occasion { Name = "Khai Trương - Chúc Mừng", Slug = "khai-truong", IconClass = "fa-champagne-glasses", Description = "Cung chúc phát tài, công việc hanh thông, vạn sự cát tường" };
            var occAnniversary = new Occasion { Name = "Kỷ Niệm Ngày Cưới", Slug = "ky-niem", IconClass = "fa-ring", Description = "Kỷ niệm những chặng đường hạnh phúc thủy chung bên nhau" };
            var occGratitude = new Occasion { Name = "Tri Ân & Ngày Của Mẹ", Slug = "tri-an-me", IconClass = "fa-hands-holding-child", Description = "Biết ơn công lao sinh thành của cha mẹ, thầy cô" };

            context.Occasions.AddRange(new[] { occBirthday, occLove, occKhaiTruong, occAnniversary, occGratitude });
            context.SaveChanges();

            // 3. FlowerTypes
            var ftRose = new FlowerType { Name = "Hoa Hồng Ecuador & Ohara", Slug = "hoa-hong", Meaning = "Tình yêu mãnh liệt, vẻ đẹp thanh lịch và sự trân quý tuyệt đối" };
            var ftTulip = new FlowerType { Name = "Hoa Tulip Hà Lan", Slug = "hoa-tulip", Meaning = "Lời tỏ tình thuần khiết, sự hoàn hảo và tình cảm chân thành" };
            var ftBaby = new FlowerType { Name = "Hoa Baby Trắng", Slug = "hoa-baby", Meaning = "Tình yêu ngây thơ, sự trong sáng thuần khiết vĩnh hằng" };
            var ftLan = new FlowerType { Name = "Lan Hồ Điệp Hoàng Gia", Slug = "lan-ho-diep", Meaning = "Sung túc, vương giả, tài lộc bền vững và trường thọ" };
            var ftSunflower = new FlowerType { Name = "Hoa Hướng Dương", Slug = "hoa-huong-duong", Meaning = "Niềm tin, hy vọng, luôn hướng về phía ánh mặt trời" };
            var ftCamTuCau = new FlowerType { Name = "Cẩm Tú Cầu", Slug = "cam-tu-cau", Meaning = "Lòng biết ơn sâu sắc và cảm xúc chân thành xuất phát từ trái tim" };

            context.FlowerTypes.AddRange(new[] { ftRose, ftTulip, ftBaby, ftLan, ftSunflower, ftCamTuCau });
            context.SaveChanges();

            // 4. Products
            var products = new List<Product>
            {
                new Product
                {
                    CategoryId = catBoHoa.Id,
                    OccasionId = occLove.Id,
                    FlowerTypeId = ftRose.Id,
                    Name = "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'",
                    Slug = "bo-hoa-hong-do-ecuador-red-romance",
                    SKU = "FL-ROM-01",
                    ShortDescription = "33 cành hồng đỏ Ecuador cỡ đại phối lá bạc Dollar bọc giấy lụa đen sang trọng viền vàng",
                    FullDescription = "Bó hoa 'Red Romance' tuyển chọn 33 cành hồng đỏ Ecuador nhập khẩu cánh dày mịn như nhung. Kết hợp cùng lá bạc nhập khẩu và giấy gói lụa cao cấp, mang thông điệp 'Yêu em say đắm bằng cả con tim'. Phù hợp tặng sinh nhật người yêu, ngày lễ tình nhân 14/2, 20/10 hoặc ngày kỷ niệm đặc biệt.",
                    FlowerCareGuide = "Cắt xéo gốc 45 độ, thay nước sạch mỗi ngày, thêm gói dưỡng hoa FloraLife tặng kèm. Tránh ánh nắng gắt trực tiếp và gió máy lạnh.",
                    ColorTone = "Đỏ",
                    Price = 950000,
                    SalePrice = 850000,
                    StockQuantity = 50,
                    MainImageUrl = "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 5.0,
                    ReviewCount = 28,
                    IsFeatured = true,
                    IsBestSeller = true,
                    IsFlashSale = true
                },
                new Product
                {
                    CategoryId = catBoHoa.Id,
                    OccasionId = occBirthday.Id,
                    FlowerTypeId = ftTulip.Id,
                    Name = "Bó Hoa Tulip Hồng 'Sweet Whispers'",
                    Slug = "bo-hoa-tulip-hong-sweet-whispers",
                    SKU = "FL-TUL-02",
                    ShortDescription = "20 cành Tulip hồng phấn Hà Lan tượng trưng cho tình cảm ngọt ngào, tinh tế",
                    FullDescription = "Bó hoa Tulip 'Sweet Whispers' mang sắc hồng dịu dàng đầy mê hoặc từ củ giống Hà Lan tuyển chọn. Bó theo phong cách tối giản Hàn Quốc (Korean Minimalist Style) tôn lên vẻ đẹp thanh khiết của cánh hoa tulip.",
                    FlowerCareGuide = "Tulip ưa nước đá lạnh. Thả vài viên đá lạnh vào bình mỗi sáng để giữ cành luôn cứng cáp và hoa tươi trên 7 ngày.",
                    ColorTone = "Hồng",
                    Price = 1200000,
                    SalePrice = 1050000,
                    StockQuantity = 35,
                    MainImageUrl = "https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 4.9,
                    ReviewCount = 19,
                    IsFeatured = true,
                    IsBestSeller = true
                },
                new Product
                {
                    CategoryId = catGioHoa.Id,
                    OccasionId = occGratitude.Id,
                    FlowerTypeId = ftCamTuCau.Id,
                    Name = "Giỏ Hoa Mây 'Pastel Harmony'",
                    Slug = "gio-hoa-may-pastel-harmony",
                    SKU = "FL-PAS-03",
                    ShortDescription = "Giỏ hoa phối tone màu pastel thanh nhã gồm Cẩm Tú Cầu, Hồng Ohara và Cúc Tana",
                    FullDescription = "Giỏ hoa nghệ thuật đan mây tự nhiên 'Pastel Harmony' phối màu pastel mềm mại, tạo cảm giác thư thái và an yên. Món quà hoàn hảo để dành tặng Mẹ, Cô giáo hay đối tác nữ trang nhã.",
                    FlowerCareGuide = "Xịt phun sương nhẹ lên cánh hoa cẩm tú cầu vào buổi sáng, châm 100ml nước vào xốp cắm hoa mỗi ngày.",
                    ColorTone = "Hồng",
                    Price = 780000,
                    SalePrice = 690000,
                    StockQuantity = 40,
                    MainImageUrl = "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 4.8,
                    ReviewCount = 14,
                    IsFeatured = true,
                    IsBestSeller = false
                },
                new Product
                {
                    CategoryId = catHopHoa.Id,
                    OccasionId = occLove.Id,
                    FlowerTypeId = ftRose.Id,
                    Name = "Hộp Hoa Mica Trong Suốt 'Eternal Glow'",
                    Slug = "hop-hoa-mica-eternal-glow",
                    SKU = "FL-GLO-04",
                    ShortDescription = "Hộp mica nắp vòm trong suốt kết hợp hoa hồng Ecuador và dây đèn LED ấm áp",
                    FullDescription = "Một kiệt tác quà tặng hiện đại: Hoa hồng tuyển chọn nằm gọn trong hộp mica chống bụi cao cấp, bao quanh là ánh sáng đom đóm lung linh của chuỗi đèn LED vàng ấm, có ngăn kéo bí mật bên dưới để đựng nhẫn hoặc son môi.",
                    FlowerCareGuide = "Hoa được cắm vào mút dưỡng ẩm sâu. Đèn LED sử dụng pin cúc áo có thể bật/tắt tùy ý.",
                    ColorTone = "Đỏ",
                    Price = 1450000,
                    SalePrice = 1350000,
                    StockQuantity = 25,
                    MainImageUrl = "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 5.0,
                    ReviewCount = 32,
                    IsFeatured = true,
                    IsBestSeller = true,
                    IsFlashSale = true
                },
                new Product
                {
                    CategoryId = catKeKhaiTruong.Id,
                    OccasionId = occKhaiTruong.Id,
                    FlowerTypeId = ftSunflower.Id,
                    Name = "Kệ Hoa Khai Trương 'Đại Phát Tài'",
                    Slug = "ke-hoa-khai-truong-dai-phat-tai",
                    SKU = "FL-KTR-05",
                    ShortDescription = "Kệ hoa 2 tầng phong cách hiện đại tone vàng cam rực rỡ với Hướng Dương & Hồng Cam Spirit",
                    FullDescription = "Kệ hoa chúc mừng khai trương 'Đại Phát Tài' được tạo tác từ hoa Hướng Dương đại đóa tượng trưng cho tiền tài nở rộ, cùng hoa Thiên Điểu vươn cao bay xa. Thiết kế chân sắt nghệ thuật sang trọng, vững chãi.",
                    FlowerCareGuide = "Tưới nước ẩm vào xốp cắm hoa 2 lần mỗi ngày để hoa giữ độ tươi đẹp từ 4-6 ngày trưng bày sự kiện.",
                    ColorTone = "Vàng",
                    Price = 1850000,
                    SalePrice = 1650000,
                    StockQuantity = 30,
                    MainImageUrl = "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 4.9,
                    ReviewCount = 22,
                    IsFeatured = true,
                    IsBestSeller = true
                },
                new Product
                {
                    CategoryId = catBinhHoa.Id,
                    OccasionId = occAnniversary.Id,
                    FlowerTypeId = ftLan.Id,
                    Name = "Bình Lan Hồ Điệp 'Vương Giả Quý Phái'",
                    Slug = "binh-lan-ho-diep-vuong-gia",
                    SKU = "FL-LAN-06",
                    ShortDescription = "Chậu sứ trắng 5 cành Lan Hồ Điệp tím hồng đột biến hoa to đều tăm tắp",
                    FullDescription = "Chậu Lan Hồ Điệp hoàng gia 5 cành uốn lượn phong thủy, gốc phủ rêu xanh tự nhiên và cành lũa mộc mạc. Lan có độ bền vượt trội từ 4 đến 8 tuần.",
                    FlowerCareGuide = "Tưới gốc mỗi tuần 1 lần lượng nước vừa phải (khoảng 150ml). Đặt nơi thoáng mát, có ánh sáng gián tiếp.",
                    ColorTone = "Tím",
                    Price = 2400000,
                    SalePrice = 2200000,
                    StockQuantity = 15,
                    MainImageUrl = "https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 5.0,
                    ReviewCount = 11,
                    IsFeatured = true,
                    IsBestSeller = false
                },
                new Product
                {
                    CategoryId = catBoHoa.Id,
                    OccasionId = occLove.Id,
                    FlowerTypeId = ftBaby.Id,
                    Name = "Bó Hoa Baby Trắng Khổng Lồ 'Pure Angel'",
                    Slug = "bo-hoa-baby-trang-pure-angel",
                    SKU = "FL-BAB-07",
                    ShortDescription = "Bó hoa Baby Hà Lan trắng tinh khôi bồng bềnh tựa mây trời bọc giấy kraft vintage",
                    FullDescription = "Bó hoa Baby trắng 'Pure Angel' kích thước khổng lồ tựa như một đám mây tuyết trắng ngần bồng bềnh. Hoa Baby Hà Lan có thể lưu giữ thành hoa khô tự nhiên chơi được hơn 1 năm mà không bị phai tàn.",
                    FlowerCareGuide = "Sau khi chưng tươi 1 tuần, có thể treo ngược nơi khô ráo thoáng gió để hoa khô tự nhiên giữ nguyên màu sắc.",
                    ColorTone = "Trắng",
                    Price = 650000,
                    SalePrice = 580000,
                    StockQuantity = 60,
                    MainImageUrl = "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 4.9,
                    ReviewCount = 45,
                    IsFeatured = true,
                    IsBestSeller = true
                },
                new Product
                {
                    CategoryId = catBoHoa.Id,
                    OccasionId = occBirthday.Id,
                    FlowerTypeId = ftRose.Id,
                    Name = "Bó Hoa Hồng Kem Dâu 'Strawberry Milk'",
                    Slug = "bo-hoa-hong-kem-dau-strawberry-milk",
                    SKU = "FL-MILK-08",
                    ShortDescription = "Bó hoa phối tone hồng kem dâu ngọt ngào với Hồng Juliet và Cúc Pingpong",
                    FullDescription = "Sự pha trộn màu sắc pastel đáng yêu ngọt ngào tựa ly trà sữa dâu tây. Rất thích hợp làm quà tặng sinh nhật bạn gái, em gái hoặc người bạn thân thiết.",
                    FlowerCareGuide = "Cắt gốc chéo, chưng bình nước sạch, đặt nơi mát mẻ.",
                    ColorTone = "Hồng",
                    Price = 520000,
                    SalePrice = 480000,
                    StockQuantity = 50,
                    MainImageUrl = "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80",
                    RatingAvg = 4.8,
                    ReviewCount = 17,
                    IsFeatured = false,
                    IsBestSeller = false
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            // 5. AddonItems
            var addons = new List<AddonItem>
            {
                new AddonItem { Name = "Gấu Bông Teddy Nơ Lụa (35cm)", Price = 180000, Type = "TeddyBear", ImageUrl = "https://images.unsplash.com/photo-1559454403-b8fb88521f11?auto=format&fit=crop&w=400&q=80" },
                new AddonItem { Name = "Hộp Chocolate Ferrero Rocher (16 viên)", Price = 220000, Type = "Chocolate", ImageUrl = "https://images.unsplash.com/photo-1549007994-cb92caebd54b?auto=format&fit=crop&w=400&q=80" },
                new AddonItem { Name = "Nến Thơm Tinh Dầu Lavender & Hoa Nhài", Price = 190000, Type = "ScentedCandle", ImageUrl = "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=400&q=80" },
                new AddonItem { Name = "Bóng Bay Khí Heli Trái Tim Rực Rỡ", Price = 90000, Type = "Balloons", ImageUrl = "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?auto=format&fit=crop&w=400&q=80" },
                new AddonItem { Name = "Bình Thủy Tinh Cắm Hoa Nghệ Thuật", Price = 150000, Type = "Vase", ImageUrl = "https://images.unsplash.com/photo-1612196808214-b8e1d6145a8c?auto=format&fit=crop&w=400&q=80" }
            };
            context.AddonItems.AddRange(addons);
            context.SaveChanges();

            // 6. Greeting Cards
            var cards = new List<GreetingCard>
            {
                new GreetingCard { Name = "Thiệp Hoa Khô Ép Kim 'Happy Birthday'", Theme = "Birthday", Price = 0, ImageUrl = "https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&w=400&q=80" },
                new GreetingCard { Name = "Thiệp Tình Yêu Handmade Trái Tim Đỏ", Theme = "Love", Price = 0, ImageUrl = "https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=400&q=80" },
                new GreetingCard { Name = "Thiệp Chúc Mừng Khai Trương Vàng Kim", Theme = "Congratulation", Price = 0, ImageUrl = "https://images.unsplash.com/photo-1589829085413-56de8ae18c73?auto=format&fit=crop&w=400&q=80" },
                new GreetingCard { Name = "Thiệp Tri Ân Tối Giản 'Thank You So Much'", Theme = "Gratitude", Price = 0, ImageUrl = "https://images.unsplash.com/photo-1607344645866-009c320c5ab8?auto=format&fit=crop&w=400&q=80" }
            };
            context.GreetingCards.AddRange(cards);
            context.SaveChanges();

            // 7. Vouchers
            var vouchers = new List<Voucher>
            {
                new Voucher { Code = "WELCOME50", Description = "Giảm ngay 50.000đ cho đơn hàng đầu tiên", DiscountAmount = 50000, MinOrderValue = 400000, StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now.AddMonths(6) },
                new Voucher { Code = "BLOOM10", Description = "Giảm 10% tối đa 100.000đ cho đơn hàng hoa tươi", DiscountPercent = 10, MaxDiscountAmount = 100000, MinOrderValue = 500000, StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now.AddMonths(6) },
                new Voucher { Code = "VIPLOVE", Description = "Ưu đãi đặc biệt VIP giảm 150.000đ đơn từ 1.200.000đ", DiscountAmount = 150000, MinOrderValue = 1200000, StartDate = DateTime.Now.AddDays(-30), EndDate = DateTime.Now.AddMonths(6) }
            };
            context.Vouchers.AddRange(vouchers);
            context.SaveChanges();

            // 8. Admin & Florist User
            var adminUser = new User
            {
                Username = "admin",
                // Mật khẩu mặc định: Admin@123
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                FullName = "Quản Trị Viên Shop Hoa",
                Email = "admin@flowershop.vn",
                Phone = "0909123456",
                Role = "Admin",
                IsActive = true
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}
