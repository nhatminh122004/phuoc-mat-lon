using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlowerShop.Helpers;
using FlowerShop.Services.AI;
using FlowerShop.Services.Email;
using FlowerShop.Services.Logging;
using FlowerShop.Services.Orders;
using FlowerShop.Services.Upload;
using Xunit;

namespace FlowerShop.Tests
{
    public class FileUploadServiceTests
    {
        [Fact]
        public void SplitImageUrls_SplitsOnAllSeparatorsAndTrims()
        {
            var urls = FileUploadService.SplitImageUrls(" a.jpg ; b.jpg,  c.jpg\r\nd.jpg ");

            Assert.Equal(new[] { "a.jpg", "b.jpg", "c.jpg", "d.jpg" }, urls.ToArray());
        }

        [Fact]
        public void SplitImageUrls_RemovesDuplicatesIgnoringCase()
        {
            var urls = FileUploadService.SplitImageUrls("A.jpg;a.jpg;B.jpg");

            Assert.Equal(2, urls.Count);
            Assert.Equal("A.jpg", urls[0]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void SplitImageUrls_ReturnsEmptyForBlank(string input)
        {
            Assert.Empty(FileUploadService.SplitImageUrls(input));
        }

        [Fact]
        public void JoinImageUrls_JoinsWithSemicolonAndDropsBlanks()
        {
            string joined = FileUploadService.JoinImageUrls(new[] { "a.jpg", "", " ", null, "b.jpg", "a.jpg" });

            Assert.Equal("a.jpg;b.jpg", joined);
        }

        [Fact]
        public void JoinImageUrls_ReturnsNullWhenNothingUsable()
        {
            Assert.Null(FileUploadService.JoinImageUrls(new[] { "", "   " }));
            Assert.Null(FileUploadService.JoinImageUrls(null));
        }
    }

    public class FileEmailServiceTests : IDisposable
    {
        private readonly string _directory;

        public FileEmailServiceTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "flowershop-email-tests", Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public void Send_WritesEmlFile()
        {
            var service = new FileEmailService(_directory);

            string error;
            bool ok = service.Send("khach@example.com", "Đơn hoa của bạn", "<p>Xin chào</p>", out error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(1, service.GetSentCount());

            string file = Directory.GetFiles(_directory, "*.eml").Single();
            string content = File.ReadAllText(file);

            Assert.Contains("khach@example.com", content);
            Assert.Contains("<p>Xin chào</p>", content);
        }

        [Fact]
        public void Send_RejectsEmptyRecipient()
        {
            var service = new FileEmailService(_directory);

            string error;
            bool ok = service.Send("", "subject", "body", out error);

            Assert.False(ok);
            Assert.NotNull(error);
        }

        [Fact]
        public void GetSentCount_IsZeroBeforeSending()
        {
            var service = new FileEmailService(_directory);

            Assert.Equal(0, service.GetSentCount());
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_directory))
                {
                    Directory.Delete(_directory, true);
                }
            }
            catch
            {
                // bỏ qua lỗi dọn temp
            }
        }
    }

    public class EmailTemplatesTests
    {
        [Fact]
        public void PasswordResetBody_ContainsLinkAndExpiry()
        {
            string url = "https://shop.vn/Account/ResetPassword?token=abc123";
            var expires = new DateTime(2026, 11, 16, 10, 30, 0);

            string body = EmailTemplates.PasswordResetBody("Nguyễn Văn A", url, expires);

            Assert.Contains(url, body);
            Assert.Contains("Nguyễn Văn A", body);
            Assert.Contains("10:30 16/11/2026", body);
            Assert.Contains("một lần", body);
        }

        [Fact]
        public void PasswordResetBody_HtmlEncodesCustomerName()
        {
            string body = EmailTemplates.PasswordResetBody("<script>alert(1)</script>", "https://x", DateTime.Now);

            Assert.DoesNotContain("<script>", body);
            Assert.Contains("&lt;script&gt;", body);
        }

        [Fact]
        public void OrderConfirmationBody_ContainsOrderFacts()
        {
            var body = EmailTemplates.OrderConfirmationBody(
                "Trần Thị B", "FL260101AB2CD", 850000m, "Lê C", new DateTime(2026, 11, 20), "08:00 - 10:00",
                "https://shop.vn/tra-cuu-don-hang?orderCode=FL260101AB2CD");

            Assert.Contains("FL260101AB2CD", body);
            Assert.Contains("850", body);
            Assert.Contains("20/11/2026", body);
            Assert.Contains("08:00 - 10:00", body);
        }

        [Fact]
        public void StatusChangedTemplates_IncludeStatusName()
        {
            string subject = EmailTemplates.OrderStatusChangedSubject("FL260101AB2CD", "Shipper đang giao hàng");
            string body = EmailTemplates.OrderStatusChangedBody("A", "FL260101AB2CD", "Shipper đang giao hàng", null, "https://x");

            Assert.Contains("FL260101AB2CD", subject);
            Assert.Contains("Shipper đang giao hàng", body);
        }
    }

