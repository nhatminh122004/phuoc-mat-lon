# Hiệu năng & tối ưu truy vấn

Mục tiêu: hệ thống chịu được **lưu lượng truy vấn lớn** (nhiều request đồng thời, hàng trăm nghìn đơn) mà không phải viết lại kiến trúc.

---

## 1. Nguyên tắc đã áp dụng trong code

### 1.1 Tắt lazy loading + proxy (chống N+1)

`DAL/FlowerShopDbContext.cs`:

```csharp
Configuration.LazyLoadingEnabled = false;
Configuration.ProxyCreationEnabled = false;
```

Trước đây bật lazy loading: trang danh sách 12 sản phẩm có thể sinh **13+ query** (1 cho list + 12 cho `product.Occasion.Name`). Nay mọi truy vấn phải khai báo `Include`/projection tường minh — sai là lỗi compile-time/runtime rõ ràng, không âm thầm chậm.

### 1.2 `AsNoTracking()` cho mọi truy vấn chỉ đọc

Các controller storefront (Home, Product, Cart, OrderTracking, Payment.Success) và admin (Order list, Product list, Review) đều dùng `AsNoTracking()`. Entity Framework không cần dựng change-tracking graph → giảm 20–40% bộ nhớ và thời gian materialize.

### 1.3 Projection thay vì load full entity

Ví dụ `CartService.AddProduct` chỉ lấy 6 cột cần thiết thay vì cả `Product` (gồm `FullDescription` TEXT):

```csharp
db.Products.AsNoTracking()
    .Where(p => p.Id == productId && p.IsActive)
    .Select(p => new { p.Id, p.Name, p.Slug, p.MainImageUrl, p.Price, p.SalePrice })
    .FirstOrDefault();
```

### 1.4 Phân trang phía server

- Storefront: 12 sản phẩm/trang (`ProductController.Index`).
- Admin đơn hàng: **20 đơn/trang** + đếm tổng (`OrderController.Index`).
- Admin review: 20/trang.

Bản cũ load **toàn bộ** đơn hàng vào RAM — sập ở ~50k đơn.

### 1.5 Cache danh mục

`HomeController` cache `Categories/Occasions/FlowerTypes` trong `MemoryCache` **5 phút** (dữ liệu gần như tĩnh). Được invalidate khi admin lưu Cấu hình (`HomeController.InvalidateCatalogCache()`); nếu thêm CRUD danh mục sau này, gọi hàm đó sau mỗi lần ghi.

### 1.6 Ghi nguyên tử, không round-trip

`DAL/DbContextAdapter.cs` dùng UPDATE có điều kiện, tránh race condition khi tải cao:

```sql
-- Trừ kho: chỉ thành công khi còn đủ hàng (chống oversell 100%)
UPDATE Products SET StockQuantity = StockQuantity - @qty
 WHERE Id = @id AND IsActive = 1 AND StockQuantity >= @qty;

-- Tăng lượt voucher: không vượt UsageLimit
UPDATE Vouchers SET UsedCount = UsedCount + 1
 WHERE Code = @code AND IsActive = 1 AND (UsageLimit = 0 OR UsedCount < UsageLimit);
```

`PlaceOrder` bọc toàn bộ trong `BeginTransaction()`: trừ kho fail ở bất kỳ sản phẩm nào → rollback.

### 1.7 Đơn đã thanh toán là idempotent

`MarkOrderPaid` kiểm tra `PaymentStatus == "Paid"` trước, và `PaymentTransactions` có `UNIQUE (Gateway, TransactionNo)` → SePay gọi IPN/webhook nhiều lần (retry) không tạo bản ghi trùng, không trừ voucher 2 lần.

---

## 2. Danh mục chỉ mục (43 index)

Nguồn sự thật: `DAL/DbIndexProvisioner.cs` — cùng danh sách được ghi vào file SQL dump và được `EnsureIndexes()` tự bổ sung khi DB tạo bằng EF.

### 2.1 Products (truy vấn nóng nhất)

| Index | Truy vấn phục vụ |
|---|---|
| `UX_Products_Slug` (unique) | `/hoa/{slug}` |
| `IX_Products_Category_Active` | lọc theo danh mục |
| `IX_Products_Occasion_Active` | lọc theo dịp tặng |
| `IX_Products_FlowerType_Active` | lọc theo loài hoa |
| `IX_Products_Active_Created` | sắp xếp mới nhất |
| `IX_Products_Active_Price` | sắp xếp/lọc giá |
| `IX_Products_Active_Color` | lọc tông màu |
| `IX_Products_Active_BestSeller` / `_FlashSale` / `_Featured` | 3 khối trang chủ |
| `IX_Products_Active_Rating` | sắp xếp theo đánh giá |
| `IX_Products_Category_Active_Price` | composite: lọc danh mục + sort giá |
| `IX_Products_SKU` | tra SKU ở admin |
| `FT_Products_Search` (FULLTEXT) | tìm theo tên/mô tả |

### 2.2 Orders

