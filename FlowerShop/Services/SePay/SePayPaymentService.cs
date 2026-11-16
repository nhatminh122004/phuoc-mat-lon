using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FlowerShop.Models.Entities;
using FlowerShop.Services.Orders;
using Newtonsoft.Json;

namespace FlowerShop.Services.SePay
{
    /// <summary>
    /// Dựng dữ liệu thanh toán SePay từ đơn hàng.
    /// Hai chế độ:
    ///  - gateway: ký form và POST sang pay.sepay.vn/v1/checkout/init (khách chọn QR/thẻ/NAPAS).
    ///  - qr: hiện ảnh QR chuyển khoản của qr.sepay.vn, xác nhận bằng webhook biến động số dư.
    /// Logic thuần (không HTTP, không DB) nên unit test được.
    /// </summary>
    public static class SePayPaymentService
    {
        private const string OperationPurchase = "PURCHASE";
        private const string CurrencyVnd = "VND";

        /// <summary>
        /// Form checkout: field đúng thứ tự tài liệu, có signature HMAC-SHA256 base64.
        /// </summary>
        public static SePayCheckoutForm BuildCheckoutForm(Order order, string publicBaseUrl, string language = "vi", SePayCredentials credentials = null)
        {
            if (order == null)
            {
                throw new ArgumentNullException("order");
            }

            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                throw new ArgumentException("Thiếu PublicBaseUrl cho callback SePay.", "publicBaseUrl");
            }

            string baseUrl = publicBaseUrl.TrimEnd('/');
            var creds = credentials ?? SePayCredentials.FromConfiguration();
            long amount = ToVndAmount(order.TotalAmount);