    public class OrderNotificationServiceTests
    {
        [Theory]
        [InlineData("Pending", "Chờ xác nhận")]
        [InlineData("Confirmed", "Đã xác nhận")]
        [InlineData("Arranging", "Nghệ nhân đang cắm hoa")]
        [InlineData("ProofReady", "Đã cắm xong - Chờ khách duyệt ảnh")]
        [InlineData("Shipping", "Shipper đang giao hàng")]
        [InlineData("Delivered", "Đã giao hoa thành công")]
        [InlineData("Cancelled", "Đã huỷ đơn")]
        [InlineData("SomethingElse", "SomethingElse")]
        public void GetStatusDisplayName_TranslatesAllStatuses(string status, string expected)
        {
            Assert.Equal(expected, OrderNotificationService.GetStatusDisplayName(status));
        }

        [Fact]
        public void SendOrderConfirmation_WithoutEmail_ReturnsFalse()
        {
            var order = new FlowerShop.Models.Entities.Order { OrderCode = "FL260101AB2CD" };

            Assert.False(OrderNotificationService.SendOrderConfirmation(new FileEmailService(Path.GetTempPath()), order, "https://x"));
        }
    }

    public class AppLoggerTests : IDisposable
    {
        private readonly string _directory;

        public AppLoggerTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "flowershop-log-tests", Guid.NewGuid().ToString("N"));
            AppLogger.Configure(_directory);
        }

        [Fact]
        public void InfoAndError_AreWrittenAndReadable()
        {
            AppLogger.Info("Đơn FL260101AB2CD đã tạo");
            AppLogger.Error("Gửi mail thất bại", new InvalidOperationException("SMTP timeout"));

            var lines = AppLogger.ReadRecent(null, 50);

            Assert.Contains(lines, l => l.Contains("[INFO]") && l.Contains("FL260101AB2CD"));
            Assert.Contains(lines, l => l.Contains("[ERROR]") && l.Contains("SMTP timeout"));
        }

        public void Dispose()
        {
            AppLogger.Configure(null);

            try
            {
                if (Directory.Exists(_directory))
                {
                    Directory.Delete(_directory, true);
                }
            }
            catch
            {
            }
        }
    }

    public class ChatbotIntentDetectorTests
    {
        [Theory]
        [InlineData("Tra cứu đơn hàng giúp em", ChatIntent.TrackOrder)]
        [InlineData("Đơn FL260101AB2CD tới đâu rồi", ChatIntent.TrackOrder)]
        [InlineData("Viết cho mình lời chúc tặng mẹ", ChatIntent.WriteCard)]
        [InlineData("Hoa sinh nhật bạn gái", ChatIntent.RecommendBirthday)]
        [InlineData("Tặng người yêu mẫu nào đẹp", ChatIntent.RecommendLove)]
        [InlineData("Hoa tặng mẹ ý nghĩa", ChatIntent.RecommendMother)]
        [InlineData("Hoa khai trương cho đối tác", ChatIntent.RecommendGrandOpening)]
        [InlineData("Mẫu rẻ dưới 500k", ChatIntent.RecommendBudget)]
        [InlineData("Chào shop", ChatIntent.Unknown)]
        [InlineData("", ChatIntent.Unknown)]
        public void Detect_MapsMessageToIntent(string message, ChatIntent expected)
        {
            Assert.Equal(expected, ChatbotIntentDetector.Detect(message));
        }

        [Theory]
        [InlineData("Đơn FL260101AB2CD", "FL260101AB2CD")]
        [InlineData("mã đơn fl260101ab2cd nhé", "FL260101AB2CD")]
        [InlineData("không có mã", "")]
        public void ExtractOrderCode_FindsCode(string message, string expected)
        {
            Assert.Equal(expected, ChatbotIntentDetector.ExtractOrderCode(message));
        }

        [Theory]
        [InlineData("SĐT 0377415147", "0377415147")]
        [InlineData("số 0377 415 147", "0377415147")]
        [InlineData("+84909123456", "0909123456")]
        [InlineData("không có số", "")]
        public void ExtractPhone_FindsVietnamesePhone(string message, string expected)
        {
            Assert.Equal(expected, ChatbotIntentDetector.ExtractPhone(message));
        }

        [Theory]
        [InlineData("0377415147", "0377415147")]
        [InlineData("0377 415 147", "0377415147")]
        [InlineData("+84 909 123 456", "0909123456")]
        [InlineData("84909123456", "0909123456")]
        [InlineData("909123456", "0909123456")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void NormalizePhone_ProducesLocalTenDigits(string input, string expected)
        {
            Assert.Equal(expected, ChatbotIntentDetector.NormalizePhone(input));
        }
    }
}
