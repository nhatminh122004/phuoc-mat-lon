# 🌸 Hữu Phước Florist — Website Bán Hoa Tươi Nghệ Thuật

### ASP.NET MVC 5 (.NET Framework 4.8) + Entity Framework 6 + MySQL (Laragon) + **SePay**

---

## 1. Trạng thái hiện tại (cập nhật 16/11/2026)

| Hạng mục | Trạng thái |
|---|---|
| Build (`MSBuild FlowerShop.sln`) | ✅ 0 warning / 0 error |
| Razor precompile (`aspnet_compiler`) | ✅ 0 error — toàn bộ view đã compile |
| Unit test (`dotnet test`) | ✅ **226 test pass** |
| Cổng thanh toán | **SePay** (QR chuyển khoản / ATM-NAPAS / thẻ quốc tế) + **COD** |
| Seed dữ liệu | 24 sản phẩm, 48 ảnh, 5 addon, 4 thiệp, 3 voucher, 3 tài khoản, 2 đơn demo, 12 đánh giá |
| Chỉ mục DB | **43 index** cho tải truy vấn lớn (xem `Docs/PERFORMANCE.md`) |

> **Đã gỡ bỏ:** VNPay, Stripe, VietQR, PayOS và server Node mock (`server.js`). Cổng online duy nhất: **SePay**.

---

## 2. Ngăn xếp công nghệ

