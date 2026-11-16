using System;
using System.Collections.Generic;
using System.Linq;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.Orders;
using System.Text.RegularExpressions;

namespace FlowerShop.Services.AI
{
    /// <summary>
    /// Chatbot tư vấn hoa. Hiện là rule-based (không gọi LLM) nhưng đã tách
    /// <see cref="ChatbotIntentDetector"/> để unit test và để thay bằng Gemini sau này
    /// mà không đổi hợp đồng dữ liệu trả về cho FE.
    /// </summary>
    public class ChatbotService
    {
        public static ChatResponseViewModel ProcessMessage(string userMessage, string sessionId)
        {
            var response = new ChatResponseViewModel();

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                response.Reply = "Dạ chào bạn! Em là Florist AI của tiệm hoa. Em có thể giúp bạn chọn mẫu hoa ưng ý, viết lời chúc thiệp ý nghĩa, hoặc tra cứu đơn hàng giúp bạn ạ! 🌸";
                return response;
            }

            string cleanMsg = userMessage.Trim();
            ChatIntent intent = ChatbotIntentDetector.Detect(cleanMsg);

            using (var db = new FlowerShopDbContext())
            {
                if (intent == ChatIntent.TrackOrder)
                {
                    var tracking = TryTrackOrder(db, cleanMsg, intent);
                    if (tracking != null)
                    {
                        return tracking;
                    }
                }

                if (intent == ChatIntent.WriteCard)
                {
                    response.Reply = GenerateCardMessage(cleanMsg.ToLowerInvariant());
                    return response;
                }

                response.Reply = RecommendFlowers(db, intent);
                response.SuggestedProducts = SuggestProducts(db, intent);
            }

            return response;
        }

