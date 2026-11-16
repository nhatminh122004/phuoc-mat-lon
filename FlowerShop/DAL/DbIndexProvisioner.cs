using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace FlowerShop.DAL
{
    public class IndexDefinition
    {
        public string Table { get; private set; }
        public string Name { get; private set; }
        public string Columns { get; private set; }
        public bool IsUnique { get; private set; }
        public bool IsFullText { get; private set; }

        public IndexDefinition(string table, string name, string columns, bool isUnique = false, bool isFullText = false)
        {
            Table = table;
            Name = name;
            Columns = columns;
            IsUnique = isUnique;
            IsFullText = isFullText;
        }
    }

    /// <summary>
    /// Danh mục chỉ mục cho tải truy vấn lớn. Mỗi chỉ mục ở đây tương ứng 1 truy vấn thật
    /// trong code (product filter, dashboard, tra cứu đơn, admin list...). Dùng cho cả
    /// file dump SQL và bước tự bổ sung chỉ mục khi DB được tạo bằng EF.
    /// </summary>
    public static class DbIndexProvisioner
    {
        public static readonly IReadOnlyList<IndexDefinition> Catalog = new List<IndexDefinition>
        {
            // ---------- Products ----------
            new IndexDefinition("Products", "UX_Products_Slug", "`Slug`", true),
            new IndexDefinition("Products", "IX_Products_Category_Active", "`CategoryId`,`IsActive`"),
            new IndexDefinition("Products", "IX_Products_Occasion_Active", "`OccasionId`,`IsActive`"),
            new IndexDefinition("Products", "IX_Products_FlowerType_Active", "`FlowerTypeId`,`IsActive`"),
            new IndexDefinition("Products", "IX_Products_Active_Created", "`IsActive`,`CreatedAt`"),
            new IndexDefinition("Products", "IX_Products_Active_Price", "`IsActive`,`Price`"),
            new IndexDefinition("Products", "IX_Products_Active_Color", "`IsActive`,`ColorTone`"),
            new IndexDefinition("Products", "IX_Products_Active_BestSeller", "`IsActive`,`IsBestSeller`"),
            new IndexDefinition("Products", "IX_Products_Active_FlashSale", "`IsActive`,`IsFlashSale`"),
            new IndexDefinition("Products", "IX_Products_Active_Featured", "`IsActive`,`IsFeatured`"),
            new IndexDefinition("Products", "IX_Products_Active_Rating", "`IsActive`,`RatingAvg`"),
            new IndexDefinition("Products", "IX_Products_Category_Active_Price", "`CategoryId`,`IsActive`,`Price`"),
            new IndexDefinition("Products", "IX_Products_SKU", "`SKU`"),
            new IndexDefinition("Products", "FT_Products_Search", "`Name`,`ShortDescription`,`FullDescription`", false, true),

            // ---------- Danh mục ----------
            new IndexDefinition("Categories", "UX_Categories_Slug", "`Slug`", true),
            new IndexDefinition("Categories", "IX_Categories_Active_Order", "`IsActive`,`DisplayOrder`"),
            new IndexDefinition("Occasions", "UX_Occasions_Slug", "`Slug`", true),
            new IndexDefinition("Occasions", "IX_Occasions_Active_Order", "`IsActive`,`DisplayOrder`"),
            new IndexDefinition("FlowerTypes", "UX_FlowerTypes_Slug", "`Slug`", true),
            new IndexDefinition("ProductImages", "IX_ProductImages_Product_Order", "`ProductId`,`DisplayOrder`"),
            new IndexDefinition("AddonItems", "IX_AddonItems_Active", "`IsActive`"),
            new IndexDefinition("GreetingCards", "IX_GreetingCards_Active", "`IsActive`"),

            // ---------- Voucher ----------
            new IndexDefinition("Vouchers", "UX_Vouchers_Code", "`Code`", true),
            new IndexDefinition("Vouchers", "IX_Vouchers_Active_Dates", "`IsActive`,`StartDate`,`EndDate`"),

            // ---------- Orders ----------
            new IndexDefinition("Orders", "UX_Orders_OrderCode", "`OrderCode`", true),
            new IndexDefinition("Orders", "IX_Orders_CreatedAt", "`CreatedAt`"),
            new IndexDefinition("Orders", "IX_Orders_Status_CreatedAt", "`OrderStatus`,`CreatedAt`"),
            new IndexDefinition("Orders", "IX_Orders_PaymentStatus_CreatedAt", "`PaymentStatus`,`CreatedAt`"),
            new IndexDefinition("Orders", "IX_Orders_User_CreatedAt", "`UserId`,`CreatedAt`"),
            new IndexDefinition("Orders", "IX_Orders_SenderEmail", "`SenderEmail`,`CreatedAt`"),
            new IndexDefinition("Orders", "IX_Orders_SenderPhone", "`SenderPhone`"),
            new IndexDefinition("Orders", "IX_Orders_RecipientPhone", "`RecipientPhone`"),
            new IndexDefinition("Orders", "IX_Orders_DeliveryDate_Status", "`DeliveryDate`,`OrderStatus`"),
            new IndexDefinition("OrderDetails", "IX_OrderDetails_Order", "`OrderId`"),
            new IndexDefinition("OrderDetails", "IX_OrderDetails_Product", "`ProductId`"),
            new IndexDefinition("OrderAddons", "IX_OrderAddons_Order", "`OrderId`"),
            new IndexDefinition("PaymentTransactions", "UX_PaymentTransactions_Gateway_Txn", "`Gateway`,`TransactionNo`", true),
            new IndexDefinition("PaymentTransactions", "IX_PaymentTransactions_Order", "`OrderId`"),

            // ---------- Review / User / Token ----------
            new IndexDefinition("Reviews", "IX_Reviews_Product_Approved_Created", "`ProductId`,`IsApproved`,`CreatedAt`"),
            new IndexDefinition("Users", "UX_Users_Username", "`Username`", true),
            new IndexDefinition("Users", "UX_Users_Email", "`Email`", true),
            new IndexDefinition("Users", "IX_Users_Role_Active", "`Role`,`IsActive`"),
            new IndexDefinition("PasswordResetTokens", "UX_PRT_TokenHash", "`TokenHash`", true),
            new IndexDefinition("PasswordResetTokens", "IX_PRT_User_Created", "`UserId`,`CreatedAt`"),
            new IndexDefinition("PasswordResetTokens", "IX_PRT_ExpiresAt", "`ExpiresAt`")
        };

        public static string BuildCreateIndexStatement(IndexDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException("definition");
            }

            string kind;
            if (definition.IsFullText)
            {
                kind = "FULLTEXT INDEX";
            }
            else if (definition.IsUnique)
            {
                kind = "UNIQUE INDEX";
            }
            else
            {
                kind = "INDEX";
            }

            return string.Format("CREATE {0} `{1}` ON `{2}` ({3})",
                kind, definition.Name, definition.Table, definition.Columns);
        }

        public static string BuildDropIndexStatement(IndexDefinition definition)
        {
            return string.Format("DROP INDEX `{0}` ON `{1}`", definition.Name, definition.Table);
        }

        /// <summary>
        /// Tạo các chỉ mục còn thiếu. An toàn khi chạy nhiều lần (idempotent).
        /// Trả về số chỉ mục đã tạo mới.
        /// </summary>
        public static int EnsureIndexes(DbContext db)
        {
            if (db == null)
            {
                return 0;
            }

            if (!IsMySqlProvider(db))
            {
                // Unit test / provider khác: bỏ qua, không ném lỗi.
                return 0;
            }

            var existing = LoadExistingIndexes(db);
            int created = 0;

            foreach (var definition in Catalog)
            {
                string key = definition.Table + "|" + definition.Name;
                if (existing.Contains(key))
                {
                    continue;
                }

                try
                {
                    db.Database.ExecuteSqlCommand(BuildCreateIndexStatement(definition));
                    created++;
                }
                catch (Exception)
                {
                    // Bảng chưa tồn tại (DB tạo dở) hoặc FULLTEXT không được hỗ trợ:
                    // không chặn khởi động ứng dụng, sẽ được xử lý bằng script SQL.
                }
            }

            return created;
        }

        private static bool IsMySqlProvider(DbContext db)
        {
            try
            {
                return db.Database.Connection.GetType().FullName.IndexOf("MySql", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static HashSet<string> LoadExistingIndexes(DbContext db)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var rows = db.Database.SqlQuery<IndexRow>(
                    "SELECT TABLE_NAME AS `TableName`, INDEX_NAME AS `IndexName` FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() GROUP BY TABLE_NAME, INDEX_NAME")
                    .ToList();

                foreach (var row in rows)
                {
                    result.Add(row.TableName + "|" + row.IndexName);
                }
            }
            catch
            {
                // Không đọc được information_schema: coi như chưa có chỉ mục.
            }

            return result;
        }

        public class IndexRow
        {
            public string TableName { get; set; }
            public string IndexName { get; set; }
        }

        /// <summary>
        /// Kiểm tra tĩnh: không có 2 chỉ mục trùng tên trên cùng bảng, không có bảng lạ.
        /// Được unit test gọi để chặn sai sót khi thêm chỉ mục mới.
        /// </summary>
        public static IList<string> ValidateCatalog(IEnumerable<string> knownTables)
        {
            var errors = new List<string>();
            var known = new HashSet<string>(knownTables ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in Catalog)
            {
                string key = definition.Table + "|" + definition.Name;
                if (!seen.Add(key))
                {
                    errors.Add("Trùng chỉ mục: " + key);
                }

                if (known.Count > 0 && !known.Contains(definition.Table))
                {
                    errors.Add("Bảng không tồn tại trong model: " + definition.Table);
                }

                if (string.IsNullOrWhiteSpace(definition.Columns))
                {
                    errors.Add("Chỉ mục không có cột: " + key);
                }
            }

            return errors;
        }
    }
}
