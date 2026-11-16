using System;
using System.Configuration;
using System.Linq;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Email;

namespace FlowerShop.Services.Security
{
    /// <summary>
    /// Luồng quên mật khẩu 2 bước:
    ///   1. RequestReset: tạo token 1 lần, lưu bản băm, gửi email chứa link.
    ///   2. CompleteReset: kiểm tra token còn hạn & chưa dùng, đổi mật khẩu, đánh dấu đã dùng.
    /// Bản cũ cho phép đổi mật khẩu chỉ với email -> ai biết email là chiếm được tài khoản.
    /// </summary>
    public class PasswordResetService
    {
        public const int MaxRequestsPerHour = 5;

        /// <summary>
        /// Trả về outcome trung tính (không tiết lộ email có tồn tại hay không).
        /// </summary>
        public static PasswordResetOutcome RequestReset(
            DAL.FlowerShopDbContext db,
            IEmailService emailSender,
            string email,
            string ipAddress,
            Func<string, string> buildResetUrl,
            DateTime now)
        {
            const string genericMessage =
                "Nếu email này đã đăng ký, chúng tôi đã gửi link đặt lại mật khẩu. Vui lòng kiểm tra hộp thư (cả mục Spam).";

            var outcome = new PasswordResetOutcome
            {
                Success = true,
                Message = genericMessage,
                Email = email
            };

            if (db == null || string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                outcome.Message = "Vui lòng nhập địa chỉ email hợp lệ.";
                outcome.Success = false;
                return outcome;
            }

            string normalizedEmail = email.Trim().ToLowerInvariant();
            var user = db.Users.FirstOrDefault(u => u.IsActive && u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                // Không lộ thông tin: vẫn trả về thông báo chung.
                return outcome;
            }

            // Giới hạn tần suất: tối đa 5 yêu cầu/giờ/tài khoản.
            var hourAgo = now.AddHours(-1);
            int recent = db.PasswordResetTokens.Count(t => t.UserId == user.Id && t.CreatedAt >= hourAgo);
            if (recent >= MaxRequestsPerHour)
            {
                outcome.Message = "Bạn đã yêu cầu đặt lại mật khẩu quá nhiều lần. Vui lòng thử lại sau 1 giờ.";
                return outcome;
            }

            // Vô hiệu hoá các token cũ chưa dùng.
            var previousTokens = db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null)
                .ToList();

            foreach (var previous in previousTokens)
            {
                previous.UsedAt = now;
            }

            string rawToken = PasswordResetTokenFactory.CreateToken();

            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = PasswordResetTokenFactory.HashToken(rawToken),
                CreatedAt = now,
                ExpiresAt = PasswordResetTokenFactory.GetExpiry(now),
                RequestedIp = Truncate(ipAddress, 64)
            });

            db.SaveChanges();

            string resetUrl = buildResetUrl != null ? buildResetUrl(rawToken) : rawToken;

            string error;
            bool delivered = emailSender != null && emailSender.Send(
                user.Email,
                EmailTemplates.PasswordResetSubject(),
                EmailTemplates.PasswordResetBody(user.FullName, resetUrl, PasswordResetTokenFactory.GetExpiry(now)),
                out error);

            outcome.EmailDelivered = delivered;

            bool exposeDevLink = string.Equals(
                ConfigurationManager.AppSettings["Security:ExposeDevResetLink"], "true",
                StringComparison.OrdinalIgnoreCase);

            if (exposeDevLink)
            {
                outcome.DevResetUrl = resetUrl;
            }

            return outcome;
        }

        /// <summary>
        /// Đặt lại mật khẩu bằng token. Không tiết lộ lý do thất bại cụ thể cho người dùng cuối.
        /// </summary>
        public static PasswordResetOutcome CompleteReset(DAL.FlowerShopDbContext db, string rawToken, string newPassword, DateTime now)
        {
            var outcome = new PasswordResetOutcome { Success = false };

            if (db == null || string.IsNullOrWhiteSpace(rawToken) || !PasswordResetTokenFactory.IsWellFormed(rawToken))
            {
                outcome.Message = "Link đặt lại mật khẩu không hợp lệ.";
                return outcome;
            }

            var validation = PasswordService.Validate(newPassword, null);
            if (!validation.IsValid)
            {
                outcome.Message = validation.Message;
                return outcome;
            }

            string hash = PasswordResetTokenFactory.HashToken(rawToken);

            var token = db.PasswordResetTokens
                .FirstOrDefault(t => t.TokenHash == hash);

            if (token == null)
            {
                outcome.Message = "Link đặt lại mật khẩu không tồn tại hoặc đã được sử dụng.";
                return outcome;
            }

            if (token.UsedAt != null)
            {
                outcome.Message = "Link này đã được sử dụng. Vui lòng yêu cầu link mới.";
                return outcome;
            }

            if (PasswordResetTokenFactory.IsExpired(token.ExpiresAt, now))
            {
                outcome.Message = "Link đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu link mới.";
                return outcome;
            }

            var user = db.Users.FirstOrDefault(u => u.Id == token.UserId);
            if (user == null || !user.IsActive)
            {
                outcome.Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hoá.";
                return outcome;
            }

            user.PasswordHash = PasswordService.Hash(newPassword);
            token.UsedAt = now;

            // Vô hiệu hoá mọi token khác của user.
            var otherTokens = db.PasswordResetTokens
                .Where(t => t.UserId == user.Id && t.Id != token.Id && t.UsedAt == null)
                .ToList();

            foreach (var other in otherTokens)
            {
                other.UsedAt = now;
            }

            db.SaveChanges();

            outcome.Success = true;
            outcome.Email = user.Email;
            outcome.Message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
            return outcome;
        }

        /// <summary>Kiểm tra token có dùng được hay không (dùng cho màn hình GET).</summary>
        public static bool IsTokenUsable(DAL.FlowerShopDbContext db, string rawToken, DateTime now)
        {
            if (db == null || string.IsNullOrWhiteSpace(rawToken) || !PasswordResetTokenFactory.IsWellFormed(rawToken))
            {
                return false;
            }

            string hash = PasswordResetTokenFactory.HashToken(rawToken);
            var token = db.PasswordResetTokens.FirstOrDefault(t => t.TokenHash == hash);

            return token != null && token.UsedAt == null && !PasswordResetTokenFactory.IsExpired(token.ExpiresAt, now);
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
