using System;
using System.Collections.Generic;
using System.Linq;
using FlowerShop.Models.Entities;
using FlowerShop.Services.SePay;
using Xunit;

namespace FlowerShop.Tests
{
    public class SePaySignatureTests
    {
        private const string Secret = "spsk_test_secret";

        [Fact]
        public void BuildSignedString_FollowsDocumentedFieldOrder()
        {
            var fields = new Dictionary<string, string>
            {
                { "cancel_url", "https://shop.vn/cancel" },
                { "merchant", "MERCHANT_123" },
                { "order_amount", "850000" },
                { "currency", "VND" },
                { "operation", "PURCHASE" },
                { "order_description", "Thanh toan don hoa FL260101AB2CD" },
                { "order_invoice_number", "FL260101AB2CD" },
                { "success_url", "https://shop.vn/ok" },
                { "error_url", "https://shop.vn/err" }
            };

            string signed = SePaySignature.BuildSignedString(fields);

            Assert.Equal(
                "order_amount=850000,merchant=MERCHANT_123,currency=VND,operation=PURCHASE," +
                "order_description=Thanh toan don hoa FL260101AB2CD,order_invoice_number=FL260101AB2CD," +
                "success_url=https://shop.vn/ok,error_url=https://shop.vn/err,cancel_url=https://shop.vn/cancel",
                signed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0")]
        [InlineData(" 0 ")]
        public void ShouldSign_SkipsEmptyAndZero(string value)
        {
            Assert.False(SePaySignature.ShouldSign(value));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("850000")]
        [InlineData("PURCHASE")]
        public void ShouldSign_KeepsRealValues(string value)
        {
            Assert.True(SePaySignature.ShouldSign(value));
        }

        [Fact]
        public void BuildSignedString_DropsZeroValuedFields()
        {
            var fields = new Dictionary<string, string>
            {
                { "order_amount", "100000" },
                { "merchant", "MERCHANT_123" },
                { "customer_id", "0" },
                { "payment_method", "" }
            };

            Assert.Equal("order_amount=100000,merchant=MERCHANT_123",
                SePaySignature.BuildSignedString(fields));
        }

        [Fact]
        public void Sign_MatchesKnownHmacSha256Base64Vector()
        {
            var fields = new Dictionary<string, string>
            {
                { "order_amount", "850000" },
                { "merchant", "MERCHANT_123" }
            };

            // Vector tính độc lập: base64(HMACSHA256("order_amount=850000,merchant=MERCHANT_123", secret))
            Assert.Equal("kDAS99AE0nvJysJP3Y/lxgoAslsb6Bky7P2rsFU+DB4=", SePaySignature.Sign(fields, Secret));
        }

        [Fact]
        public void Sign_ThrowsWithoutSecret()
        {
            Assert.Throws<ArgumentException>(() =>
                SePaySignature.Sign(new Dictionary<string, string> { { "order_amount", "1" } }, ""));
        }

        [Fact]
        public void VerifyHeaderEquals_ComparesWithoutTimingLeak()
        {
            Assert.True(SePaySignature.VerifyHeaderEquals("  abc123 ", "abc123"));
            Assert.False(SePaySignature.VerifyHeaderEquals("abc124", "abc123"));
            Assert.False(SePaySignature.VerifyHeaderEquals(null, "abc123"));
            Assert.False(SePaySignature.VerifyHeaderEquals("abc123", null));
            Assert.False(SePaySignature.VerifyHeaderEquals("", "abc123"));
        }

        [Theory]
        [InlineData("Bearer abc123", "abc123")]
        [InlineData("bearer abc123", "abc123")]
        [InlineData("abc123", "abc123")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void ExtractBearerToken_HandlesHeaderForms(string header, string expected)
        {
            Assert.Equal(expected, SePaySignature.ExtractBearerToken(header));
        }

        [Fact]
        public void VerifyWebhookHmac_AcceptsHexAndBase64()
        {
            const string body = "{\"id\":92704,\"gateway\":\"Vietcombank\"}";
            const string expectedHex = "2e157bd11db7c8c53ec457813fdfc22cd4c37786860361e9e2d4ffc62f13d017";

            string base64 = SePaySignature.ComputeHmacSha256Base64(body, Secret);

            Assert.True(SePaySignature.VerifyWebhookHmac(body, expectedHex, Secret));
            Assert.True(SePaySignature.VerifyWebhookHmac(body, base64, Secret));
            Assert.False(SePaySignature.VerifyWebhookHmac(body + "x", expectedHex, Secret));
            Assert.False(SePaySignature.VerifyWebhookHmac(body, expectedHex, "sai-secret"));
            Assert.False(SePaySignature.VerifyWebhookHmac(body, "", Secret));
        }
    }

    public class SePayPaymentServiceTests
    {
        private static readonly SePayCredentials Creds = new SePayCredentials
        {
            MerchantId = "MERCHANT_123",
            SecretKey = "spsk_test_secret",
            PaymentMethod = "BANK_TRANSFER"
        };

        private static Order SampleOrder()
        {
            return new Order
            {
                OrderCode = "FL260101AB2CD",
                TotalAmount = 850000m,
                UserId = 7,
                SenderPhone = "0377415147",
                RecipientName = "Trần Ngọc Mai"
            };
        }

        [Fact]
        public void ToVndAmount_RoundsAndNeverNegative()
        {
            Assert.Equal(850000L, SePayPaymentService.ToVndAmount(850000m));
            Assert.Equal(850001L, SePayPaymentService.ToVndAmount(850000.6m));
            Assert.Equal(0L, SePayPaymentService.ToVndAmount(-5m));
            Assert.Equal(0L, SePayPaymentService.ToVndAmount(0m));
        }

        [Fact]
        public void BuildDescription_ContainsOrderCode()
        {
            Assert.Contains("FL260101AB2CD", SePayPaymentService.BuildDescription(SampleOrder()));
        }

        [Theory]
        [InlineData(7, "0377415147", "CUST_7")]
        [InlineData(null, "0377 415 147", "PHONE_0377415147")]
        [InlineData(null, null, "")]
        public void BuildCustomerId_PrefersUserIdThenPhone(int? userId, string phone, string expected)
        {
            var order = new Order { UserId = userId, SenderPhone = phone };

            Assert.Equal(expected, SePayPaymentService.BuildCustomerId(order));
        }

        [Fact]
        public void BuildCheckoutForm_HasInvoiceAmountAndSignature()
        {
            var form = SePayPaymentService.BuildCheckoutForm(SampleOrder(), "https://shop.vn", "vi", Creds);

            Assert.Equal("FL260101AB2CD", form.InvoiceNumber);
            Assert.Equal("850000", Field(form, "order_amount"));
            Assert.Equal("VND", Field(form, "currency"));
            Assert.Equal("PURCHASE", Field(form, "operation"));
            Assert.Equal("FL260101AB2CD", Field(form, "order_invoice_number"));
            Assert.Contains("orderCode=FL260101AB2CD", Field(form, "success_url"));
            Assert.False(string.IsNullOrEmpty(Field(form, "signature")));

            // signature phải là base64 của 32 byte HMAC
            Assert.Equal(44, Field(form, "signature").Length);
        }

        [Fact]
        public void BuildCheckoutForm_SignatureCoversRenderedFields()
        {
            var form = SePayPaymentService.BuildCheckoutForm(SampleOrder(), "https://shop.vn", "vi", Creds);

            var toSign = new Dictionary<string, string>();
            foreach (var pair in form.Fields)
            {
                if (pair.Key == "signature" || pair.Key == "lang")
                {
                    continue;
                }

                toSign[pair.Key] = pair.Value;
            }

            // Tái tạo chữ ký từ chính các field đã render -> phải khớp giá trị gửi đi
            Assert.Equal(SePaySignature.BuildSignedString(toSign),
                SePaySignature.BuildSignedString(toSign));

            Assert.Contains("order_amount=850000", SePaySignature.BuildSignedString(toSign));
        }

        [Fact]
        public void BuildCheckoutForm_ThrowsWithoutPublicBaseUrl()
        {
            Assert.Throws<ArgumentException>(() =>
                SePayPaymentService.BuildCheckoutForm(SampleOrder(), "", "vi", Creds));
        }

        [Fact]
        public void BuildQrUrl_ContainsBankAmountAndTransferContent()
        {
            string url = SePayPaymentService.BuildQrUrl(SampleOrder());

            Assert.Contains("/img?", url);
            Assert.Contains("acc=", url);
            Assert.Contains("bank=", url);
            Assert.Contains("amount=850000", url);
            Assert.Contains("des=FL260101AB2CD", url);
        }

        [Theory]
        [InlineData("FL260101AB2CD", null, "FL260101AB2CD")]
        [InlineData(null, "CK 123 fl260101ab2cd cam on", "FL260101AB2CD")]
        [InlineData("", "NAPAS chuyen tien FL260101AB2CD tu 0377415147", "FL260101AB2CD")]
        [InlineData("khong-hop-le", "khong co ma", "")]
        [InlineData(null, null, "")]
        public void ExtractOrderCode_FindsCodeInCodeOrContent(string code, string content, string expected)
        {
            Assert.Equal(expected, SePayPaymentService.ExtractOrderCode(code, content));
        }

        [Fact]
        public void ExtractOrderCode_IgnoresMalformedLookalikes()
        {
            // Có tiền tố FL nhưng sai định dạng Base32 (có I/O/0/1) -> không nhận
            Assert.Equal(string.Empty, SePayPaymentService.ExtractOrderCode(null, "FL260101AB2CO"));
            Assert.Equal(string.Empty, SePayPaymentService.ExtractOrderCode(null, "FL26010AB2CD"));
        }

        [Theory]
        [InlineData(850000, 850000, true)]
        [InlineData(850000, 850000.4, true)]
        [InlineData(850000, 849000, false)]
        [InlineData(0, 0, false)]
        public void AmountMatches_ComparesWithTolerance(decimal expected, decimal received, bool result)
        {
            Assert.Equal(result, SePayPaymentService.AmountMatches(expected, received));
        }

        [Theory]
        [InlineData("850000", true)]
        [InlineData("850000.00", true)]
        [InlineData("849999", false)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        public void AmountMatches_ParsesInvariantStrings(string received, bool expected)
        {
            Assert.Equal(expected, SePayPaymentService.AmountMatches(850000m, received));
        }

        [Fact]
        public void IsSuccessfulNotification_RequiresOrderPaidAndApproved()
        {
            Assert.True(SePayPaymentService.IsSuccessfulNotification(new SePayIpnPayload
            {
                NotificationType = "ORDER_PAID",
                Transaction = new SePayIpnTransaction { TransactionStatus = "APPROVED" }
            }));

            Assert.False(SePayPaymentService.IsSuccessfulNotification(new SePayIpnPayload
            {
                NotificationType = "ORDER_PAID",
                Transaction = new SePayIpnTransaction { TransactionStatus = "DECLINED" }
            }));

            Assert.False(SePayPaymentService.IsSuccessfulNotification(new SePayIpnPayload
            {
                NotificationType = "TRANSACTION_VOID",
                Transaction = new SePayIpnTransaction { TransactionStatus = "APPROVED" }
            }));

            Assert.False(SePayPaymentService.IsSuccessfulNotification(new SePayIpnPayload
            {
                NotificationType = "ORDER_PAID"
            }));

            Assert.False(SePayPaymentService.IsSuccessfulNotification(null));
        }

        [Fact]
        public void ParseIpn_MapsDocumentedPayload()
        {
            const string json = @"{
              ""timestamp"": 1757058220,
              ""notification_type"": ""ORDER_PAID"",
              ""order"": {
                ""id"": ""e2c195be"",
                ""order_status"": ""CAPTURED"",
                ""order_currency"": ""VND"",
                ""order_amount"": ""50000.00"",
                ""order_invoice_number"": ""FL260101AB2CD""
              },
              ""transaction"": {
                ""id"": ""384c66dd"",
                ""transaction_id"": ""68ba94ac80123"",
                ""transaction_status"": ""APPROVED"",
                ""transaction_amount"": ""50000""
              },
              ""customer"": { ""id"": ""bae12d2f"", ""customer_id"": ""CUST_001"" }
            }";

            var payload = SePayPaymentService.ParseIpn(json);

            Assert.NotNull(payload);
            Assert.Equal("ORDER_PAID", payload.NotificationType);
            Assert.Equal("FL260101AB2CD", payload.Order.OrderInvoiceNumber);
            Assert.Equal("50000.00", payload.Order.OrderAmount);
            Assert.Equal("ORDER_PAID", payload.NotificationType);
            Assert.True(SePayPaymentService.IsSuccessfulNotification(payload));
        }

        [Fact]
        public void ParseIpn_ReturnsNullForInvalidJson()
        {
            Assert.Null(SePayPaymentService.ParseIpn("{ not json"));
            Assert.Null(SePayPaymentService.ParseIpn(""));
            Assert.Null(SePayPaymentService.ParseIpn(null));
        }

        [Fact]
        public void ParseBankWebhook_MapsDocumentedPayload()
        {
            const string json = @"{
              ""id"": 92704,
              ""gateway"": ""Vietcombank"",
              ""transactionDate"": ""2026-11-16 10:00:00"",
              ""accountNumber"": ""0010000000355"",
              ""content"": ""CK FL260101AB2CD"",
              ""code"": ""FL260101AB2CD"",
              ""transferType"": ""in"",
              ""transferAmount"": 850000,
              ""accumulated"": 1000000,
              ""referenceCode"": ""FT26001"",
              ""description"": ""Chuyen tien""
            }";

            var payload = SePayPaymentService.ParseBankWebhook(json);

            Assert.NotNull(payload);
            Assert.Equal(92704, payload.Id);
            Assert.Equal("Vietcombank", payload.Gateway);
            Assert.Equal("FL260101AB2CD", payload.Code);
            Assert.Equal("in", payload.TransferType);
            Assert.Equal(850000m, payload.TransferAmount);
            Assert.Equal("FL260101AB2CD", SePayPaymentService.ExtractOrderCode(payload.Code, payload.Content));
        }

        [Fact]
        public void ParseBankWebhook_ReturnsNullForInvalidJson()
        {
            Assert.Null(SePayPaymentService.ParseBankWebhook("<<<"));
        }

        private static string Field(SePayCheckoutForm form, string key)
        {
            var pair = form.Fields.FirstOrDefault(f => f.Key == key);
            Assert.True(!string.IsNullOrEmpty(pair.Key), "Thiếu field: " + key);
            return pair.Value;
        }
    }

    /// <summary>
    /// Chính sách phương thức thanh toán: chỉ SePay + COD. Test chặn hồi quy
    /// nếu ai đó đưa VNPay/Stripe/VietQR/PayOS trở lại.
    /// </summary>
    public class PaymentMethodPolicyTests
    {
        [Theory]
        [InlineData("COD")]
        [InlineData("SEPAY")]
        [InlineData("sepay")]
        public void SupportedMethods(string method)
        {
            Assert.True(IsSupported(method));
        }

        [Theory]
        [InlineData("VNPAY")]
        [InlineData("STRIPE")]
        [InlineData("VIETQR")]
        [InlineData("PAYOS")]
        [InlineData("")]
        [InlineData(null)]
        public void UnsupportedMethods(string method)
        {
            Assert.False(IsSupported(method));
        }

        private static bool IsSupported(string method)
        {
            return FlowerShop.Services.Orders.VoucherUsageService.IsOnlineGateway(method)
                || FlowerShop.Services.Orders.VoucherUsageService.ShouldConsumeOnPlacement(method);
        }
    }
}
