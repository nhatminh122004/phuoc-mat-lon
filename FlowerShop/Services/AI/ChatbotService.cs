using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FlowerShop.DAL;
using FlowerShop.Models.Entities;
using FlowerShop.Models.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FlowerShop.Services.AI
{
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
            string lowerMsg = cleanMsg.ToLowerInvariant();

            using (var db = new FlowerShopDbContext())
            {
                // 1. Kiểm tra nếu hỏi tra cứu đơn hàng (chứa mã đơn FL... hoặc từ 'tra cứu', 'kiểm tra đơn', 'đơn hàng')
                var orderCodeMatch = Regex.Match(cleanMsg, @"(FL[\d\-]+|[0-9]{8,15})", RegexOptions.IgnoreCase);
                if (orderCodeMatch.Success || lowerMsg.Contains("đơn hàng") || lowerMsg.Contains("tra cứu"))
                {
                    string searchKey = orderCodeMatch.Success ? orderCodeMatch.Value : cleanMsg;
                    var foundOrder = db.Orders
                        .Include("OrderDetails")
                        .FirstOrDefault(o => o.OrderCode.Equals(searchKey, StringComparison.OrdinalIgnoreCase) 
                                          || o.SenderPhone == searchKey 
                                          || o.RecipientPhone == searchKey);

                    if (foundOrder != null)
                    {
                        string statusText = GetVietnameseOrderStatus(foundOrder.OrderStatus);
                        response.Reply = $"Dạ em đã tìm thấy đơn hàng **{foundOrder.OrderCode}** của bạn!\n" +
                                         $"📍 **Người nhận:** {foundOrder.RecipientName} - {foundOrder.RecipientAddress}\n" +
                                         $"⏰ **Thời gian giao:** {foundOrder.DeliveryDate:dd/MM/yyyy} ({foundOrder.DeliveryTimeSlot})\n" +
                                         $"🌸 **Trạng thái:** {statusText}\n" +
                                         $"💰 **Tổng tiền:** {foundOrder.TotalAmount:N0} đ ({foundOrder.PaymentStatus})\n" +
                                         (string.IsNullOrEmpty(foundOrder.ProofPhotoUrl) ? "" : "📸 Bạn có thể bấm vào thẻ bên dưới để xem ảnh hoa thực tế nghệ nhân đã cắm nhé!");

                        response.OrderSummary = new OrderStatusSummary
                        {
                            OrderCode = foundOrder.OrderCode,
                            Status = foundOrder.OrderStatus,
                            StatusName = statusText,
                            DeliveryTime = $"{foundOrder.DeliveryDate:dd/MM/yyyy} ({foundOrder.DeliveryTimeSlot})",
                            ProofPhotoUrl = foundOrder.ProofPhotoUrl
                        };
                        return response;
                    }
                    else if (orderCodeMatch.Success)
                    {
                        response.Reply = $"Dạ em chưa tìm thấy đơn hàng mã **{orderCodeMatch.Value}** trong hệ thống. Bạn vui lòng kiểm tra lại mã đơn hoặc gửi số điện thoại đặt hàng để em tra cứu giúp nhé!";
                        return response;
                    }
                }

                // 2. Kiểm tra nếu nhờ viết lời chúc / thiệp
                if (lowerMsg.Contains("lời chúc") || lowerMsg.Contains("viết thiệp") || lowerMsg.Contains("câu chúc") || lowerMsg.Contains("thơ"))
                {
                    response.Reply = GenerateCardMessage(lowerMsg);
                    return response;
                }

                // 3. Tư vấn chọn hoa theo ngân sách & dịp tặng
                var (suggestedText, products) = RecommendFlowers(db, lowerMsg);
                response.Reply = suggestedText;
                response.SuggestedProducts = products;
            }

            return response;
        }

        private static (string, List<ProductSuggestion>) RecommendFlowers(FlowerShopDbContext db, string query)
        {
            var productsQuery = db.Products.Where(p => p.IsActive);
            string reply = "";

            if (query.Contains("sinh nhật"))
            {
                reply = "Dạ, với hoa sinh nhật, em gợi ý cho bạn những mẫu hoa rạng rỡ mang ý nghĩa tuổi mới may mắn và hạnh phúc nhất:";
                productsQuery = productsQuery.Where(p => p.Occasion.Slug.Contains("sinh-nhat") || p.IsBestSeller);
            }
            else if (query.Contains("người yêu") || query.Contains("bạn gái") || query.Contains("tình yêu") || query.Contains("valentine") || query.Contains("vợ"))
            {
                reply = "Dạ tặng người yêu hay vợ thì hoa Hồng Ecuador đỏ nhung hoặc Tulip hồng là tuyệt vời nhất, thể hiện tình yêu nồng cháy và sự trân trọng:";
                productsQuery = productsQuery.Where(p => p.ColorTone == "Đỏ" || p.ColorTone == "Hồng" || p.Occasion.Slug.Contains("tinh-yeu"));
            }
            else if (query.Contains("mẹ") || query.Contains("tri ân") || query.Contains("cô"))
            {
                reply = "Dạ tặng Mẹ hoặc Cô giáo, tiệm có những giỏ hoa tone Pastel ấm áp, Cẩm Tú Cầu và Lan Hồ Điệp biểu trưng cho lòng biết ơn sâu sắc:";
                productsQuery = productsQuery.Where(p => p.Occasion.Slug.Contains("tri-an") || p.Category.Slug.Contains("gio"));
            }
            else if (query.Contains("khai trương") || query.Contains("chúc mừng") || query.Contains("đối tác") || query.Contains("sếp"))
            {
                reply = "Dạ mừng khai trương hồng phát và chúc mừng đối tác, hoa Hướng Dương và Kệ hoa đại tài lộc tone Vàng Kim là lựa chọn hàng đầu ạ:";
                productsQuery = productsQuery.Where(p => p.ColorTone == "Vàng" || p.Occasion.Slug.Contains("khai-truong"));
            }
            else if (query.Contains("dưới 500") || query.Contains("500k") || query.Contains("rẻ"))
            {
                reply = "Dạ với tầm giá tiết kiệm dưới 600k, shop có những mẫu hoa bó mini và hoa baby xinh xắn cực kỳ được yêu thích:";
                productsQuery = productsQuery.Where(p => (p.SalePrice ?? p.Price) <= 650000);
            }
            else
            {
                reply = "Dạ chào bạn! Tiệm hoa gửi bạn tham khảo các mẫu hoa tươi nghệ thuật bán chạy và được yêu thích nhất hôm nay ạ:";
                productsQuery = productsQuery.Where(p => p.IsBestSeller || p.IsFeatured);
            }

            var list = productsQuery.Take(3).Select(p => new ProductSuggestion
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.MainImageUrl,
                Price = p.SalePrice ?? p.Price,
                Slug = p.Slug
            }).ToList();

            if (!list.Any())
            {
                list = db.Products.Where(p => p.IsActive).OrderBy(p => p.Id).Take(3).Select(p => new ProductSuggestion
                {
                    Id = p.Id,
                    Name = p.Name,
                    ImageUrl = p.MainImageUrl,
                    Price = p.SalePrice ?? p.Price,
                    Slug = p.Slug
                }).ToList();
            }

            return (reply, list);
        }

        private static string GenerateCardMessage(string query)
        {
            if (query.Contains("mẹ"))
            {
                return "💌 **Gợi ý lời chúc tặng Mẹ yêu:**\n\n" +
                       "1. *'Cảm ơn Mẹ vì đã luôn là bến đỗ bình yên nhất của con. Chúc Mẹ luôn mạnh khỏe, yêu đời và mãi nụ cười rạng rỡ như những đóa hoa này!'*\n\n" +
                       "2. *'Mẹ là đóa hoa đẹp nhất trong cuộc đời con. Chúc người phụ nữ vĩ đại nhất của gia đình ta một ngày tràn ngập niềm vui và hạnh phúc!'*";
            }
            if (query.Contains("khai trương"))
            {
                return "💌 **Gợi ý lời chúc Khai Trương hồng phát:**\n\n" +
                       "1. *'Chúc Công ty/Cửa hàng khai trương hồng phát, tài lộc dồi dào, vạn sự hanh thông và ngày càng phát triển thịnh vượng!'*\n\n" +
                       "2. *'Mừng bạn mở rộng chi nhánh mới! Chúc buôn may bán đắt, khách đến nườm nượp, tiền vô như nước!'*";
            }
            return "💌 **Gợi ý lời chúc Sinh Nhật / Tình Yêu ngọt ngào:**\n\n" +
                   "1. *'Chúc em tuổi mới luôn rạng rỡ, xinh đẹp và hạnh phúc. Cảm ơn em đã xuất hiện và làm cho thế giới của anh trở nên ngập tràn màu sắc!'*\n\n" +
                   "2. *'Mỗi cánh hoa thay cho một lời yêu thương chân thành nhất anh dành tặng em. Happy Birthday my love!'*";
        }

        private static string GetVietnameseOrderStatus(string status)
        {
            switch (status)
            {
                case "Pending": return "Chờ xác nhận";
                case "Confirmed": return "Đã xác nhận";
                case "Arranging": return "Nghệ nhân đang cắm hoa";
                case "ProofReady": return "Đã cắm xong - Chờ khách duyệt ảnh";
                case "Shipping": return "Shipper đang giao hàng";
                case "Delivered": return "Đã giao hoa thành công";
                case "Cancelled": return "Đã hủy đơn";
                default: return status;
            }
        }
    }
}
