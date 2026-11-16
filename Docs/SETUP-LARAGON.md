# Cài đặt & chạy trên Laragon

Tài liệu này hướng dẫn dựng môi trường dev hoàn chỉnh (MySQL + IIS Express) trên Windows với Laragon.

---

## 1. Yêu cầu

| Thành phần | Phiên bản |
|---|---|
| Laragon | 5.x / 6.x (MySQL 8.0 hoặc MariaDB 10.6+) |
| .NET Framework | 4.8 (Developer Pack) |
| Visual Studio | 2022 (workload **ASP.NET and web development**) |
| .NET SDK | 6.0+ (chỉ cần cho `dotnet test`) |

---

## 2. Bật MySQL & tạo database

1. Mở Laragon → **Start All**.
2. Menu **Database** → chọn **HeidiSQL** (hoặc phpMyAdmin).
3. Tạo kết nối: host `localhost`, user `root`, password rỗng (mặc định Laragon).
4. Mở file **`FlowerShop/App_Data/FlowerShop_Database_Dump.sql`** → **Execute all**.

Script sẽ:
- Tạo DB `FlowerShopDb` (utf8mb4).
- DROP + CREATE **15 bảng**.
- Tạo **43 index** (bao gồm FULLTEXT tìm kiếm sản phẩm).
- Seed 5 danh mục, 5 dịp, 6 loại hoa, **24 sản phẩm**, 48 ảnh, 5 addon, 4 thiệp, 3 voucher, 3 tài khoản, 2 đơn demo, 12 đánh giá.
- Chạy `ANALYZE TABLE` và in bảng đếm số dòng để bạn xác nhận.

Kiểm tra nhanh:

```sql
SELECT COUNT(*) FROM Products;              -- phải = 24
SELECT COUNT(DISTINCT INDEX_NAME) FROM information_schema.STATISTICS
 WHERE TABLE_SCHEMA='FlowerShopDb';         -- phải >= 43
```

---

## 3. Cấu hình ứng dụng

Mở `FlowerShop/Web.config`:

