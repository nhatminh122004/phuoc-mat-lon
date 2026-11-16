using System;
using System.Configuration;

namespace FlowerShop.Services.SePay
{
    /// <summary>
    /// Cấu hình SePay đọc từ Web.config (admin có thể sửa ở trang Cấu hình).
    /// Hai môi trường: sandbox (mặc định) và production.
    /// </summary>
    public static class SePayOptions
    {
        public const string ModeGateway = "gateway";
        public const string ModeQr = "qr";

        /// <summary>"sandbox" hoặc "production".</summary>
        public static string EnvironmentName
        {
            get
            {
                string value = (ConfigurationManager.AppSettings["SePay:Env"] ?? "sandbox").Trim().ToLowerInvariant();
                return value == "production" || value == "live" ? "production" : "sandbox";
            }
        }

        public static bool IsSandbox
        {
            get { return EnvironmentName != "production"; }
        }

        /// <summary>
        /// "gateway" = chuyển khách sang trang thanh toán SePay (QR/thẻ/NAPAS).
        /// "qr"      = hiện QR chuyển khoản ngay trên site + xác nhận bằng webhook ngân hàng.
        /// </summary>
        public static string Mode
        {
            get
            {
                string value = (ConfigurationManager.AppSettings["SePay:Mode"] ?? ModeGateway).Trim().ToLowerInvariant();
                return value == ModeQr ? ModeQr : ModeGateway;
            }
        }

        public static string MerchantId
        {
            get { return (ConfigurationManager.AppSettings["SePay:MerchantId"] ?? string.Empty).Trim(); }
        }

        /// <summary>Secret Key đơn vị bán hàng (spsk_live_... / spsk_test_...), dùng ký form checkout.</summary>
        public static string SecretKey
        {
            get { return (ConfigurationManager.AppSettings["SePay:SecretKey"] ?? string.Empty).Trim(); }
        }

        /// <summary>Secret key merchant tự đặt ở tab IPN, SePay gửi kèm header X-Secret-Key.</summary>
        public static string IpnSecretKey
        {
            get { return (ConfigurationManager.AppSettings["SePay:IpnSecretKey"] ?? string.Empty).Trim(); }
        }

        /// <summary>CARD | BANK_TRANSFER | NAPAS_BANK_TRANSFER. Để trống = khách tự chọn trên trang SePay.</summary>
        public static string PaymentMethod
        {
            get { return (ConfigurationManager.AppSettings["SePay:PaymentMethod"] ?? "BANK_TRANSFER").Trim(); }
        }

        public static string CheckoutBaseUrl
        {
            get
            {
                string configured = ConfigurationManager.AppSettings["SePay:CheckoutBaseUrl"];
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return configured.Trim().TrimEnd('/');
                }

                return IsSandbox ? "https://pay-sandbox.sepay.vn" : "https://pay.sepay.vn";
            }
        }

        public static string CheckoutInitUrl
        {
            get { return CheckoutBaseUrl + "/v1/checkout/init"; }
        }

        /// <summary>Dùng cho chế độ QR: ngân hàng nhận tiền (mã hiển thị trên app ngân hàng).</summary>
        public static string Bank
        {
            get { return (ConfigurationManager.AppSettings["SePay:Bank"] ?? "MBBank").Trim(); }
        }

        public static string AccountNumber
        {
            get { return (ConfigurationManager.AppSettings["SePay:AccountNumber"] ?? string.Empty).Trim(); }
        }

        public static string AccountHolder
        {
            get { return (ConfigurationManager.AppSettings["SePay:AccountHolder"] ?? "TIEM HOA HUU PHUOC").Trim(); }
        }

        public static string QrBaseUrl
        {
            get
            {
                string configured = ConfigurationManager.AppSettings["SePay:QrBaseUrl"];
                return !string.IsNullOrWhiteSpace(configured)
                    ? configured.Trim().TrimEnd('/')
                    : "https://qr.sepay.vn";
            }
        }

        /// <summary>API key merchant khai báo trên webhook SePay (header Authorization: Bearer ...).</summary>
        public static string WebhookApiKey
        {
            get { return (ConfigurationManager.AppSettings["SePay:WebhookApiKey"] ?? string.Empty).Trim(); }
        }

        /// <summary>Nếu dùng xác thực HMAC-SHA256 cho webhook (header X-Signature) thì điền secret này.</summary>
        public static string WebhookHmacSecret
        {
            get { return (ConfigurationManager.AppSettings["SePay:WebhookHmacSecret"] ?? string.Empty).Trim(); }
        }

        /// <summary>Cấu hình tối thiểu để chạy chế độ gateway.</summary>
        public static bool IsGatewayConfigured
        {
            get { return !string.IsNullOrEmpty(MerchantId) && !string.IsNullOrEmpty(SecretKey); }
        }

        public static string PublicBaseUrl(string requestScheme, string requestAuthority)
        {
            string configured = ConfigurationManager.AppSettings["SePay:PublicBaseUrl"];

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim().TrimEnd('/');
            }

            return string.Format("{0}://{1}", requestScheme, requestAuthority);
        }

        public static bool ShouldConsumeVoucherOnPlacement
        {
            get { return false; } // SePay là cổng online: chỉ trừ lượt voucher khi đã Paid
        }
    }
}
