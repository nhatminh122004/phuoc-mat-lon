-- =======================================================================
-- Hữu Phước Florist - Full Database Script (MySQL 5.7 / 8.0 - Laragon ready)
-- -----------------------------------------------------------------------
-- Import: mở HeidiSQL/phpMyAdmin trong Laragon -> chạy toàn bộ file này.
-- Script idempotent: DROP + CREATE + seed, chạy lại nhiều lần vẫn sạch.
--
-- Nội dung:
--   1. 15 bảng (14 bảng nghiệp vụ + PasswordResetTokens)
--   2. 43 chỉ mục tối ưu cho tải truy vấn lớn (xem DAL/DbIndexProvisioner.cs)
--   3. Seed đầy đủ: 5 danh mục, 5 dịp, 6 loại hoa, 24 sản phẩm, 48 ảnh,
--      5 addon, 4 thiệp, 3 voucher, 3 tài khoản, 2 đơn demo, 12 đánh giá
--
-- Tài khoản mặc định (mật khẩu lưu dạng SHA-256, tự nâng cấp BCrypt khi đăng nhập):
--   admin    / Admin@123
--   florist  / Florist@123
--   customer / Customer@123
-- =======================================================================

CREATE DATABASE IF NOT EXISTS `FlowerShopDb` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `FlowerShopDb`;

SET NAMES utf8mb4;
SET SESSION sql_mode = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION';
SET FOREIGN_KEY_CHECKS = 0;

-- =======================================================================
-- 1. SCHEMA
-- =======================================================================

DROP TABLE IF EXISTS `PasswordResetTokens`;
DROP TABLE IF EXISTS `PaymentTransactions`;
DROP TABLE IF EXISTS `OrderAddons`;
DROP TABLE IF EXISTS `OrderDetails`;
DROP TABLE IF EXISTS `Orders`;
DROP TABLE IF EXISTS `Reviews`;
DROP TABLE IF EXISTS `ProductImages`;
DROP TABLE IF EXISTS `Products`;
DROP TABLE IF EXISTS `Vouchers`;
DROP TABLE IF EXISTS `GreetingCards`;
DROP TABLE IF EXISTS `AddonItems`;
DROP TABLE IF EXISTS `FlowerTypes`;
DROP TABLE IF EXISTS `Occasions`;
DROP TABLE IF EXISTS `Categories`;
DROP TABLE IF EXISTS `Users`;

