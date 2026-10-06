using System;
using System.Configuration;
using System.Web;
using FlowerShop.Models.Entities;

namespace FlowerShop.Services.VNPay
{
    public class VnPayService
    {
        public static string CreatePaymentUrl(Order order, HttpContextBase context)
        {
            var vnpUrl = ConfigurationManager.AppSettings["Vnpay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var vnpTmnCode = ConfigurationManager.AppSettings["Vnpay:TmnCode"] ?? "DEMOSHOP";
            var vnpHashSecret = ConfigurationManager.AppSettings["Vnpay:HashSecret"] ?? "SECRETKEY1234567890ABCDEF";
            var returnUrl = ConfigurationManager.AppSettings["Vnpay:ReturnUrl"] ?? "http://localhost:5000/Payment/VnpayReturn";

            var pay = new VnPayLibrary();
            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_TmnCode", vnpTmnCode);
            pay.AddRequestData("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString()); // Số tiền nhân 100 theo quy định VNPay
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa tuoi don hang {order.OrderCode}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", returnUrl);
            pay.AddRequestData("vnp_TxnRef", order.OrderCode);

            return pay.CreateRequestUrl(vnpUrl, vnpHashSecret);
        }

        public static bool ValidateCallback(HttpRequestBase request, out string orderCode, out string responseCode, out string transactionNo)
        {
            orderCode = string.Empty;
            responseCode = string.Empty;
            transactionNo = string.Empty;

            var vnpHashSecret = ConfigurationManager.AppSettings["Vnpay:HashSecret"] ?? "SECRETKEY1234567890ABCDEF";
            var pay = new VnPayLibrary();

            foreach (string s in request.QueryString)
            {
                if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                {
                    pay.AddResponseData(s, request.QueryString[s]);
                }
            }

            orderCode = pay.GetResponseData("vnp_TxnRef");
            responseCode = pay.GetResponseData("vnp_ResponseCode");
            transactionNo = pay.GetResponseData("vnp_TransactionNo");
            var vnpSecureHash = request.QueryString["vnp_SecureHash"];

            return pay.ValidateSignature(vnpSecureHash, vnpHashSecret);
        }

        private static string GetIpAddress(HttpContextBase context)
        {
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ipAddress) || ipAddress.ToLower() == "unknown")
            {
                ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];
            }
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }
    }
}
