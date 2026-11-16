using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlowerShop.Models.Entities
{
    /// <summary>
    /// Token khôi phục mật khẩu dùng một lần. Chỉ lưu bản băm SHA-256 của token,
    /// không lưu token thô. Có index UNIQUE(TokenHash) + INDEX(UserId), INDEX(ExpiresAt).
    /// </summary>
    [Table("PasswordResetTokens")]
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(64)]
        public string TokenHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [StringLength(64)]
        public string RequestedIp { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public bool IsUsable(DateTime nowUtcFallback)
        {
            return UsedAt == null && nowUtcFallback <= ExpiresAt;
        }
    }
}
