using System;
using System.Configuration;
using System.Net;
using FlowerShop.Models.Entities;

namespace FlowerShop.Services.VietQR
{
    public class VietQrService
    {
        public static string GenerateQrCodeUrl(Order order)
        {
            var bankId = ConfigurationManager.AppSettings["VietQR:BankId"] ?? "MB"; // MBBank, VCB, TCB, etc.
            var accountNo = ConfigurationManager.AppSettings["VietQR:AccountNo"] ?? "0909123456888";
            var accountName = ConfigurationManager.AppSettings["VietQR:AccountName"] ?? "TIEM HOA FLOWER BOUTIQUE";
            var memo = WebUtility.UrlEncode($"THANH TOAN {order.OrderCode}");
            var amount = (long)order.TotalAmount;

            // Định dạng VietQR Compact 2
            return $"https://img.vietqr.io/image/{bankId}-{accountNo}-compact2.png?amount={amount}&addInfo={memo}&accountName={WebUtility.UrlEncode(accountName)}";
        }
    }
}
