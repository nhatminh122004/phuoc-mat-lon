using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using FlowerShop.Models.Entities;
using Stripe;

namespace FlowerShop.Services.Stripe
{
    public class StripePaymentService
    {
        public static string PublishableKey => ConfigurationManager.AppSettings["Stripe:PublishableKey"] ?? "pk_test_TYoo61E7ZXqStWST8qjekqqQ";
        public static string SecretKey => ConfigurationManager.AppSettings["Stripe:SecretKey"] ?? "sk_test_your_stripe_secret_key_here";

        static StripePaymentService()
        {
            StripeConfiguration.ApiKey = SecretKey;
        }

        public static PaymentIntent CreatePaymentIntent(Order order)
        {
            // Chuyển đổi VND sang USD (tỷ giá ước tính 1 USD ~ 25.000 VND)
            long amountInCents = (long)Math.Round((order.TotalAmount / 25000m) * 100);
            if (amountInCents < 50) amountInCents = 50; // Tối thiểu 0.50 USD

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "usd",
                PaymentMethodTypes = new List<string> { "card" },
                Description = $"Order {order.OrderCode} - Flower Boutique",
                Metadata = new Dictionary<string, string>
                {
                    { "OrderCode", order.OrderCode },
                    { "RecipientName", order.RecipientName },
                    { "DeliveryDate", order.DeliveryDate.ToString("yyyy-MM-dd") }
                }
            };

            var service = new PaymentIntentService();
            return service.Create(options);
        }

        public static bool ConfirmPayment(string paymentIntentId, out string chargeId)
        {
            chargeId = string.Empty;
            try
            {
                var service = new PaymentIntentService();
                var intent = service.Get(paymentIntentId);
                if (intent.Status == "succeeded")
                {
                    chargeId = intent.LatestChargeId;
                    return true;
                }
            }
            catch
            {
                // Log exception
            }
            return false;
        }
    }
}