```xml
<connectionStrings>
  <add name="FlowerShopConnectionString"
       connectionString="server=localhost;port=3306;database=FlowerShopDb;uid=root;password=;CharSet=utf8mb4;SslMode=none;AllowUserVariables=True;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

> `AllowUserVariables=True` cần cho một số câu lệnh raw SQL của `DbContextAdapter`.
> Nếu MySQL của bạn có mật khẩu, sửa `password=...`.

### 3.1 Email (tuỳ chọn nhưng nên bật)

Mặc định `Smtp:Host` để trống → mail được **ghi ra file** `FlowerShop/App_Data/emails/*.eml` (mở bằng Notepad để xem nội dung). Rất tiện khi dev.

Muốn gửi mail thật:
1. Laragon → **MailHog** (hoặc Mailpit) → Start.
2. Trong `Web.config`:
   ```xml
   <add key="Smtp:Host" value="localhost" />
   <add key="Smtp:Port" value="1025" />
   ```
3. Mở `http://localhost:8025` để xem hộp thư.

### 3.2 SePay (cổng thanh toán)

Đăng ký tài khoản tại [sepay.vn](https://sepay.vn), sau đó vào **Cổng thanh toán → Cấu hình**:

1. **Thông tin đơn vị** → lấy `Merchant ID` và `Secret Key` (`spsk_test_...` khi bật Test Mode).
2. **IPN** → nhập URL `https://<public-host>/Payment/Ipn` và tự đặt một `IPN Secret Key`.
3. (Chế độ `qr`) **Webhooks** → thêm webhook biến động số dư tới `https://<public-host>/Payment/Webhook`,
   chọn xác thực **HMAC-SHA256** (khuyến nghị) hoặc **API Key**.

Điền vào `Web.config` (hoặc trang **Admin → Cấu hình**):

```xml
<add key="SePay:Env" value="sandbox" />              <!-- sandbox | production -->
<add key="SePay:Mode" value="gateway" />             <!-- gateway | qr -->
<add key="SePay:MerchantId" value="MERCHANT_123" />
<add key="SePay:SecretKey" value="spsk_test_..." />
<add key="SePay:IpnSecretKey" value="chuoi-tu-dat" />
<add key="SePay:PaymentMethod" value="BANK_TRANSFER" /><!-- để trống = khách tự chọn -->
<add key="SePay:PublicBaseUrl" value="https://abc123.ngrok-free.app" />
```


**Chế độ `qr`** (hiện QR ngay trên site, không cần Approval cổng thanh toán):

```xml
<add key="SePay:Mode" value="qr" />
<add key="SePay:Bank" value="MBBank" />
<add key="SePay:AccountNumber" value="0123456789" />
<add key="SePay:AccountHolder" value="TIEM HOA HUU PHUOC" />
<add key="SePay:WebhookHmacSecret" value="hmac-secret-tu-dat" />
```

QR dùng `https://qr.sepay.vn/img?acc=...&bank=...&amount=...&des=<OrderCode>`; webhook đối soát theo `code`/`content`.

### 3.3 SePay cần URL công khai

IPN và webhook đều là **server-to-server**:

- SePay **không gọi được** `localhost` / `127.0.0.1` / IP nội bộ.
- Chạy dev: `ngrok http <cổng IIS Express>` rồi dán URL vào `SePay:PublicBaseUrl` và vào cấu hình IPN/webhook trên SePay.
- Endpoint phải trả HTTP **200** thì SePay mới coi là đã nhận (code hiện trả JSON `{ "success": true }`).

Không có ngrok? Vẫn test được chế độ `qr` bằng tay: mở **Admin → Đơn hàng → chi tiết** và đặt `PaymentStatus = Paid`,
hoặc gọi thử webhook từ máy local:

```powershell
$body = @{ id = 999999; gateway = "MBBank"; code = "FL260101AB2CD"; content = "CK FL260101AB2CD";
           transferType = "in"; transferAmount = 850000 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:PORT/Payment/Webhook" -Method Post -Body $body -ContentType "application/json"
```


## 4. Chạy ứng dụng

1. Mở `FlowerShop.sln` bằng Visual Studio 2022.
2. Chuột phải project **FlowerShop** → **Set as Startup Project**.
3. Nhấn **F5** (IIS Express).
4. Truy cập:
   - Storefront: `http://localhost:PORT/`
   - Admin: `http://localhost:PORT/Admin/Dashboard`

Đăng nhập `admin / Admin@123`.

---

## 5. Build & test bằng command line

```powershell
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# 1. Restore package (packages.config)
& $msbuild FlowerShop.sln /t:restore /p:RestorePackagesConfig=true

# 2. Build
& $msbuild FlowerShop.sln /p:Configuration=Debug

# 3. Unit test (183 test, không cần MySQL)
dotnet test FlowerShop.Tests\FlowerShop.Tests.csproj

# 4. Precompile toàn bộ Razor view (bắt lỗi cú pháp view)
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" `
    -v / -p (Resolve-Path FlowerShop).Path -f $env:TEMP\fs-precompile
```

---

## 6. Xử lý sự cố

| Hiện tượng | Nguyên nhân & cách sửa |
|---|---|
| `Unable to connect to any of the specified MySQL hosts` | MySQL chưa Start trong Laragon, hoặc sai `port`/`password` trong connection string |
| Tiếng Việt thành `B? Hoa H?ng` trong DB | **Import sai charset** (mất dữ liệu vĩnh viễn, không phải lỗi font). Chạy lại `.\scripts\db-import.ps1` — script đẩy **raw bytes** vào stdin + `--default-character-set=utf8mb4`. Nếu import bằng HeidiSQL: chọn encoding **UTF-8 (utf8mb4)** ở hộp thoại import, và chạy `SET NAMES utf8mb4;` trước |
| Tiếng Việt hiển thị `BÃ³ Hoa` (mojibake) | Dữ liệu đã bị double-encode → import lại từ file dump gốc (file dump là UTF-8 chuẩn) |
| Lỗi `Incorrect string value` khi lưu | DB/cột chưa `utf8mb4` → chạy lại script dump; kiểm tra `CharSet=utf8mb4` trong connection string |
| Script `.ps1` báo lỗi parse ở dòng có tiếng Việt | File `.ps1` phải lưu **UTF-8 có BOM** (PowerShell 5.1 đọc ANSI nếu thiếu BOM) |
| `The specified task executable location ... csc.exe is invalid` | csproj từng bị trỏ vào Microsoft.Net.Compilers.Toolset: đã sửa, đảm bảo `LangVersion=7.3` và không còn `<CscToolPath>` |
| `Could not load file or assembly Microsoft.Web.Infrastructure` | Thiếu binding redirect → đã thêm trong Web.config; nếu vẫn lỗi, copy `Microsoft.Web.Infrastructure.dll` vào `bin` |
| Trang Admin báo 500 do Razor | Chạy `aspnet_compiler` ở bước 5.4 để thấy lỗi cụ thể |
| Mail không gửi | `Smtp:Host` trống → kiểm tra `App_Data/emails`; có Host → kiểm tra MailHog đã Start |
| `PaymentStatus` không đổi sau khi thanh toán | `SePay:SecretKey`/`MerchantId` sai, `SePay:PublicBaseUrl` chưa đúng ngrok, hoặc IPN/webhook chưa khai báo trên SePay (xem log `App_Data/logs`) |

Log ứng dụng: `FlowerShop/App_Data/logs/app-yyyyMMdd.log`.