        private static ChatResponseViewModel TryTrackOrder(FlowerShopDbContext db, string message, ChatIntent intent)
        {
            string orderCode = ChatbotIntentDetector.ExtractOrderCode(message);
            string phone = ChatbotIntentDetector.NormalizePhone(message);

            if (string.IsNullOrEmpty(orderCode) && string.IsNullOrEmpty(phone))
            {
                return null;
            }

            var foundOrder = db.Orders.AsNoTracking()
                .Where(o => (orderCode != "" && o.OrderCode == orderCode)
                         || (phone != "" && (o.SenderPhone == phone || o.RecipientPhone == phone)))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.OrderCode,
                    o.RecipientName,
                    o.RecipientAddress,
                    o.DeliveryDate,
                    o.DeliveryTimeSlot,
                    o.OrderStatus,
                    o.PaymentStatus,
                    o.TotalAmount,
                    o.ProofPhotoUrl
                })
                .FirstOrDefault();

            if (foundOrder == null)
            {
                return new ChatResponseViewModel
                {
                    Reply = string.IsNullOrEmpty(orderCode)
                        ? "Dạ em chưa tìm thấy đơn hàng nào gắn với số điện thoại này. Bạn kiểm tra lại giúp em nhé!"
                        : string.Format("Dạ em chưa tìm thấy đơn hàng mã **{0}**. Bạn vui lòng kiểm tra lại mã đơn hoặc gửi số điện thoại đặt hàng để em tra cứu giúp nhé!", orderCode)
                };
            }

            string statusName = OrderNotificationService.GetStatusDisplayName(foundOrder.OrderStatus);

            var response = new ChatResponseViewModel
            {
                Reply = string.Format(
                    "Dạ em đã tìm thấy đơn hàng **{0}** của bạn!\n📍 **Người nhận:** {1} - {2}\n⏰ **Thời gian giao:** {3:dd/MM/yyyy} ({4})\n🌸 **Trạng thái:** {5}\n💰 **Tổng tiền:** {6:N0} đ ({7}){8}",
                    foundOrder.OrderCode,
                    foundOrder.RecipientName,
                    foundOrder.RecipientAddress,
                    foundOrder.DeliveryDate,
                    foundOrder.DeliveryTimeSlot,
                    statusName,
                    foundOrder.TotalAmount,
                    foundOrder.PaymentStatus,
                    string.IsNullOrEmpty(foundOrder.ProofPhotoUrl)
                        ? string.Empty
                        : "\n📸 Bạn có thể bấm vào thẻ bên dưới để xem ảnh hoa thực tế nghệ nhân đã cắm nhé!"),
                OrderSummary = new OrderStatusSummary
                {
                    OrderCode = foundOrder.OrderCode,
                    Status = foundOrder.OrderStatus,
                    StatusName = statusName,
                    DeliveryTime = string.Format("{0:dd/MM/yyyy} ({1})", foundOrder.DeliveryDate, foundOrder.DeliveryTimeSlot),
                    ProofPhotoUrl = foundOrder.ProofPhotoUrl
                }
            };

            return response;
        }

        private static string RecommendFlowers(FlowerShopDbContext db, ChatIntent intent)
        {
            switch (intent)
            {
                case ChatIntent.RecommendBirthday:
                    return "Dạ, với hoa sinh nhật, em gợi ý cho bạn những mẫu hoa rạng rỡ mang ý nghĩa tuổi mới may mắn và hạnh phúc nhất:";
                case ChatIntent.RecommendLove:
                    return "Dạ tặng người yêu hay vợ thì hoa Hồng Ecuador đỏ nhung hoặc Tulip hồng là tuyệt vời nhất, thể hiện tình yêu nồng cháy và sự trân trọng:";
                case ChatIntent.RecommendMother:
                    return "Dạ tặng Mẹ hoặc Cô giáo, tiệm có những giỏ hoa tone Pastel ấm áp, Cẩm Tú Cầu và Lan Hồ Điệp biểu trưng cho lòng biết ơn sâu sắc:";
                case ChatIntent.RecommendGrandOpening:
                    return "Dạ mừng khai trương hồng phát và chúc mừng đối tác, hoa Hướng Dương và Kệ hoa đại tài lộc tone Vàng Kim là lựa chọn hàng đầu ạ:";
                case ChatIntent.RecommendBudget:
                    return "Dạ với tầm giá tiết kiệm, shop có những mẫu hoa bó mini và hoa baby xinh xắn cực kỳ được yêu thích:";
                default:
                    return "Dạ chào bạn! Tiệm hoa gửi bạn tham khảo các mẫu hoa tươi nghệ thuật bán chạy và được yêu thích nhất hôm nay ạ:";
            }
        }

        private static List<ProductSuggestion> SuggestProducts(FlowerShopDbContext db, ChatIntent intent)
        {
            var query = db.Products.AsNoTracking().Where(p => p.IsActive);

            switch (intent)
            {
                case ChatIntent.RecommendBirthday:
                    query = query.Where(p => p.Occasion.Slug.Contains("sinh-nhat") || p.IsBestSeller);
                    break;
                case ChatIntent.RecommendLove:
                    query = query.Where(p => p.ColorTone == "Đỏ" || p.ColorTone == "Hồng" || p.Occasion.Slug.Contains("tinh-yeu"));
                    break;
                case ChatIntent.RecommendMother:
                    query = query.Where(p => p.Occasion.Slug.Contains("tri-an") || p.Category.Slug.Contains("gio"));
                    break;
                case ChatIntent.RecommendGrandOpening:
                    query = query.Where(p => p.ColorTone == "Vàng" || p.Occasion.Slug.Contains("khai-truong"));
                    break;
                case ChatIntent.RecommendBudget:
                    query = query.Where(p => (p.SalePrice ?? p.Price) <= 650000m);
                    break;
                default:
                    query = query.Where(p => p.IsBestSeller || p.IsFeatured);
                    break;
            }

            var list = query
                .OrderByDescending(p => p.RatingAvg)
                .Take(3)
                .Select(p => new ProductSuggestion
                {
                    Id = p.Id,
                    Name = p.Name,
                    ImageUrl = p.MainImageUrl,
                    Price = p.SalePrice ?? p.Price,
                    Slug = p.Slug
                })
                .ToList();

            if (!list.Any())
            {
                list = db.Products.AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.RatingAvg)
                    .Take(3)
                    .Select(p => new ProductSuggestion
                    {
                        Id = p.Id,
                        Name = p.Name,
                        ImageUrl = p.MainImageUrl,
                        Price = p.SalePrice ?? p.Price,
                        Slug = p.Slug
                    })
                    .ToList();
            }

            return list;
        }

        private static string GenerateCardMessage(string lowerMessage)
        {
            if (lowerMessage.Contains("mẹ"))
            {
                return "💌 **Gợi ý lời chúc tặng Mẹ yêu:**\n\n" +
                       "1. *'Cảm ơn Mẹ vì đã luôn là bến đỗ bình yên nhất của con. Chúc Mẹ luôn mạnh khỏe, yêu đời và mãi nụ cười rạng rỡ như những đóa hoa này!'*\n\n" +
                       "2. *'Mẹ là đóa hoa đẹp nhất trong cuộc đời con. Chúc người phụ nữ vĩ đại nhất của gia đình ta một ngày tràn ngập niềm vui và hạnh phúc!'*";
            }

            if (lowerMessage.Contains("khai trương"))
            {
                return "💌 **Gợi ý lời chúc Khai Trương hồng phát:**\n\n" +
                       "1. *'Chúc Công ty/Cửa hàng khai trương hồng phát, tài lộc dồi dào, vạn sự hanh thông và ngày càng phát triển thịnh vượng!'*\n\n" +
                       "2. *'Mừng bạn mở rộng chi nhánh mới! Chúc buôn may bán đắt, khách đến nườm nượp, tiền vô như nước!'*";
            }

            return "💌 **Gợi ý lời chúc Sinh Nhật / Tình Yêu ngọt ngào:**\n\n" +
                   "1. *'Chúc em tuổi mới luôn rạng rỡ, xinh đẹp và hạnh phúc. Cảm ơn em đã xuất hiện và làm cho thế giới của anh trở nên ngập tràn màu sắc!'*\n\n" +
                   "2. *'Mỗi cánh hoa thay cho một lời yêu thương chân thành nhất anh dành tặng em. Happy Birthday my love!'*";
        }
    }
}
