using System;
using System.Web.Mvc;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.AI;
using FlowerShop.Services.Logging;

namespace FlowerShop.Controllers
{
    public class ChatbotController : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendMessage(ChatRequestViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
            {
                return Json(new { success = false, reply = "Dạ tiệm hoa có thể hỗ trợ gì cho bạn ạ?" });
            }

            try
            {
                string sessionId = Session != null ? Session.SessionID : "anonymous";
                var response = ChatbotService.ProcessMessage(model.Message, sessionId);

                return Json(new
                {
                    success = true,
                    reply = response.Reply,
                    products = response.SuggestedProducts,
                    orderSummary = response.OrderSummary
                });
            }
            catch (Exception ex)
            {
                AppLogger.Error("Chatbot lỗi khi xử lý tin nhắn: " + model.Message, ex);
                return Json(new
                {
                    success = false,
                    reply = "Dạ em đang gặp chút trục trặc. Bạn vui lòng thử lại hoặc gọi hotline 0377 415 147 để được tư vấn ngay ạ!"
                });
            }
        }
    }
}
