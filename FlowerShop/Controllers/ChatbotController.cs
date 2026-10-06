using System.Web.Mvc;
using FlowerShop.Models.ViewModels;
using FlowerShop.Services.AI;

namespace FlowerShop.Controllers
{
    public class ChatbotController : Controller
    {
        [HttpPost]
        public JsonResult SendMessage(ChatRequestViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Message))
            {
                return Json(new { success = false, reply = "Dạ tiệm hoa có thể hỗ trợ gì cho bạn ạ?" });
            }

            string sessionId = Session.SessionID;
            var response = ChatbotService.ProcessMessage(model.Message, sessionId);

            return Json(new
            {
                success = true,
                reply = response.Reply,
                products = response.SuggestedProducts,
                orderSummary = response.OrderSummary
            });
        }
    }
}
