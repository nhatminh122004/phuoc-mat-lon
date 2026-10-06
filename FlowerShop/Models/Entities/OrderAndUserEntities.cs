using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShop.Models.Entities
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } // Ví dụ: FL202609088831

        public int? UserId { get; set; }

        // Người gửi (Sender)
        [Required]
        [StringLength(100)]
        public string SenderName { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderPhone { get; set; }

        [StringLength(100)]
        public string SenderEmail { get; set; }

        // Người nhận (Recipient)
        [Required]
        [StringLength(100)]
        public string RecipientName { get; set; }

        [Required]
        [StringLength(20)]
        public string RecipientPhone { get; set; }

        [Required]
        [StringLength(250)]
        public string RecipientAddress { get; set; }

        [StringLength(100)]
        public string District { get; set; }

        [StringLength(100)]
        public string City { get; set; } = "Hồ Chí Minh";

        // Giao hoa
        public DateTime DeliveryDate { get; set; } = DateTime.Today;

        [StringLength(50)]
        public string DeliveryTimeSlot { get; set; } // 8h-10h, 10h-12h, 14h-16h, 18h-20h, Hoả tốc 2h

        public bool IsAnonymous { get; set; } = false; // Tặng bí mật

        // Thiệp & Thông điệp
        public int? GreetingCardId { get; set; }

        [StringLength(1000)]
        public string CardMessage { get; set; }

        [StringLength(500)]
        public string OrderNotes { get; set; }

        // Bằng chứng cắm hoa thực tế gửi khách duyệt
        [StringLength(350)]
        public string ProofPhotoUrl { get; set; }

        // Tài chính
        public decimal SubTotal { get; set; }

        public decimal AddonTotal { get; set; } = 0;

        public decimal ShippingFee { get; set; } = 0;

        public decimal DiscountAmount { get; set; } = 0;

        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string VoucherCode { get; set; }

        // Phương thức & Trạng thái thanh toán
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD"; // COD, VNPAY, STRIPE, VIETQR

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Failed, Refunded

        // Trạng thái vận hành đơn hoa
        [Required]
        [StringLength(50)]
        public string OrderStatus { get; set; } = "Pending"; 
        // Pending -> Confirmed -> Arranging (Đang cắm hoa) -> Shipping -> Delivered -> Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<OrderAddon> OrderAddons { get; set; } = new List<OrderAddon>();
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }

    [Table("OrderDetails")]
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [StringLength(350)]
        public string ProductImageUrl { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal TotalPrice { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }

    [Table("OrderAddons")]
    public class OrderAddon
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int AddonItemId { get; set; }

        [Required]
        [StringLength(150)]
        public string AddonName { get; set; }

        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal TotalPrice { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [ForeignKey("AddonItemId")]
        public virtual AddonItem AddonItem { get; set; }
    }

    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string Gateway { get; set; } // VNPAY, STRIPE, VIETQR

        [StringLength(100)]
        public string TransactionNo { get; set; }

        [StringLength(50)]
        public string GatewayResponseCode { get; set; }

        public decimal Amount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "VND";

        public DateTime PayDate { get; set; } = DateTime.Now;

        [Column(TypeName = "text")]
        public string RawPayload { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }

    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }

        public int Rating { get; set; } = 5;

        [StringLength(1000)]
        public string Comment { get; set; }

        [StringLength(350)]
        public string ImageUrl { get; set; }

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }

    [Table("Users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [StringLength(150)]
        public string FullName { get; set; }

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "Customer"; // Admin, Florist, Customer

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