- **Framework**: ASP.NET MVC 5 (.NET Framework 4.8 / C# 7.3)
- **ORM**: Entity Framework 6.4.4 (Code-First + initializer idempotent)
- **CSDL**: MySQL 5.7 / 8.0 (Laragon)
- **Thanh toán**: SePay ([docs.sepay.vn](https://docs.sepay.vn)) — ký form HMAC-SHA256, IPN + webhook biến động số dư
- **Email**: SMTP (MailHog trên Laragon) hoặc ghi file `App_Data/emails` khi chưa cấu hình
- **Bảo mật**: BCrypt (work factor 11), token reset mật khẩu SHA-256 + hết hạn 30 phút, chống brute-force, `[ValidateAntiForgeryToken]` toàn bộ POST
- **Test**: xunit 2.9 trên `net48` (chạy trực tiếp trên assembly thật của app)

---

## 3. Cấu trúc dự án

```text
phuoc-mat-lon/
├── FlowerShop.sln
├── FlowerShop/
│   ├── App_Data/
│   │   └── FlowerShop_Database_Dump.sql   # 15 bảng + 43 index + seed đầy đủ
│   ├── Areas/Admin/
│   │   ├── Controllers/                   # Dashboard, Order, Product, Voucher, Review, Settings
│   │   ├── Filters/AdminAuthorizeAttribute.cs
│   │   └── Views/
│   ├── Controllers/                       # Home, Product, Cart, Checkout, Payment(SePay), OrderTracking, Account, Chatbot, CustomBouquet
│   ├── DAL/
│   │   ├── FlowerShopDbContext.cs         # lazy loading OFF, bỏ tracking cho truy vấn đọc
│   │   ├── FlowerShopDbInitializer.cs     # seed idempotent
│   │   ├── SeedCatalog.cs                 # 16 sản phẩm mở rộng + review + đơn demo
│   │   ├── DbIndexProvisioner.cs          # catalog 43 index + EnsureIndexes (idempotent)
│   │   └── DbContextAdapter.cs            # UPDATE kho/voucher nguyên tử
│   ├── Helpers/SlugHelper.cs
│   ├── Models/{Entities,ViewModels}
│   ├── Services/
│   │   ├── AI/            ChatbotService + ChatbotIntentDetector
│   │   ├── Cart/          CartService (Session)
│   │   ├── Email/         IEmailService, SmtpEmailService, FileEmailService, EmailTemplates
│   │   ├── Logging/       AppLogger
│   │   ├── Orders/        OrderCodeGenerator, StockService, VoucherUsageService,
│   │   │                  OrderNotificationService, BouquetBuilderService
│   │   ├── Pricing/       PricingService (nguồn sự thật duy nhất cho mọi phép tính tiền)
│   │   ├── Security/      PasswordService, PasswordResetService, PasswordResetTokenFactory, LoginThrottle
│   │   ├── Upload/        FileUploadService
│   │   └── SePay/         SePayOptions, SePaySignature, SePayPaymentService, SePayModels
│   ├── Scripts/js/        csrf.js, cart.js, auth.js, main.js, chatbot.js, custom-bouquet.js
│   └── Views/
└── FlowerShop.Tests/                      # 226 unit test (xunit, net48)
```

---

## 4. Cài đặt trên Laragon

Chi tiết từng bước: **[Docs/SETUP-LARAGON.md](Docs/SETUP-LARAGON.md)**

Tóm tắt:

1. Laragon → **Database → MySQL → Start**.
2. Mở HeidiSQL/phpMyAdmin, chạy **toàn bộ** `FlowerShop/App_Data/FlowerShop_Database_Dump.sql`.
3. Kiểm tra `FlowerShop/Web.config`:
   ```xml
   <add name="FlowerShopConnectionString"
        connectionString="server=localhost;port=3306;database=FlowerShopDb;uid=root;password=;CharSet=utf8mb4;SslMode=none;AllowUserVariables=True;"
        providerName="MySql.Data.MySqlClient" />
   ```
4. Mở `FlowerShop.sln` bằng Visual Studio 2022 → F5 (IIS Express).
5. (Tuỳ chọn) Laragon → MailHog để nhận email thật, rồi điền `Smtp:Host`/`Smtp:Port` trong Web.config hoặc trang **Admin → Cấu hình**.

### Tài khoản mặc định

| Vai trò | Tài khoản | Mật khẩu |
|---|---|---|
| Quản trị | `admin` | `Admin@123` |
| Khách | `customer` | `Customer@123` |
| Nghệ nhân | `florist` | `Florist@123` |

> Mật khẩu seed lưu dạng **SHA-256 hex**; lần đăng nhập đầu tiên hệ thống tự nâng cấp sang **BCrypt**.

---

## 5. Thanh toán

**SePay** — cấu hình ở **Admin → Cấu hình** hoặc `Web.config` (theo [docs.sepay.vn](https://docs.sepay.vn)):

| Chế độ | Mô tả | Cần khai báo |
|---|---|---|
| `gateway` (mặc định) | Chuyển khách sang trang thanh toán SePay (QR ngân hàng, ATM/NAPAS, thẻ quốc tế) | `MerchantId`, `SecretKey`, IPN URL `https://<host>/Payment/Ipn` |
| `qr` | Hiện QR `qr.sepay.vn` ngay trên site, tự xác nhận khi tiền vào | Ngân hàng + số TK, webhook `https://<host>/Payment/Webhook` |

Luồng xác nhận:
- `gateway` → SePay gọi **IPN** (`POST /Payment/Ipn`, header `X-Secret-Key`) → đơn chuyển `Paid` + `Confirmed`.
- `qr` → SePay gọi **webhook biến động số dư** (`POST /Payment/Webhook`) → khớp mã đơn trong `code`/`content` + đúng số tiền → `Paid`.
- Trang success_url **không** tự tin tưởng: chỉ hiện "thành công" khi DB đã `Paid`, còn lại là trang chờ + polling `/Payment/Status`.

**COD** — đơn tạo xong là chốt, không qua cổng thanh toán.

Quy tắc nghiệp vụ đã cố định:
- Chỉ `SEPAY` và `COD` là phương thức hợp lệ (có test chặn hồi quy).
- Voucher: COD trừ lượt ngay khi đặt, SePay trừ khi thanh toán thành công.
- Đơn thanh toán lỗi → `PaymentStatus = Failed`, khách có thể đặt lại.

---

## 6. Kiểm thử

```powershell
# Toàn bộ unit test (không cần MySQL)
dotnet test FlowerShop.Tests\FlowerShop.Tests.csproj

# Build + kiểm tra Razor toàn bộ view
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" FlowerShop.sln
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p FlowerShop -f $env:TEMP\fs-precompile
```

Phạm vi test: công thức tiền & voucher, mã đơn, tồn kho, băm mật khẩu, token reset, chống brute-force, chữ ký SePay (HMAC-SHA256 base64 vector), IPN/webhook parsing, slug tiếng Việt, intent chatbot, upload ảnh, email + template, logger, và **hợp đồng file SQL dump** (đủ bảng, đủ index, đủ 24 sản phẩm, không còn cổng thanh toán đã gỡ).

---

## 7. Hiệu năng

Xem **[Docs/PERFORMANCE.md](Docs/PERFORMANCE.md)** — 43 index, chính sách `AsNoTracking`, chống N+1, phân trang phía server, cache danh mục 5 phút, và thông số `my.ini` gợi ý cho Laragon.

---

## 8. Việc còn lại (chưa làm trong đợt này)

- CRUD danh mục / dịp / loại hoa / addon / thiệp trong Admin (hiện phải sửa DB).
- Quản lý người dùng & phân quyền Florist/Staff (Role đã có, UI chưa).
- Huỷ đơn phía khách + hoàn tiền tự động.
- Trang "Đơn hàng của tôi" / Profile / sổ địa chỉ.
- Form liên hệ thật (hiện chỉ hiện thông báo, không lưu DB, không gửi mail).
- SEO: meta description, Open Graph, JSON-LD, sitemap.
- Chatbot gọi LLM thật (hiện là rule-based có unit test).