| Index | Truy vấn phục vụ |
|---|---|
| `UX_Orders_OrderCode` (unique) | tra cứu đơn, SePay `order_invoice_number` |
| `IX_Orders_CreatedAt` | dashboard doanh thu theo ngày |
| `IX_Orders_Status_CreatedAt` | tab trạng thái ở admin |
| `IX_Orders_PaymentStatus_CreatedAt` | doanh thu chỉ tính đơn `Paid` |
| `IX_Orders_User_CreatedAt` | "đơn hàng của tôi" |
| `IX_Orders_SenderEmail` / `_SenderPhone` / `_RecipientPhone` | tra cứu + chống dò đơn |
| `IX_Orders_DeliveryDate_Status` | đơn giao gấp trong ngày |

### 2.3 Còn lại

`OrderDetails (OrderId)`, `OrderDetails (ProductId)`, `OrderAddons (OrderId)`,
`UX_PaymentTransactions_Gateway_Txn (unique)`, `PaymentTransactions (OrderId)`,
`IX_Reviews_Product_Approved_Created`, `UX_Vouchers_Code`, `IX_Vouchers_Active_Dates`,
`UX_Users_Username`, `UX_Users_Email`, `IX_Users_Role_Active`,
`IX_PRT_TokenHash (unique)`, `IX_PRT_User_Created`, `IX_PRT_ExpiresAt`,
`IX_ProductImages_Product_Order`, `IX_AddonItems_Active`, `IX_GreetingCards_Active`,
`IX_Categories_Active_Order`, `IX_Occasions_Active_Order`, `UX_Categories_Slug`,
`UX_Occasions_Slug`, `UX_FlowerTypes_Slug`.

### 2.4 Kiểm tra index đã tồn tại

```sql
SELECT TABLE_NAME, INDEX_NAME, GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX) AS cols
FROM information_schema.STATISTICS
WHERE TABLE_SCHEMA = 'FlowerShopDb'
GROUP BY TABLE_NAME, INDEX_NAME
ORDER BY TABLE_NAME, INDEX_NAME;

-- Index không được dùng (ứng viên xoá)
SELECT * FROM sys.schema_unused_indexes WHERE object_schema = 'FlowerShopDb';
```

---

## 3. Cấu hình MySQL cho tải lớn (Laragon → `my.ini`)

```ini
innodb_buffer_pool_size            = 2G      # ~70% RAM nếu máy chỉ chạy MySQL
innodb_log_file_size               = 512M
innodb_log_buffer_size             = 64M
innodb_flush_log_at_trx_commit     = 2       # trade-off an toàn/hiệu năng
innodb_flush_method                = O_DIRECT_NO_FSYNC
innodb_io_capacity                 = 2000
innodb_io_capacity_max             = 4000
max_connections                    = 300
table_open_cache                   = 4000
thread_cache_size                  = 64
tmp_table_size                     = 64M
max_heap_table_size                = 64M

slow_query_log                     = 1
long_query_time                    = 1
log_queries_not_using_indexes      = 1
```

Sau khi sửa: restart MySQL trong Laragon.

> `innodb_buffer_pool_size` 2G chỉ phù hợp máy ≥ 4GB RAM. Máy 8GB chạy kèm VS 2022 nên để 1G.

---

## 4. Kiểm chứng bằng EXPLAIN

```sql
-- Trang danh mục + sắp xếp giá (phải dùng IX_Products_Category_Active_Price)
EXPLAIN SELECT * FROM Products
WHERE CategoryId = 1 AND IsActive = 1
ORDER BY Price LIMIT 12;

-- Dashboard doanh thu hôm nay (phải dùng IX_Orders_PaymentStatus_CreatedAt)
EXPLAIN SELECT SUM(TotalAmount) FROM Orders
WHERE PaymentStatus = 'Paid' AND CreatedAt >= CURDATE();

-- Tra cứu đơn (phải là const/eq_ref trên UX_Orders_OrderCode)
EXPLAIN SELECT * FROM Orders WHERE OrderCode = 'FL260101AB2CD';
```

Kỳ vọng: `type` = `ref`/`range`/`const`, `Extra` **không** chứa `Using filesort` với trang danh mục, `rows` nhỏ (không quét full table).

---

## 5. Bài test tải gợi ý

```powershell
# 1000 request, 50 kết nối đồng thời vào trang chủ + danh mục
# (cài: choco install hey  hoặc dùng k6)
hey -n 1000 -c 50 "http://localhost:PORT/"
hey -n 1000 -c 50 "http://localhost:PORT/Product?categoryId=1&sortBy=priceAsc&page=1"
```

Theo dõi:
- `SHOW GLOBAL STATUS LIKE 'Questions'` → số query/giây.
- `slow_query_log` → query > 1s.
- RAM tiến trình `w3wp` (không được tăng tuyến tính theo request → nếu tăng là do tracking entity).

---

## 6. Checklist khi thêm truy vấn mới

1. Chỉ đọc? → `AsNoTracking()`.
2. Có navigation property trong view? → `Include` tường minh (lazy loading đã tắt).
3. Trả về danh sách dài? → `Skip/Take` phía server, không phân trang ở client.
4. Có `WHERE`/`ORDER BY` trên cột mới? → thêm index vào `DbIndexProvisioner.Catalog` **và** file SQL dump, rồi chạy `DatabaseScriptTests`.
5. Có ghi hàng loạt? → dùng `ExecuteSqlCommand` như `DbContextAdapter`, tránh load entity rồi lưu từng cái.
6. Có `Contains()` tìm kiếm văn bản? → cân nhắc FULLTEXT thay `LIKE '%...%'`.
