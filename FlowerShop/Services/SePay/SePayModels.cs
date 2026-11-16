using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FlowerShop.Services.SePay
{
    /// <summary>
    /// Bộ thông tin merchant dùng để ký form. Cho phép truyền vào tường minh
    /// (unit test / multi-tenant) thay vì luôn đọc Web.config.
    /// </summary>
    public class SePayCredentials
    {
        public string MerchantId { get; set; }
        public string SecretKey { get; set; }
        public string PaymentMethod { get; set; }

        public static SePayCredentials FromConfiguration()
        {
            return new SePayCredentials
            {
                MerchantId = SePayOptions.MerchantId,
                SecretKey = SePayOptions.SecretKey,
                PaymentMethod = SePayOptions.PaymentMethod
            };
        }
    }

    /// <summary>Form dữ liệu để POST tới https://pay.sepay.vn/v1/checkout/init.</summary>
    public class SePayCheckoutForm
    {
        public string ActionUrl { get; set; }
        public string InvoiceNumber { get; set; }
        public string Signature { get; set; }

        /// <summary>Field theo đúng thứ tự SePay yêu cầu (đã kèm signature).</summary>
        public IList<KeyValuePair<string, string>> Fields { get; set; }

        public SePayCheckoutForm()
        {
            Fields = new List<KeyValuePair<string, string>>();
        }
    }

    /// <summary>IPN của Cổng thanh toán SePay (POST JSON + header X-Secret-Key).</summary>
    public class SePayIpnPayload
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("notification_type")]
        public string NotificationType { get; set; }

        [JsonProperty("order")]
        public SePayIpnOrder Order { get; set; }

        [JsonProperty("transaction")]
        public SePayIpnTransaction Transaction { get; set; }

        [JsonProperty("customer")]
        public SePayIpnCustomer Customer { get; set; }
    }

    public class SePayIpnOrder
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }

        [JsonProperty("order_currency")]
        public string OrderCurrency { get; set; }

        [JsonProperty("order_amount")]
        public string OrderAmount { get; set; }

        [JsonProperty("order_invoice_number")]
        public string OrderInvoiceNumber { get; set; }

        [JsonProperty("order_description")]
        public string OrderDescription { get; set; }
    }

    public class SePayIpnTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("transaction_type")]
        public string TransactionType { get; set; }

        [JsonProperty("transaction_date")]
        public string TransactionDate { get; set; }

        [JsonProperty("transaction_status")]
        public string TransactionStatus { get; set; }

        [JsonProperty("transaction_amount")]
        public string TransactionAmount { get; set; }

        [JsonProperty("transaction_currency")]
        public string TransactionCurrency { get; set; }
    }

    public class SePayIpnCustomer
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }
    }

    /// <summary>
    /// Webhook biến động số dư ngân hàng (dùng khi chạy chế độ QR).
    /// Tham khảo: https://docs.sepay.vn/tich-hop-webhooks.html#payload
    /// </summary>
    public class SePayBankWebhookPayload
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("gateway")]
        public string Gateway { get; set; }

        [JsonProperty("transactionDate")]
        public string TransactionDate { get; set; }

        [JsonProperty("accountNumber")]
        public string AccountNumber { get; set; }

        [JsonProperty("subAccount")]
        public string SubAccount { get; set; }

        /// <summary>Mã thanh toán SePay tách được từ nội dung chuyển khoản.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>"in" = tiền vào, "out" = tiền ra.</summary>
        [JsonProperty("transferType")]
        public string TransferType { get; set; }

        [JsonProperty("transferAmount")]
        public decimal TransferAmount { get; set; }

        [JsonProperty("accumulated")]
        public decimal Accumulated { get; set; }

        [JsonProperty("referenceCode")]
        public string ReferenceCode { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
