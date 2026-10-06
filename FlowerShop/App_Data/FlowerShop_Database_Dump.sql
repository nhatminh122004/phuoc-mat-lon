-- =======================================================================
-- Database: FlowerShopDb
-- MySQL 5.7 / 8.0+ Compatibility Script
-- =======================================================================

CREATE DATABASE IF NOT EXISTS `FlowerShopDb` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `FlowerShopDb`;

SET FOREIGN_KEY_CHECKS = 0;

-- Table Categories
DROP TABLE IF EXISTS `Categories`;
CREATE TABLE `Categories` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `Description` VARCHAR(500) NULL,
  `ImageUrl` VARCHAR(300) NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table Occasions
DROP TABLE IF EXISTS `Occasions`;
CREATE TABLE `Occasions` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `IconClass` VARCHAR(200) NULL,
  `ImageUrl` VARCHAR(300) NULL,
  `Description` VARCHAR(500) NULL,
  `DisplayOrder` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table FlowerTypes
DROP TABLE IF EXISTS `FlowerTypes`;
CREATE TABLE `FlowerTypes` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Slug` VARCHAR(150) NOT NULL,
  `Meaning` VARCHAR(500) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table Products
DROP TABLE IF EXISTS `Products`;
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
  `Price` DECIMAL(18,2) NOT NULL,
  `SalePrice` DECIMAL(18,2) NULL,
  `StockQuantity` INT NOT NULL DEFAULT 100,
  `MainImageUrl` VARCHAR(350) NULL,
  `RatingAvg` DOUBLE NOT NULL DEFAULT 5.0,
  `ReviewCount` INT NOT NULL DEFAULT 0,
  `IsFeatured` TINYINT(1) NOT NULL DEFAULT 0,
  `IsBestSeller` TINYINT(1) NOT NULL DEFAULT 0,
  `IsFlashSale` TINYINT(1) NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `FK_Products_Categories` (`CategoryId`),
  KEY `FK_Products_Occasions` (`OccasionId`),
  KEY `FK_Products_FlowerTypes` (`FlowerTypeId`),
  CONSTRAINT `FK_Products_Categories` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Products_Occasions` FOREIGN KEY (`OccasionId`) REFERENCES `Occasions` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Products_FlowerTypes` FOREIGN KEY (`FlowerTypeId`) REFERENCES `FlowerTypes` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table AddonItems
DROP TABLE IF EXISTS `AddonItems`;
CREATE TABLE `AddonItems` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `Price` DECIMAL(18,2) NOT NULL,
  `ImageUrl` VARCHAR(350) NULL,
  `Type` VARCHAR(50) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table GreetingCards
DROP TABLE IF EXISTS `GreetingCards`;
CREATE TABLE `GreetingCards` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(150) NOT NULL,
  `ImageUrl` VARCHAR(350) NULL,
  `Price` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `Theme` VARCHAR(100) NULL,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table Vouchers
DROP TABLE IF EXISTS `Vouchers`;
CREATE TABLE `Vouchers` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Code` VARCHAR(50) NOT NULL,
  `Description` VARCHAR(250) NULL,
  `DiscountPercent` INT NULL,
  `DiscountAmount` DECIMAL(18,2) NULL,
  `MinOrderValue` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `MaxDiscountAmount` DECIMAL(18,2) NULL,
  `StartDate` DATETIME NULL,
  `EndDate` DATETIME NULL,
  `UsageLimit` INT NOT NULL DEFAULT 1000,
  `UsedCount` INT NOT NULL DEFAULT 0,
  `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table Orders
DROP TABLE IF EXISTS `Orders`;
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
  `OrderNotes` VARCHAR(500) NULL,
  `ProofPhotoUrl` VARCHAR(350) NULL,
  `SubTotal` DECIMAL(18,2) NOT NULL,
  `AddonTotal` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `ShippingFee` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `DiscountAmount` DECIMAL(18,2) NOT NULL DEFAULT 0,
  `TotalAmount` DECIMAL(18,2) NOT NULL,
  `VoucherCode` VARCHAR(50) NULL,
  `PaymentMethod` VARCHAR(50) NOT NULL DEFAULT 'COD',
  `PaymentStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
  `OrderStatus` VARCHAR(50) NOT NULL DEFAULT 'Pending',
  `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` DATETIME NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_OrderCode` (`OrderCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table OrderDetails
DROP TABLE IF EXISTS `OrderDetails`;
CREATE TABLE `OrderDetails` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `ProductId` INT NOT NULL,
  `ProductName` VARCHAR(200) NOT NULL,
  `ProductImageUrl` VARCHAR(350) NULL,
  `UnitPrice` DECIMAL(18,2) NOT NULL,
  `Quantity` INT NOT NULL DEFAULT 1,
  `TotalPrice` DECIMAL(18,2) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_OrderDetails_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table OrderAddons
DROP TABLE IF EXISTS `OrderAddons`;
CREATE TABLE `OrderAddons` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `AddonItemId` INT NOT NULL,
  `AddonName` VARCHAR(150) NOT NULL,
  `UnitPrice` DECIMAL(18,2) NOT NULL,
  `Quantity` INT NOT NULL DEFAULT 1,
  `TotalPrice` DECIMAL(18,2) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_OrderAddons_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table PaymentTransactions
DROP TABLE IF EXISTS `PaymentTransactions`;
CREATE TABLE `PaymentTransactions` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `OrderId` INT NOT NULL,
  `Gateway` VARCHAR(50) NOT NULL,
  `TransactionNo` VARCHAR(100) NULL,
  `GatewayResponseCode` VARCHAR(50) NULL,
  `Amount` DECIMAL(18,2) NOT NULL,
  `Currency` VARCHAR(10) NOT NULL DEFAULT 'VND',
  `PayDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `RawPayload` TEXT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `FK_Payments_Orders` FOREIGN KEY (`OrderId`) REFERENCES `Orders` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Table Users
DROP TABLE IF EXISTS `Users`;
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
  UNIQUE KEY `UK_Username` (`Username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
