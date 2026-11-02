# 🌸 Hữu Phước Florist - Website Bán Hoa Tươi Nghệ Thuật & Quà Tặng Sang Trọng
### Nền tảng: ASP.NET MVC 5 + C# + Entity Framework 6 + MySQL + VNPay + Stripe + AI Chatbot

---

## 🌟 Giới Thiệu Dự Án
**Hữu Phước Florist** là hệ thống website thương mại điện tử chuyên biệt cho ngành hoa tươi nghệ thuật và quà tặng cao cấp. Được thiết kế theo phong cách thẩm mỹ *Modern Minimalist Floral Boutique* (Sky Blue - xanh da trời sang trọng), kết hợp đầy đủ các quy trình nghiệp vụ đặc thù của tiệm hoa hiện đại:
- **Tách biệt người gửi & người nhận**: Hỗ trợ chế độ tặng ẩn danh bí mật (*Secret Admirer*).
- **Giao hoa hỏa tốc**: Lựa chọn ngày và khung giờ nhận hoa chính xác (khung 60-120 phút).
- **Duyệt ảnh thực tế trước khi giao**: Thợ cắm hoa chụp ảnh bó hoa hoàn thiện gửi khách duyệt trước khi shipper giao.
- **Tự cắm hoa theo ý thích (Interactive Bouquet Builder)**: Khách tự chọn phong cách cắm, loài hoa, số lượng cành (9, 19, 33, 99 cành) và giấy gói.
- **Đa cổng thanh toán**: Tích hợp thanh toán nội địa qua **VNPay Sandbox** (QR, ATM), quốc tế qua **Stripe Elements** (3D Secure), quét mã **VietQR** động và thanh toán tiền mặt (COD).
- **AI Florist Chatbot**: Trợ lý ảo tư vấn chọn mẫu hoa theo ngân sách/dịp tặng, tự động sáng tác lời chúc thiệp ý nghĩa và tra cứu vận đơn trực tiếp.

---

## 🛠️ Ngăn Xếp Công Nghệ (Tech Stack)
- **Framework**: ASP.NET MVC 5 (.NET Framework 4.8 / C#)
- **ORM**: Entity Framework 6 (Code-First & Database Initializer)
- **Cơ sở dữ liệu**: MySQL Server 5.7 / 8.0+
- **Cổng thanh toán**:
  - **VNPay** Gateway v2.1.0 (HMAC-SHA512 checksum & IPN webhook)
  - **Stripe** .NET SDK (Stripe Elements & PaymentIntent)
  - **VietQR** API (NAPAS 24/7 tự điền số tiền & nội dung đơn)
- **AI Chatbot**: Gemini API / Intelligent Floral Recommendation Engine
- **Giao diện**: HTML5, Modern CSS (Glassmorphism, CSS Variables), Vanilla JS, FontAwesome 6, SweetAlert2

---

## 📂 Cấu Trúc Dự Án (Project Structure)
```text
d:\phuoc-mat-lon/
├── FlowerShop.sln                 # File Solution Visual Studio 2019 / 2022
├── FlowerShop/
│   ├── FlowerShop.csproj          # Project file cấu hình MVC5, EF6, MySQL
│   ├── Web.config                 # Chuỗi kết nối MySQL, cấu hình VNPay, Stripe
│   ├── Global.asax / .cs          # Khởi động ứng dụng, nạp DbInitializer
│   ├── App_Data/
│   │   └── FlowerShop_Database_Dump.sql # File SQL sẵn sàng import 1-click
│   ├── App_Start/                 # RouteConfig, FilterConfig
│   ├── Areas/
│   │   └── Admin/                 # Khu vực quản trị cửa hàng
│   │       ├── Controllers/       # Dashboard, Order, Product, Voucher, Settings
│   │       └── Views/             # Quản lý đơn, in phiếu giao hoa ẩn giá, duyệt ảnh
│   ├── Controllers/               # Home, Product, CustomBouquet, Cart, Checkout, Payment, OrderTracking, Chatbot
│   ├── DAL/                       # FlowerShopDbContext, SeedData (25+ mẫu hoa)
│   ├── Models/                    # Entities (Product, Order, Addon...) & ViewModels
│   ├── Services/                  # VNPay, Stripe, VietQR, AI Chatbot, Cart
│   ├── Content/css/               # style.css (Storefront), admin.css, chatbot.css
│   ├── Scripts/js/                # main.js, cart.js, chatbot.js, custom-bouquet.js
│   └── Views/                     # Storefront Razor Views (Home, Product, Cart, Checkout, Tracking...)
└── packages/                      # Thư viện NuGet đã restore sẵn sàng
```

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Dự Án

### 1. Cơ sở dữ liệu MySQL
1. Mở công cụ quản lý MySQL của bạn (**phpMyAdmin**, **MySQL Workbench**, **HeidiSQL** hoặc **DBeaver**).
2. Tạo cơ sở dữ liệu `FlowerShopDb` và import file:
   👉 `FlowerShop/App_Data/FlowerShop_Database_Dump.sql`
3. Mở file `FlowerShop/Web.config` và kiểm tra lại chuỗi kết nối:
   ```xml
   <add name="FlowerShopConnectionString" 
        connectionString="server=localhost;port=3306;database=FlowerShopDb;uid=root;password=your_password;CharSet=utf8mb4;SslMode=none;" 
        providerName="MySql.Data.MySqlClient" />
   ```
*(Lưu ý: Hệ thống cũng có sẵn `FlowerShopDbInitializer` tự động sinh bảng và dữ liệu mẫu nếu kết nối với database trống).*

### 2. Mở & Chạy Dự Án Bằng Visual Studio
1. Nhấp đúp vào file `FlowerShop.sln` để mở trong **Visual Studio 2019 / 2022**.
2. Nhấn phím **F5** hoặc nút **IIS Express (Google Chrome)** trên thanh công cụ.
3. Trình duyệt sẽ tự động mở trang chủ website tại địa chỉ `http://localhost:...`.

### 3. Tài Khoản Quản Trị (Admin Portal)
- Đường dẫn quản trị: `http://localhost:.../Admin/Dashboard`
- Tài khoản mẫu: `admin`
- Mật khẩu: `Admin@123`

---

## 💳 Hướng Dẫn Kiểm Thử Thanh Toán (Payment Gateways)
1. **VNPay Sandbox**:
   - Chọn phương thức *Cổng VNPAY* ở bước thanh toán.
   - Tại trang VNPay Sandbox, chọn *Ngân hàng NCB*, nhập số thẻ test: `9704198526191432198`, tên `NGUYEN VAN A`, ngày phát hành `07/15`, OTP: `123456`.
2. **Stripe Thẻ Quốc Tế**:
   - Chọn *Cổng Quốc Tế Stripe*.
   - Nhập số thẻ test: `4242 4242 4242 4242`, ngày hết hạn bất kỳ trong tương lai, CVC `123`.
3. **VietQR**:
   - Chọn *Quét Mã VietQR*. Hệ thống tự động tạo mã QR chứa đúng số tiền và cú pháp `THANH TOAN FL...`.
