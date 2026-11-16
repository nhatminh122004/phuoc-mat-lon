using System;
using System.Collections.Generic;
using System.Linq;
using FlowerShop.DAL;
using FlowerShop.Models.ViewModels;

namespace FlowerShop.Services.Orders
{
    public class StockRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Requested { get; set; }
        public int Available { get; set; }
    }

    public class StockCheckResult
    {
        public bool Ok { get; set; }
        public List<string> Errors { get; private set; }

        public StockCheckResult()
        {
            Errors = new List<string>();
        }
    }

    /// <summary>
    /// Kiểm tra & trừ tồn kho. Phần kiểm tra là pure function (unit test được);
    /// phần trừ dùng câu UPDATE có điều kiện để chống bán quá tồn khi tải cao:
    ///   UPDATE Products SET StockQuantity = StockQuantity - @qty
    ///    WHERE Id = @id AND IsActive = 1 AND StockQuantity >= @qty
    /// Nếu affected rows = 0 nghĩa là hết hàng hoặc sản phẩm bị tắt -> rollback transaction.
    /// </summary>
    public static class StockService
    {
        public static List<StockRequest> BuildRequests(IEnumerable<CartItem> cartItems, IDictionary<int, int> stockByProduct)
        {
            var result = new List<StockRequest>();

            if (cartItems == null)
            {
                return result;
            }

            foreach (var item in cartItems)
            {
                int available = 0;
                if (stockByProduct != null)
                {
                    stockByProduct.TryGetValue(item.ProductId, out available);
                }

                result.Add(new StockRequest
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Requested = item.Quantity,
                    Available = available
                });
            }

            return result;
        }

        public static StockCheckResult Validate(IEnumerable<StockRequest> requests)
        {
            var check = new StockCheckResult { Ok = true };

            if (requests == null)
            {
                return check;
            }

            foreach (var request in requests)
            {
                if (request.QuantityRequestedInvalid())
                {
                    check.Ok = false;
                    check.Errors.Add(string.Format("Số lượng đặt cho '{0}' không hợp lệ.", request.ProductName));
                    continue;
                }

                if (request.Available <= 0)
                {
                    check.Ok = false;
                    check.Errors.Add(string.Format("'{0}' đã hết hàng, vui lòng chọn mẫu hoa khác.", request.ProductName));
                    continue;
                }

                if (request.Requested > request.Available)
                {
                    check.Ok = false;
                    check.Errors.Add(string.Format("'{0}' chỉ còn {1} sản phẩm (bạn đặt {2}).",
                        request.ProductName, request.Available, request.Requested));
                }
            }

            return check;
        }

        /// <summary>
        /// Trừ kho nguyên tử. Trả về false nếu không đủ hàng / sản phẩm không bán.
        /// </summary>
        public static bool TryDecrement(DbContextAdapter db, int productId, int quantity)
        {
            if (db == null || productId <= 0 || quantity <= 0)
            {
                return false;
            }

            return db.TryDecrementStock(productId, quantity);
        }
    }

    internal static class StockRequestExtensions
    {
        /// <summary>Số lượng phải là số dương.</summary>
        public static bool QuantityRequestedInvalid(this StockRequest request)
        {
            return request == null || request.Requested <= 0;
        }
    }
}
