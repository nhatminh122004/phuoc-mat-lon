using System;
using System.Data.Entity;

namespace FlowerShop.DAL
{
    /// <summary>
    /// Adapter mỏng bọc DbContext để service nghiệp vụ (StockService, VoucherUsageService)
    /// không phụ thuộc trực tiếp EF, đồng thời cho phép unit test thay thế bằng fake.
    /// </summary>
    public class DbContextAdapter
    {
        private readonly FlowerShopDbContext _db;

        public DbContextAdapter(FlowerShopDbContext db)
        {
            if (db == null)
            {
                throw new ArgumentNullException("db");
            }

            _db = db;
        }

        public FlowerShopDbContext Context
        {
            get { return _db; }
        }

        /// <summary>
        /// Trừ kho nguyên tử: chỉ thành công khi còn đủ hàng.
        /// </summary>
        public bool TryDecrementStock(int productId, int quantity)
        {
            const string sql =
                "UPDATE `Products` SET `StockQuantity` = `StockQuantity` - {1} " +
                "WHERE `Id` = {0} AND `IsActive` = 1 AND `StockQuantity` >= {1}";

            int affected = _db.Database.ExecuteSqlCommand(sql, productId, quantity);
            return affected == 1;
        }

        /// <summary>
        /// Hoàn kho khi đơn bị huỷ.
        /// </summary>
        public void IncreaseStock(int productId, int quantity)
        {
            const string sql =
                "UPDATE `Products` SET `StockQuantity` = `StockQuantity` + {1} WHERE `Id` = {0}";

            _db.Database.ExecuteSqlCommand(sql, productId, quantity);
        }

        /// <summary>
        /// Tăng lượt dùng voucher nguyên tử, chặn vượt UsageLimit.
        /// </summary>
        public bool TryConsumeVoucher(string code, int nowUsedCount)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            const string sql =
                "UPDATE `Vouchers` SET `UsedCount` = `UsedCount` + 1 " +
                "WHERE `Code` = {0} AND `IsActive` = 1 AND (`UsageLimit` = 0 OR `UsedCount` < `UsageLimit`)";

            int affected = _db.Database.ExecuteSqlCommand(sql, code.Trim());
            return affected == 1;
        }
    }
}
