using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShop.Models.Entities
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? OccasionId { get; set; }

        public int? FlowerTypeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Slug { get; set; }

        [StringLength(50)]
        public string SKU { get; set; }

        [StringLength(500)]
        public string ShortDescription { get; set; }

        [Column(TypeName = "text")]
        public string FullDescription { get; set; }

        [StringLength(1000)]
        public string FlowerCareGuide { get; set; }

        [StringLength(100)]
        public string ColorTone { get; set; } // Đỏ, Hồng, Trắng, Vàng, Xanh, Tím

        [Required]
        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        public int StockQuantity { get; set; } = 100;

        [StringLength(350)]
        public string MainImageUrl { get; set; }

        public double RatingAvg { get; set; } = 5.0;

        public int ReviewCount { get; set; } = 0;

        public bool IsFeatured { get; set; } = false;

        public bool IsBestSeller { get; set; } = false;

        public bool IsFlashSale { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        [ForeignKey("OccasionId")]
        public virtual Occasion Occasion { get; set; }

        [ForeignKey("FlowerTypeId")]
        public virtual FlowerType FlowerType { get; set; }

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }

    [Table("ProductImages")]
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(350)]
        public string ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }

    [Table("AddonItems")]
    public class AddonItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        public decimal Price { get; set; }

        [StringLength(350)]
        public string ImageUrl { get; set; }

        [StringLength(50)]
        public string Type { get; set; } // TeddyBear, Chocolate, ScentedCandle, Balloons, Vase

        public bool IsActive { get; set; } = true;
    }

    [Table("GreetingCards")]
    public class GreetingCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(350)]
        public string ImageUrl { get; set; }

        public decimal Price { get; set; } = 0; // Thường miễn phí tặng kèm

        [StringLength(100)]
        public string Theme { get; set; } // Birthday, Love, Anniversary, Gratitude

        public bool IsActive { get; set; } = true;
    }

    [Table("Vouchers")]
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        [StringLength(250)]
        public string Description { get; set; }

        public int? DiscountPercent { get; set; }

        public decimal? DiscountAmount { get; set; }

        public decimal MinOrderValue { get; set; } = 0;

        public decimal? MaxDiscountAmount { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int UsageLimit { get; set; } = 1000;

        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