CREATE TABLE `Categories` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `Description` VARCHAR(500) NULL,
  `ImageUrl` VARCHAR(300) NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Categories_Slug` (`Slug`),
  KEY `IX_Categories_Active_Order` (`IsActive`,`DisplayOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Occasions` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `IconClass` VARCHAR(200) NULL,
  `ImageUrl` VARCHAR(300) NULL,
  `Description` VARCHAR(500) NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Occasions_Slug` (`Slug`),
  KEY `IX_Occasions_Active_Order` (`IsActive`,`DisplayOrder`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `FlowerTypes` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `Meaning` VARCHAR(500) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_FlowerTypes_Slug` (`Slug`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Products` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `CategoryId` INT NOT NULL,
  `OccasionId` INT NULL,
  `FlowerTypeId` INT NULL,
  `Name` VARCHAR(200) NOT NULL,
  `Slug` VARCHAR(200) NOT NULL,
  `SKU` VARCHAR(50) NULL,
  `ShortDescription` VARCHAR(500) NULL,
  `FullDescription` TEXT NULL,
  `FlowerCareGuide` VARCHAR(1000) NULL,
  `ColorTone` VARCHAR(100) NULL,
  `Price` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `SalePrice` DECIMAL(18,0) NULL,
  `StockQuantity` INT NOT NULL DEFAULT 100,
  `MainImageUrl` VARCHAR(350) NULL,
  `RatingAvg` DOUBLE NOT NULL DEFAULT 5,
  `ReviewCount` INT NOT NULL DEFAULT 0,
  `IsFeatured` TINYINT(1) NOT NULL DEFAULT 0,
  `IsBestSeller` TINYINT(1) NOT NULL DEFAULT 0,
  `IsFlashSale` TINYINT(1) NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Products_Slug` (`Slug`),
  KEY `IX_Products_Category_Active` (`CategoryId`,`IsActive`),
  KEY `IX_Products_Occasion_Active` (`OccasionId`,`IsActive`),
  KEY `IX_Products_FlowerType_Active` (`FlowerTypeId`,`IsActive`),
  KEY `IX_Products_Active_Created` (`IsActive`,`CreatedAt`),
  KEY `IX_Products_Active_Price` (`IsActive`,`Price`),
  KEY `IX_Products_Active_Color` (`IsActive`,`ColorTone`),
  KEY `IX_Products_Active_BestSeller` (`IsActive`,`IsBestSeller`),
  KEY `IX_Products_Active_FlashSale` (`IsActive`,`IsFlashSale`),
  KEY `IX_Products_Active_Featured` (`IsActive`,`IsFeatured`),
  KEY `IX_Products_Active_Rating` (`IsActive`,`RatingAvg`),
  KEY `IX_Products_Category_Active_Price` (`CategoryId`,`IsActive`,`Price`),
  KEY `IX_Products_SKU` (`SKU`),
  FULLTEXT KEY `FT_Products_Search` (`Name`,`ShortDescription`,`FullDescription`),
  CONSTRAINT `FK_Products_Categories` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Products_Occasions` FOREIGN KEY (`OccasionId`) REFERENCES `Occasions` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Products_FlowerTypes` FOREIGN KEY (`FlowerTypeId`) REFERENCES `FlowerTypes` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `ProductImages` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `ProductId` INT NOT NULL,
  `ImageUrl` VARCHAR(350) NOT NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_ProductImages_Product_Order` (`ProductId`,`DisplayOrder`),
  CONSTRAINT `FK_ProductImages_Products` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `AddonItems` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Price` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `ImageUrl` VARCHAR(350) NULL,
  `Type` VARCHAR(50) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_AddonItems_Active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `GreetingCards` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `ImageUrl` VARCHAR(350) NULL,
  `Price` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `Theme` VARCHAR(100) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  KEY `IX_GreetingCards_Active` (`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Vouchers` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(50) NOT NULL,
  `Description` VARCHAR(250) NULL,
  `DiscountPercent` INT NULL,
  `DiscountAmount` DECIMAL(18,0) NULL,
  `MinOrderValue` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `MaxDiscountAmount` DECIMAL(18,0) NULL,
  `StartDate` DATETIME NULL,
  `EndDate` DATETIME NULL,
  `UsageLimit` INT NOT NULL DEFAULT 1000,
  `UsedCount` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Vouchers_Code` (`Code`),
  KEY `IX_Vouchers_Active_Dates` (`IsActive`,`StartDate`,`EndDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Users` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Username` VARCHAR(100) NOT NULL,
  `PasswordHash` VARCHAR(255) NOT NULL,
  `FullName` VARCHAR(150) NULL,
  `Email` VARCHAR(100) NULL,
  `Phone` VARCHAR(20) NULL,
  `Role` VARCHAR(50) NOT NULL DEFAULT 'Customer',
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Users_Username` (`Username`),
  UNIQUE KEY `UX_Users_Email` (`Email`),
  KEY `IX_Users_Role_Active` (`Role`,`IsActive`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `PasswordResetTokens` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `UserId` INT NOT NULL,
  `TokenHash` VARCHAR(64) NOT NULL,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ExpiresAt` DATETIME NOT NULL,
  `UsedAt` DATETIME NULL,
  `RequestedIp` VARCHAR(64) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_PRT_TokenHash` (`TokenHash`),
  KEY `IX_PRT_User_Created` (`UserId`,`CreatedAt`),
  KEY `IX_PRT_ExpiresAt` (`ExpiresAt`),
  CONSTRAINT `FK_PRT_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Orders` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderCode` VARCHAR(50) NOT NULL,
  `UserId` INT NULL,
  `SenderName` VARCHAR(100) NOT NULL,
  `SenderPhone` VARCHAR(20) NOT NULL,
  `SenderEmail` VARCHAR(100) NULL,
  `RecipientName` VARCHAR(100) NOT NULL,
  `RecipientPhone` VARCHAR(20) NOT NULL,
  `RecipientAddress` VARCHAR(250) NOT NULL,
  `District` VARCHAR(100) NULL,
  `City` VARCHAR(100) NULL,
  `DeliveryDate` DATETIME NOT NULL,
  `DeliveryTimeSlot` VARCHAR(50) NULL,
  `IsAnonymous` TINYINT(1) NOT NULL DEFAULT 0,
  `GreetingCardId` INT NULL,
  `CardMessage` VARCHAR(1000) NULL,
  `OrderNotes` VARCHAR(2000) NULL,
  `ProofPhotoUrl` VARCHAR(2000) NULL,
  `SubTotal` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `AddonTotal` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `ShippingFee` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `DiscountAmount` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `TotalAmount` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `VoucherCode` VARCHAR(50) NULL,
  `PaymentMethod` VARCHAR(50) NOT NULL DEFAULT 'COD',
  `PaymentStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
  `OrderStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` DATETIME NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Orders_OrderCode` (`OrderCode`),
  KEY `IX_Orders_CreatedAt` (`CreatedAt`),
  KEY `IX_Orders_Status_CreatedAt` (`OrderStatus`,`CreatedAt`),
  KEY `IX_Orders_PaymentStatus_CreatedAt` (`PaymentStatus`,`CreatedAt`),
  KEY `IX_Orders_User_CreatedAt` (`UserId`,`CreatedAt`),
  KEY `IX_Orders_SenderEmail` (`SenderEmail`,`CreatedAt`),
  KEY `IX_Orders_SenderPhone` (`SenderPhone`),
  KEY `IX_Orders_RecipientPhone` (`RecipientPhone`),
  KEY `IX_Orders_DeliveryDate_Status` (`DeliveryDate`,`OrderStatus`),
  CONSTRAINT `FK_Orders_GreetingCards` FOREIGN KEY (`GreetingCardId`) REFERENCES `GreetingCards` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `OrderDetails` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `ProductId` INT NOT NULL,
  `ProductName` VARCHAR(200) NOT NULL,
  `ProductImageUrl` VARCHAR(350) NULL,
  `UnitPrice` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `Quantity` INT NOT NULL DEFAULT 1,
  `TotalPrice` DECIMAL(18,0) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderDetails_Order` (`OrderId`),
  KEY `IX_OrderDetails_Product` (`ProductId`),
  CONSTRAINT `FK_OrderDetails_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderDetails_Products` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `OrderAddons` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `AddonItemId` INT NOT NULL,
  `AddonName` VARCHAR(150) NOT NULL,
  `UnitPrice` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `Quantity` INT NOT NULL DEFAULT 1,
  `TotalPrice` DECIMAL(18,0) NOT NULL DEFAULT 0,
  PRIMARY KEY (`Id`),
  KEY `IX_OrderAddons_Order` (`OrderId`),
  CONSTRAINT `FK_OrderAddons_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OrderAddons_AddonItems` FOREIGN KEY (`AddonItemId`) REFERENCES `AddonItems` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `PaymentTransactions` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `Gateway` VARCHAR(50) NOT NULL,
  `TransactionNo` VARCHAR(100) NULL,
  `GatewayResponseCode` VARCHAR(50) NULL,
  `Amount` DECIMAL(18,0) NOT NULL DEFAULT 0,
  `Currency` VARCHAR(10) NULL DEFAULT 'VND',
  `PayDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `RawPayload` TEXT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_PaymentTransactions_Gateway_Txn` (`Gateway`,`TransactionNo`),
  KEY `IX_PaymentTransactions_Order` (`OrderId`),
  CONSTRAINT `FK_PaymentTransactions_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `Reviews` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `ProductId` INT NOT NULL,
  `CustomerName` VARCHAR(100) NOT NULL,
  `Rating` INT NOT NULL DEFAULT 5,
  `Comment` VARCHAR(1000) NULL,
  `ImageUrl` VARCHAR(2000) NULL,
  `IsApproved` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `IX_Reviews_Product_Approved_Created` (`ProductId`,`IsApproved`,`CreatedAt`),
  CONSTRAINT `FK_Reviews_Products` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =======================================================================
-- 2. SEED DATA
-- =======================================================================

INSERT INTO `Categories` (`Id`,`Name`,`Slug`,`Description`,`ImageUrl`,`DisplayOrder`,`IsActive`) VALUES
(1,'Bó Hoa Tươi','bo-hoa-tuoi','Những bó hoa nghệ thuật thiết kế tinh tế từ các nghệ nhân cắm hoa hàng đầu','https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=600&q=80',1,1),
(2,'Giỏ & Lẵng Hoa','gio-lang-hoa','Giỏ hoa gỗ, mây tre đan sang trọng phù hợp trưng bày phòng khách và văn phòng','https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=600&q=80',2,1),
(3,'Bình Hoa Cao Cấp','binh-hoa-cao-cap','Bình gốm sứ & pha lê nghệ thuật cắm sẵn sang trọng quý phái','https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=600&q=80',3,1),
(4,'Hộp Hoa & Mica LED','hop-hoa-mica-led','Hộp quà hoa bí mật nắp mở đính kèm dây đèn LED đom đóm lãng mạn','https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=600&q=80',4,1),
(5,'Kệ Hoa Khai Trương','ke-hoa-khai-truong','Kệ hoa 2-3 tầng đại tài lộc mừng hồng phát, tân gia và kỷ niệm thành lập','https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=600&q=80',5,1);

INSERT INTO `Occasions` (`Id`,`Name`,`Slug`,`IconClass`,`Description`,`DisplayOrder`,`IsActive`) VALUES
(1,'Hoa Sinh Nhật','sinh-nhat','fa-cake-candles','Trao gửi lời chúc tuổi mới rạng rỡ và tràn đầy may mắn',1,1),
(2,'Tình Yêu & Valentine','tinh-yeu','fa-heart','Bày tỏ tình cảm chân thành, kỷ niệm ngày yêu và lời cầu hôn ngọt ngào',2,1),
(3,'Khai Trương - Chúc Mừng','khai-truong','fa-champagne-glasses','Cung chúc phát tài, công việc hanh thông, vạn sự cát tường',3,1),
(4,'Kỷ Niệm Ngày Cưới','ky-niem','fa-ring','Kỷ niệm những chặng đường hạnh phúc thủy chung bên nhau',4,1),
(5,'Tri Ân & Ngày Của Mẹ','tri-an-me','fa-hands-holding-child','Biết ơn công lao sinh thành của cha mẹ, thầy cô',5,1);

INSERT INTO `FlowerTypes` (`Id`,`Name`,`Slug`,`Meaning`,`IsActive`) VALUES
(1,'Hoa Hồng Ecuador & Ohara','hoa-hong','Tình yêu mãnh liệt, vẻ đẹp thanh lịch và sự trân quý tuyệt đối',1),
(2,'Hoa Tulip Hà Lan','hoa-tulip','Lời tỏ tình thuần khiết, sự hoàn hảo và tình cảm chân thành',1),
(3,'Hoa Baby Trắng','hoa-baby','Tình yêu ngây thơ, sự trong sáng thuần khiết vĩnh hằng',1),
(4,'Lan Hồ Điệp Hoàng Gia','lan-ho-diep','Sung túc, vương giả, tài lộc bền vững và trường thọ',1),
(5,'Hoa Hướng Dương','hoa-huong-duong','Niềm tin, hy vọng, luôn hướng về phía ánh mặt trời',1),
(6,'Cẩm Tú Cầu','cam-tu-cau','Lòng biết ơn sâu sắc và cảm xúc chân thành xuất phát từ trái tim',1);

INSERT INTO `Products` (`Id`,`CategoryId`,`OccasionId`,`FlowerTypeId`,`Name`,`Slug`,`SKU`,`ShortDescription`,`FullDescription`,`FlowerCareGuide`,`ColorTone`,`Price`,`SalePrice`,`StockQuantity`,`MainImageUrl`,`RatingAvg`,`ReviewCount`,`IsFeatured`,`IsBestSeller`,`IsFlashSale`,`IsActive`) VALUES
(1,1,2,1,'Bó Hoa Hồng Đỏ Ecuador ''Red Romance''','bo-hoa-hong-do-ecuador-red-romance','FL-ROM-01','33 cành hồng đỏ Ecuador cỡ đại phối lá bạc Dollar bọc giấy lụa đen sang trọng viền vàng','Bó hoa ''Red Romance'' tuyển chọn 33 cành hồng đỏ Ecuador nhập khẩu cánh dày mịn như nhung. Kết hợp cùng lá bạc nhập khẩu và giấy gói lụa cao cấp, mang thông điệp ''Yêu em say đắm bằng cả con tim''.','Cắt xéo gốc 45 độ, thay nước sạch mỗi ngày, thêm gói dưỡng hoa FloraLife tặng kèm.','Đỏ',950000,850000,50,'https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80',5.0,28,1,1,1,1),
(2,1,1,2,'Bó Hoa Tulip Hồng ''Sweet Whispers''','bo-hoa-tulip-hong-sweet-whispers','FL-TUL-02','20 cành Tulip hồng phấn Hà Lan tượng trưng cho tình cảm ngọt ngào, tinh tế','Bó hoa Tulip ''Sweet Whispers'' mang sắc hồng dịu dàng đầy mê hoặc từ củ giống Hà Lan tuyển chọn, cắm theo phong cách tối giản Hàn Quốc.','Tulip ưa nước đá lạnh. Thả vài viên đá lạnh vào bình mỗi sáng để giữ cành luôn cứng cáp.','Hồng',1200000,1050000,35,'https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=800&q=80',4.9,19,1,1,0,1),
(3,2,5,6,'Giỏ Hoa Mây ''Pastel Harmony''','gio-hoa-may-pastel-harmony','FL-PAS-03','Giỏ hoa phối tone màu pastel thanh nhã gồm Cẩm Tú Cầu, Hồng Ohara và Cúc Tana','Giỏ hoa nghệ thuật đan mây tự nhiên ''Pastel Harmony'' phối màu pastel mềm mại, tạo cảm giác thư thái và an yên.','Xịt phun sương nhẹ lên cánh hoa cẩm tú cầu vào buổi sáng, châm 100ml nước vào xốp mỗi ngày.','Hồng',780000,690000,40,'https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80',4.8,14,1,0,0,1),
(4,4,2,1,'Hộp Hoa Mica Trong Suốt ''Eternal Glow''','hop-hoa-mica-eternal-glow','FL-GLO-04','Hộp mica nắp vòm trong suốt kết hợp hoa hồng Ecuador và dây đèn LED ấm áp','Hoa hồng tuyển chọn nằm gọn trong hộp mica chống bụi cao cấp, bao quanh là ánh sáng đom đóm lung linh của chuỗi đèn LED vàng ấm, có ngăn kéo bí mật bên dưới.','Hoa được cắm vào mút dưỡng ẩm sâu. Đèn LED sử dụng pin cúc áo có thể bật/tắt tùy ý.','Đỏ',1450000,1350000,25,'https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80',5.0,32,1,1,1,1),
(5,5,3,5,'Kệ Hoa Khai Trương ''Đại Phát Tài''','ke-hoa-khai-truong-dai-phat-tai','FL-KTR-05','Kệ hoa 2 tầng hiện đại tone vàng cam rực rỡ với Hướng Dương & Hồng Cam Spirit','Kệ hoa chúc mừng khai trương ''Đại Phát Tài'' tạo tác từ hoa Hướng Dương đại đóa tượng trưng cho tiền tài nở rộ, cùng hoa Thiên Điểu vươn cao bay xa.','Tưới nước ẩm vào xốp cắm hoa 2 lần mỗi ngày để hoa giữ độ tươi 4-6 ngày.','Vàng',1850000,1650000,30,'https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80',4.9,22,1,1,0,1),
(6,3,4,4,'Bình Lan Hồ Điệp ''Vương Giả Quý Phái''','binh-lan-ho-diep-vuong-gia','FL-LAN-06','Chậu sứ trắng 5 cành Lan Hồ Điệp tím hồng đột biến hoa to đều tăm tắp','Chậu Lan Hồ Điệp hoàng gia 5 cành uốn lượn phong thủy, gốc phủ rêu xanh tự nhiên và cành lũa mộc mạc. Lan có độ bền 4-8 tuần.','Tưới gốc mỗi tuần 1 lần khoảng 150ml. Đặt nơi thoáng mát, ánh sáng gián tiếp.','Tím',2400000,2200000,15,'https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=800&q=80',5.0,11,1,0,0,1),
(7,1,2,3,'Bó Hoa Baby Trắng Khổng Lồ ''Pure Angel''','bo-hoa-baby-trang-pure-angel','FL-BAB-07','Bó hoa Baby Hà Lan trắng tinh khôi bồng bềnh tựa mây trời bọc giấy kraft vintage','Bó hoa Baby trắng ''Pure Angel'' kích thước khổng lồ tựa đám mây tuyết trắng ngần. Hoa Baby Hà Lan có thể lưu giữ thành hoa khô chơi hơn 1 năm.','Sau khi chưng tươi 1 tuần, treo ngược nơi khô ráo để hoa khô tự nhiên giữ nguyên màu sắc.','Trắng',650000,580000,60,'https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80',4.9,45,1,1,0,1),
(8,1,1,1,'Bó Hoa Hồng Kem Dâu ''Strawberry Milk''','bo-hoa-hong-kem-dau-strawberry-milk','FL-MILK-08','Bó hoa phối tone hồng kem dâu ngọt ngào với Hồng Juliet và Cúc Pingpong','Sự pha trộn màu sắc pastel đáng yêu ngọt ngào tựa ly trà sữa dâu tây. Rất thích hợp làm quà tặng sinh nhật bạn gái, em gái hoặc người bạn thân thiết.','Cắt gốc chéo, chưng bình nước sạch, đặt nơi mát mẻ.','Hồng',520000,480000,50,'https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80',4.8,17,0,0,0,1),
(9,1,1,6,'Bó Hoa Cẩm Tú Cầu Xanh Pastel ''Cloud Nine''','bo-hoa-cam-tu-cau-xanh-pastel-cloud-nine','FL-HYD-09','Cẩm tú cầu xanh pastel kết hợp baby trắng trong lớp giấy Hàn Quốc mềm mại','Sắc xanh dịu của cẩm tú cầu tượng trưng cho sự bình yên và lòng biết ơn.','Ngâm chân hoa vào nước ấm 30 giây rồi chưng bình mát, thay nước mỗi 2 ngày.','Xanh',890000,790000,40,'https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80',4.7,14,0,1,0,1),
(10,2,3,5,'Giỏ Hoa Hướng Dương ''Sunrise Fortune''','gio-hoa-huong-duong-sunrise-fortune','FL-SUN-10','Giỏ mây đan 9 bông hướng dương rực rỡ, biểu tượng tài lộc và niềm tin','Hướng dương luôn hướng về phía mặt trời - biểu trưng cho ý chí vươn lên và vận may.','Giữ nước sạch, tránh gió lạnh trực tiếp, cắt gốc 45 độ mỗi 2 ngày.','Vàng',1450000,NULL,25,'https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80',4.9,22,0,1,0,1),
(11,5,3,4,'Kệ Hoa Lan Hồ Điệp ''Royal Prosperity''','ke-hoa-lan-ho-diep-royal-prosperity','FL-ORC-11','Kệ 3 tầng lan hồ điệp tím hoàng gia, món quà khai trương đẳng cấp','Lan hồ điệp tím tượng trưng cho sự sung túc và vương giả. Kệ 3 tầng kết hợp cẩm tú cầu trắng và lá trâu bạc.','Tưới sương 2 lần/tuần, đặt nơi thoáng mát có ánh sáng gián tiếp.','Tím',3200000,2950000,12,'https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80',5.0,31,1,0,0,1),
(12,4,2,1,'Hộp Hoa Mica LED ''Starlight''','hop-hoa-mica-led-starlight','FL-LED-12','Hộp mica trong suốt đính LED đom đóm cùng 19 bông hồng Ohara','Hộp quà bí mật nắp mở, đèn LED đom đóm tạo hiệu ứng lung linh khi mở nắp.','Tháo đế xốp giữ ẩm, chưng bình mát, tránh ánh nắng trực tiếp.','Hồng',1150000,NULL,30,'https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80',4.8,19,0,0,1,1),
(13,1,4,2,'Bó Hoa Tulip Trắng ''Pure Elegance''','bo-hoa-tulip-trang-pure-elegance','FL-TUL-13','25 cành tulip trắng Hà Lan thanh khiết, gói giấy lụa kem tối giản','Tulip trắng là lời tỏ tình thuần khiết và lời chúc bình an, phong cách tối giản Bắc Âu.','Tulip ưa nước mát, thêm viên đá lạnh mỗi sáng để giữ cành cứng cáp.','Trắng',980000,NULL,35,'https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=800&q=80',4.6,11,0,0,0,1),
(14,1,5,3,'Bó Hoa Baby Trắng Mini ''Little Angel''','bo-hoa-baby-trang-mini-little-angel','FL-BAB-14','Baby trắng bó mini trong giấy kraft, quà tặng nhỏ xinh dưới 500k','Món quà nhỏ nhưng đầy ý nghĩa cho mẹ, cô giáo hay bạn thân.','Baby khô tự nhiên đẹp nhất, giữ nơi khô ráo, tránh nước đọng.','Trắng',390000,350000,80,'https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80',4.5,26,0,0,1,1),
(15,3,4,6,'Bình Hoa Cẩm Tú Cầu Hồng ''Blush Bloom''','binh-hoa-cam-tu-cau-hong-blush-bloom','FL-HYD-15','Bình gốm trắng cao cấp cắm cẩm tú cầu hồng và hồng Juliet','Bình gốm trắng nhập khẩu kết hợp cẩm tú cầu hồng phấn và hồng Juliet cao cấp.','Thay nước mỗi 2 ngày, cắt gốc 45 độ, giữ xa hơi nóng.','Hồng',1250000,NULL,18,'https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=800&q=80',4.8,9,1,0,0,1),
(16,2,4,1,'Giỏ Hoa Cưới Trắng Xanh ''Wedding Whisper''','gio-hoa-cuoi-trang-xanh-wedding-whisper','FL-WED-16','Giỏ hoa cưới tone trắng xanh, phối hồng trắng và lan hồ điệp','Thiết kế dành cho lễ cưới, lễ đính hôn hoặc kỷ niệm ngày cưới.','Giữ ẩm đế xốp, đặt nơi mát, tránh nắng trực tiếp.','Trắng',1850000,NULL,15,'https://images.unsplash.com/photo-1519225421980-715cb0215aed?auto=format&fit=crop&w=800&q=80',4.9,12,0,0,0,1),
(17,1,3,1,'Bó Hoa Hồng Vàng ''Golden Wish''','bo-hoa-hong-vang-golden-wish','FL-ROM-17','33 bông hồng vàng Ecuador - lời chúc thịnh vượng và khởi đầu may mắn','Hồng vàng tượng trưng cho tình bạn bền vững và lời chúc thành công.','Cắt gốc, thay nước sạch mỗi ngày, tránh ánh nắng gắt.','Vàng',780000,720000,45,'https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80',4.4,8,0,0,0,1),
(18,4,2,1,'Hộp Hoa Hồng Đỏ ''Love Letter''','hop-hoa-hong-do-love-letter','FL-BOX-18','Hộp nhung đỏ 24 bông hồng đỏ Ecuador kèm thiệp viết tay','Hộp nhung đỏ sang trọng chứa 24 bông hồng đỏ Ecuador, kèm thiệp ép kim viết tay.','Giữ hộp nơi mát, mở nắp để hoa thoáng khí 2 lần/ngày.','Đỏ',1350000,NULL,22,'https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80',4.9,24,0,1,0,1),
(19,1,5,4,'Bó Hoa Lan Hồ Điệp Trắng ''Snow Queen''','bo-hoa-lan-ho-diep-trang-snow-queen','FL-ORC-19','Bó lan hồ điệp trắng 7 nhánh - món quà tri ân sang trọng cho Mẹ','Lan hồ điệp trắng biểu trưng cho sự trường thọ và lòng biết ơn.','Tưới sương nhẹ 2 lần/tuần, tránh nước đọng ở nách lá.','Trắng',1650000,1490000,20,'https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80',4.9,16,1,0,0,1),
(20,2,1,3,'Giỏ Hoa Baby Trắng Pastel ''Cloud Basket''','gio-hoa-baby-trang-pastel-cloud-basket','FL-BAS-20','Giỏ mây nhỏ cắm baby trắng và cẩm tú cầu xanh nhạt','Giỏ mây tự nhiên đựng baby trắng, cẩm tú cầu xanh nhạt và lá bạc.','Giữ khô thoáng, tránh tưới trực tiếp lên hoa.','Trắng',690000,NULL,28,'https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80',4.3,7,0,0,0,1),
(21,3,3,5,'Bình Hoa Hướng Dương Văn Phòng ''Office Sun''','binh-hoa-huong-duong-van-phong-office-sun','FL-SUN-21','Bình thủy tinh cắm 5 hướng dương nhỏ để bàn làm việc','Thiết kế gọn cho bàn làm việc, mang năng lượng tích cực mỗi ngày.','Thay nước 2 ngày/lần, cắt gốc 45 độ.','Vàng',560000,520000,33,'https://images.unsplash.com/photo-1612196808214-b8e1d6145a8c?auto=format&fit=crop&w=800&q=80',4.2,6,0,0,1,1),
(22,1,1,6,'Bó Hoa Cẩm Tú Cầu Tím ''Lavender Dream''','bo-hoa-cam-tu-cau-tim-lavender-dream','FL-HYD-22','Cẩm tú cầu tím phối baby trắng trong giấy lụa Hàn Quốc','Sắc tím lãng mạn của cẩm tú cầu kết hợp baby trắng tạo nên bó hoa nhẹ nhàng.','Chưng nước sạch, tránh nắng trực tiếp, cắt gốc mỗi 2 ngày.','Tím',820000,NULL,26,'https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80',4.6,10,0,0,0,1),
(23,5,3,1,'Kệ Hoa Chúc Mừng 2 Tầng ''Success Bloom''','ke-hoa-chuc-mung-2-tang-success-bloom','FL-KE2-23','Kệ 2 tầng hồng đỏ và lan hồ điệp, kèm băng rôn chúc mừng theo yêu cầu','Kệ hoa 2 tầng cỡ đại cho khai trương, in băng rôn theo tên công ty.','Tưới ẩm đế xốp 1 lần/ngày, đặt nơi thoáng mát.','Đỏ',2450000,NULL,10,'https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80',4.7,13,0,0,0,1),
(24,4,2,1,'Hộp Quà Hoa & Chocolate ''Sweet Combo''','hop-qua-hoa-chocolate-sweet-combo','FL-COM-24','Combo hộp hoa hồng Ohara + 16 viên chocolate Ferrero','Combo quà tặng hoàn hảo: 12 bông hồng Ohara trong hộp kraft cao cấp kèm hộp chocolate Ferrero Rocher 16 viên.','Bảo quản chocolate nơi khô mát, hoa chưng bình riêng sau khi nhận.','Hồng',990000,890000,24,'https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80',4.5,15,0,0,0,1);

-- Gallery: 2 ảnh/sản phẩm (ảnh chính + ảnh phối cảnh)
INSERT INTO `ProductImages` (`ProductId`,`ImageUrl`,`DisplayOrder`)
SELECT `Id`, `MainImageUrl`, 0 FROM `Products`;
INSERT INTO `ProductImages` (`ProductId`,`ImageUrl`,`DisplayOrder`)
SELECT `Id`, 'https://images.unsplash.com/photo-1490750967868-88aa4486c946?auto=format&fit=crop&w=800&q=80', 1 FROM `Products`;

INSERT INTO `AddonItems` (`Id`,`Name`,`Price`,`ImageUrl`,`Type`,`IsActive`) VALUES
(1,'Gấu Bông Teddy Nơ Lụa (35cm)',180000,'https://images.unsplash.com/photo-1559454403-b8fb88521f11?auto=format&fit=crop&w=400&q=80','TeddyBear',1),
(2,'Hộp Chocolate Ferrero Rocher (16 viên)',220000,'https://images.unsplash.com/photo-1549007994-cb92caebd54b?auto=format&fit=crop&w=400&q=80','Chocolate',1),
(3,'Nến Thơm Tinh Dầu Lavender & Hoa Nhài',190000,'https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=400&q=80','ScentedCandle',1),
(4,'Bóng Bay Khí Heli Trái Tim Rực Rỡ',90000,'https://images.unsplash.com/photo-1530103862676-de8c9debad1d?auto=format&fit=crop&w=400&q=80','Balloons',1),
(5,'Bình Thủy Tinh Cắm Hoa Nghệ Thuật',150000,'https://images.unsplash.com/photo-1612196808214-b8e1d6145a8c?auto=format&fit=crop&w=400&q=80','Vase',1);

INSERT INTO `GreetingCards` (`Id`,`Name`,`ImageUrl`,`Price`,`Theme`,`IsActive`) VALUES
(1,'Thiệp Hoa Khô Ép Kim ''Happy Birthday''','https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&w=400&q=80',0,'Birthday',1),
(2,'Thiệp Tình Yêu Handmade Trái Tim Đỏ','https://images.unsplash.com/photo-1513519245088-0e12902e5a38?auto=format&fit=crop&w=400&q=80',0,'Love',1),
(3,'Thiệp Chúc Mừng Khai Trương Vàng Kim','https://images.unsplash.com/photo-1589829085413-56de8ae18c73?auto=format&fit=crop&w=400&q=80',0,'Congratulation',1),
(4,'Thiệp Tri Ân Tối Giản ''Thank You So Much''','https://images.unsplash.com/photo-1607344645866-009c320c5ab8?auto=format&fit=crop&w=400&q=80',0,'Gratitude',1);

INSERT INTO `Vouchers` (`Id`,`Code`,`Description`,`DiscountPercent`,`DiscountAmount`,`MinOrderValue`,`MaxDiscountAmount`,`StartDate`,`EndDate`,`UsageLimit`,`UsedCount`,`IsActive`) VALUES
(1,'WELCOME50','Giảm ngay 50.000đ cho đơn hàng đầu tiên',NULL,50000,400000,NULL,NOW() - INTERVAL 30 DAY,NOW() + INTERVAL 6 MONTH,1000,0,1),
(2,'BLOOM10','Giảm 10% tối đa 100.000đ cho đơn hàng hoa tươi',10,NULL,500000,100000,NOW() - INTERVAL 30 DAY,NOW() + INTERVAL 6 MONTH,500,0,1),
(3,'VIPLOVE','Ưu đãi VIP giảm 150.000đ đơn từ 1.200.000đ',NULL,150000,1200000,NULL,NOW() - INTERVAL 30 DAY,NOW() + INTERVAL 6 MONTH,200,0,1);

-- Mật khẩu dạng SHA-256 (PasswordService.Verify hỗ trợ sẵn và tự nâng cấp sang BCrypt khi đăng nhập)
INSERT INTO `Users` (`Id`,`Username`,`PasswordHash`,`FullName`,`Email`,`Phone`,`Role`,`IsActive`) VALUES
(1,'admin','e86f78a8a3caf0b60d8e74e5942aa6d86dc150cd3c03338aef25b7d2d7e3acc7','Quản Trị Viên Shop Hoa','admin@flowershop.vn','0909123456','Admin',1),
(2,'customer','98ec654a8df28f8f0f8f02220483d46916b85017de0b74d8ec755c28cb8539a8','Khách Hàng Mẫu','customer@flowershop.vn','0901234567','Customer',1),
(3,'florist','a09012c0ec4130bc941892281c041a2ba07dcceb8b1b5d26059744c3f7d6035f','Nghệ Nhân Cắm Hoa','florist@flowershop.vn','0902345678','Florist',1);

-- 2 đơn demo: 1 chờ khách duyệt ảnh + 1 đã giao & đã thanh toán
INSERT INTO `Orders` (`Id`,`OrderCode`,`UserId`,`SenderName`,`SenderPhone`,`SenderEmail`,`RecipientName`,`RecipientPhone`,`RecipientAddress`,`District`,`City`,`DeliveryDate`,`DeliveryTimeSlot`,`IsAnonymous`,`GreetingCardId`,`CardMessage`,`OrderNotes`,`ProofPhotoUrl`,`SubTotal`,`AddonTotal`,`ShippingFee`,`DiscountAmount`,`TotalAmount`,`VoucherCode`,`PaymentMethod`,`PaymentStatus`,`OrderStatus`,`CreatedAt`,`UpdatedAt`) VALUES
(1,'FL260101AB2CD',2,'Nguyễn Văn An','0377415147','an.nguyen@example.com','Trần Ngọc Mai','0909123456','601/8/17 Cách Mạng Tháng 8, Phường Hoà Hưng, Quận 10','Quận 10','Hồ Chí Minh',NOW() + INTERVAL 1 DAY,'10:00 - 12:00',0,1,'Chúc em một ngày thật rực rỡ và nhiều niềm vui!','Gọi trước khi giao 15 phút.','https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80',850000,0,0,0,850000,NULL,'COD','Pending','ProofReady',NOW() - INTERVAL 3 DAY,NULL),
(2,'FL260105XY7ZW',NULL,'Lý Bảo Ngọc','0912345678','ngoc.ly@example.com','Phạm Thanh Tùng','0987654321','23 Nguyễn Huệ, Phường Bến Nghé, Quận 1','Quận 1','Hồ Chí Minh',NOW() - INTERVAL 8 DAY,'18:00 - 20:00',1,2,'Secret admirer của bạn','',NULL,850000,0,0,0,850000,NULL,'SEPAY','Paid','Delivered',NOW() - INTERVAL 9 DAY,NOW() - INTERVAL 8 DAY);

INSERT INTO `OrderDetails` (`Id`,`OrderId`,`ProductId`,`ProductName`,`ProductImageUrl`,`UnitPrice`,`Quantity`,`TotalPrice`) VALUES
(1,1,1,'Bó Hoa Hồng Đỏ Ecuador ''Red Romance''','https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80',850000,1,850000),
(2,2,1,'Bó Hoa Hồng Đỏ Ecuador ''Red Romance''','https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80',850000,1,850000);

INSERT INTO `PaymentTransactions` (`Id`,`OrderId`,`Gateway`,`TransactionNo`,`GatewayResponseCode`,`Amount`,`Currency`,`PayDate`,`RawPayload`) VALUES
(1,2,'SEPAY','DEMO-SEPAY-00001','00',850000,'VND',NOW() - INTERVAL 8 DAY,'seed-demo');

INSERT INTO `Reviews` (`Id`,`ProductId`,`CustomerName`,`Rating`,`Comment`,`ImageUrl`,`IsApproved`,`CreatedAt`) VALUES
(1,1,'Nguyễn Thu Hà',5,'Hoa tươi lâu, đóng gói rất kỹ. Shipper giao đúng khung giờ đã chọn.',NULL,1,NOW() - INTERVAL 2 DAY),
(2,2,'Trần Minh Quân',5,'Bó hoa đẹp hơn ảnh, người nhận rất thích. Sẽ đặt lại lần nữa.',NULL,1,NOW() - INTERVAL 4 DAY),
(3,3,'Lê Phương Thảo',4,'Hoa đẹp, mùi thơm nhẹ. Nhân viên tư vấn nhiệt tình.',NULL,1,NOW() - INTERVAL 5 DAY),
(4,4,'Phạm Anh Khoa',5,'Hộp mica sang xịn, đèn LED lung linh. Bạn gái mình rất thích.',NULL,1,NOW() - INTERVAL 6 DAY),
(5,5,'Đỗ Thị Kim Ngân',5,'Kệ hoa to hơn mong đợi, giao đúng giờ khai trương.',NULL,1,NOW() - INTERVAL 7 DAY),
(6,6,'Vũ Hoàng Nam',5,'Lan bền thật, để được hơn 3 tuần vẫn tươi.',NULL,1,NOW() - INTERVAL 8 DAY),
(7,7,'Ngô Thanh Huyền',5,'Baby to bồng bềnh cực xinh, chụp ảnh rất đẹp.',NULL,1,NOW() - INTERVAL 9 DAY),
(8,8,'Bùi Minh Thư',4,'Tone hồng dễ thương, đóng gói chắc chắn.',NULL,1,NOW() - INTERVAL 10 DAY),
(9,9,'Trịnh Gia Hân',5,'Cẩm tú cầu xanh nhìn mát mắt, sẽ ủng hộ tiếp.',NULL,1,NOW() - INTERVAL 11 DAY),
(10,10,'Lương Thành Đạt',5,'Giỏ hướng dương rất rực rỡ, khách hàng của tôi khen.',NULL,1,NOW() - INTERVAL 12 DAY),
(11,18,'Hoàng Yến Nhi',5,'Hộp nhung đỏ sang trọng, thiệp viết tay chỉn chu.',NULL,1,NOW() - INTERVAL 13 DAY),
(12,24,'Nguyễn Bảo Trân',4,'Combo tiện lợi, chocolate còn hạn dài.',NULL,1,NOW() - INTERVAL 14 DAY);

SET FOREIGN_KEY_CHECKS = 1;

-- =======================================================================
-- 3. CẬP NHẬT THỐNG KÊ + KIỂM TRA NHANH
-- =======================================================================
ANALYZE TABLE `Categories`, `Occasions`, `FlowerTypes`, `Products`, `ProductImages`,
              `AddonItems`, `GreetingCards`, `Vouchers`, `Users`, `PasswordResetTokens`,
              `Orders`, `OrderDetails`, `OrderAddons`, `PaymentTransactions`, `Reviews`;

SELECT 'Categories' AS `Table`, COUNT(*) AS `Rows` FROM `Categories`
UNION ALL SELECT 'Occasions', COUNT(*) FROM `Occasions`
UNION ALL SELECT 'FlowerTypes', COUNT(*) FROM `FlowerTypes`
UNION ALL SELECT 'Products', COUNT(*) FROM `Products`
UNION ALL SELECT 'ProductImages', COUNT(*) FROM `ProductImages`
UNION ALL SELECT 'AddonItems', COUNT(*) FROM `AddonItems`
UNION ALL SELECT 'GreetingCards', COUNT(*) FROM `GreetingCards`
UNION ALL SELECT 'Vouchers', COUNT(*) FROM `Vouchers`
UNION ALL SELECT 'Users', COUNT(*) FROM `Users`
UNION ALL SELECT 'Orders', COUNT(*) FROM `Orders`
UNION ALL SELECT 'OrderDetails', COUNT(*) FROM `OrderDetails`
UNION ALL SELECT 'PaymentTransactions', COUNT(*) FROM `PaymentTransactions`
UNION ALL SELECT 'Reviews', COUNT(*) FROM `Reviews`;

-- =======================================================================
-- 4. GHI CHÚ CẤU HÌNH MYSQL CHO TẢI LỚN (Laragon -> my.ini)
-- -----------------------------------------------------------------------
-- innodb_buffer_pool_size      = 2G          (≈70% RAM nếu server chỉ chạy MySQL)
-- innodb_log_file_size         = 512M
-- innodb_flush_log_at_trx_commit = 2         (trade-off an toàn/hiệu năng)
-- max_connections              = 300         (ASP.NET pool mặc định 100 conn)
-- table_open_cache             = 4000
-- innodb_io_capacity           = 2000
-- tmp_table_size/max_heap_table_size = 64M
-- slow_query_log = 1, long_query_time = 1
-- =======================================================================