            var signedValues = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "order_amount", amount.ToString(CultureInfo.InvariantCulture) },
                { "merchant", creds.MerchantId },
                { "currency", CurrencyVnd },
                { "operation", OperationPurchase },
                { "order_description", BuildDescription(order) },
                { "order_invoice_number", order.OrderCode },
                { "customer_id", BuildCustomerId(order) },
                { "payment_method", creds.PaymentMethod },
                { "success_url", baseUrl + "/Payment/Return?orderCode=" + Uri.EscapeDataString(order.OrderCode) },
                { "error_url", baseUrl + "/Payment/Error?orderCode=" + Uri.EscapeDataString(order.OrderCode) },
                { "cancel_url", baseUrl + "/Payment/Cancel?orderCode=" + Uri.EscapeDataString(order.OrderCode) }
            };

            string signature = SePaySignature.Sign(signedValues, creds.SecretKey);

            // Thứ tự input trên form phải khớp mẫu của SePay.
            var form = new SePayCheckoutForm
            {
                ActionUrl = SePayOptions.CheckoutInitUrl,
                InvoiceNumber = order.OrderCode,
                Signature = signature
            };

            form.Fields.Add(new KeyValuePair<string, string>("order_amount", signedValues["order_amount"]));
            form.Fields.Add(new KeyValuePair<string, string>("merchant", signedValues["merchant"]));
            form.Fields.Add(new KeyValuePair<string, string>("currency", signedValues["currency"]));
            form.Fields.Add(new KeyValuePair<string, string>("operation", signedValues["operation"]));
            form.Fields.Add(new KeyValuePair<string, string>("order_description", signedValues["order_description"]));
            form.Fields.Add(new KeyValuePair<string, string>("order_invoice_number", signedValues["order_invoice_number"]));

            if (SePaySignature.ShouldSign(signedValues["customer_id"]))
            {
                form.Fields.Add(new KeyValuePair<string, string>("customer_id", signedValues["customer_id"]));
            }

            if (SePaySignature.ShouldSign(signedValues["payment_method"]))
            {
                form.Fields.Add(new KeyValuePair<string, string>("payment_method", signedValues["payment_method"]));
            }

            form.Fields.Add(new KeyValuePair<string, string>("success_url", signedValues["success_url"]));
            form.Fields.Add(new KeyValuePair<string, string>("error_url", signedValues["error_url"]));
            form.Fields.Add(new KeyValuePair<string, string>("cancel_url", signedValues["cancel_url"]));
            form.Fields.Add(new KeyValuePair<string, string>("lang", string.IsNullOrWhiteSpace(language) ? "vi" : language));
            form.Fields.Add(new KeyValuePair<string, string>("signature", signature));

            return form;
        }

        /// <summary>
        /// Ảnh QR động: https://qr.sepay.vn/img?acc=...&amp;bank=...&amp;amount=...&amp;des=...
        /// Nội dung chuyển khoản = mã đơn để SePay/webhook đối soát tự động.
        /// </summary>
        public static string BuildQrUrl(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException("order");
            }

            long amount = ToVndAmount(order.TotalAmount);

            return string.Format(
                "{0}/img?acc={1}&bank={2}&amount={3}&des={4}",
                SePayOptions.QrBaseUrl,
                Uri.EscapeDataString(SePayOptions.AccountNumber),
                Uri.EscapeDataString(SePayOptions.Bank),
                amount.ToString(CultureInfo.InvariantCulture),
                Uri.EscapeDataString(order.OrderCode));
        }

        /// <summary>Chuẩn hoá số tiền VND (làm tròn về đồng, tối thiểu 1).</summary>
        public static long ToVndAmount(decimal amount)
        {
            if (amount <= 0m)
            {
                return 0;
            }

            return (long)Math.Round(amount, MidpointRounding.AwayFromZero);
        }

        public static string BuildDescription(Order order)
        {
            string code = order != null && !string.IsNullOrEmpty(order.OrderCode) ? order.OrderCode : "DON HANG";
            return "Thanh toan don hoa " + code;
        }

        public static string BuildCustomerId(Order order)
        {
            if (order == null)
            {
                return string.Empty;
            }

            if (order.UserId.HasValue && order.UserId.Value > 0)
            {
                return "CUST_" + order.UserId.Value.ToString(CultureInfo.InvariantCulture);
            }

            string phone = (order.SenderPhone ?? string.Empty).Replace(" ", string.Empty);
            return string.IsNullOrEmpty(phone) ? string.Empty : "PHONE_" + phone;
        }

        /// <summary>
        /// Tách mã đơn khỏi nội dung chuyển khoản (SePay có thể trả về field `code` hoặc chỉ có `content`).
        /// </summary>
        public static string ExtractOrderCode(string code, string content)
        {
            string fromCode = OrderCodeGenerator.Normalize(code);
            if (OrderCodeGenerator.IsWellFormed(fromCode))
            {
                return fromCode;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            // Mã đơn dạng FLyyMMdd + 5 ký tự Base32 không nhập nhằng (I, O, 0, 1)
            var match = Regex.Match(content.ToUpperInvariant(), @"FL\d{6}[A-HJ-NP-Z2-9]{5}");

            if (match.Success && OrderCodeGenerator.IsWellFormed(match.Value))
            {
                return match.Value;
            }

            return string.Empty;
        }

        /// <summary>So khớp số tiền nhận được với giá trị đơn (cho phép lệch &lt; 1 đồng do làm tròn).</summary>
        public static bool AmountMatches(decimal expected, decimal received)
        {
            if (expected <= 0m)
            {
                return false;
            }

            return Math.Abs(expected - received) < 1m;
        }

        public static bool AmountMatches(decimal expected, string receivedText)
        {
            decimal received;

            if (!decimal.TryParse(receivedText, NumberStyles.Any, CultureInfo.InvariantCulture, out received))
            {
                return false;
            }

            return AmountMatches(expected, received);
        }

        /// <summary>IPN chỉ được coi là thanh toán thành công khi ORDER_PAID + giao dịch APPROVED.</summary>
        public static bool IsSuccessfulNotification(SePayIpnPayload payload)
        {
            if (payload == null)
            {
                return false;
            }

            if (!string.Equals(payload.NotificationType, "ORDER_PAID", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (payload.Transaction == null)
            {
                return false;
            }

            return string.Equals(payload.Transaction.TransactionStatus, "APPROVED", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsVoidNotification(SePayIpnPayload payload)
        {
            return payload != null
                && string.Equals(payload.NotificationType, "TRANSACTION_VOID", StringComparison.OrdinalIgnoreCase);
        }

        public static SePayIpnPayload ParseIpn(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<SePayIpnPayload>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static SePayBankWebhookPayload ParseBankWebhook(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<SePayBankWebhookPayload>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
