const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');
const querystring = require('querystring');

const PORT = 5000;
const BASE_DIR = path.join(__dirname, 'FlowerShop');

// --- Mock In-Memory Database (Trùng khớp 100% SeedData trong C# EF6) ---
const categories = [
    { id: 1, name: "Bó Hoa Tươi", slug: "bo-hoa-tuoi", imageUrl: "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=600&q=80" },
    { id: 2, name: "Giỏ & Lẵng Hoa", slug: "gio-lang-hoa", imageUrl: "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=600&q=80" },
    { id: 3, name: "Bình Hoa Cao Cấp", slug: "binh-hoa-cao-cap", imageUrl: "https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=600&q=80" },
    { id: 4, name: "Hộp Hoa & Mica LED", slug: "hop-hoa-mica-led", imageUrl: "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=600&q=80" },
    { id: 5, name: "Kệ Hoa Khai Trương", slug: "ke-hoa-khai-truong", imageUrl: "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=600&q=80" }
];

const occasions = [
    { id: 1, name: "Hoa Sinh Nhật", slug: "sinh-nhat" },
    { id: 2, name: "Tình Yêu & Valentine", slug: "tinh-yeu" },
    { id: 3, name: "Khai Trương - Chúc Mừng", slug: "khai-truong" },
    { id: 4, name: "Kỷ Niệm Ngày Cưới", slug: "ky-niem" },
    { id: 5, name: "Tri Ân & Ngày Của Mẹ", slug: "tri-an-me" }
];

const products = [
    {
        id: 1,
        categoryId: 1,
        occasionId: 2,
        name: "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'",
        slug: "bo-hoa-hong-do-ecuador-red-romance",
        sku: "FL-ROM-01",
        shortDesc: "33 cành hồng đỏ Ecuador cỡ đại phối lá bạc Dollar bọc giấy lụa đen sang trọng viền vàng",
        fullDesc: "Bó hoa 'Red Romance' tuyển chọn 33 cành hồng đỏ Ecuador nhập khẩu cánh dày mịn như nhung. Kết hợp cùng lá bạc nhập khẩu và giấy gói lụa cao cấp, mang thông điệp 'Yêu em say đắm bằng cả con tim'.",
        careGuide: "Cắt xéo gốc 45 độ, thay nước sạch mỗi ngày, thêm gói dưỡng hoa FloraLife tặng kèm. Tránh ánh nắng gắt trực tiếp.",
        colorTone: "Đỏ",
        price: 950000,
        salePrice: 850000,
        mainImageUrl: "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 5.0,
        reviewCount: 28,
        isFlashSale: true,
        isBestSeller: true
    },
    {
        id: 2,
        categoryId: 1,
        occasionId: 1,
        name: "Bó Hoa Tulip Hồng 'Sweet Whispers'",
        slug: "bo-hoa-tulip-hong-sweet-whispers",
        sku: "FL-TUL-02",
        shortDesc: "20 cành Tulip hồng phấn Hà Lan tượng trưng cho tình cảm ngọt ngào, tinh tế",
        fullDesc: "Bó hoa Tulip 'Sweet Whispers' mang sắc hồng dịu dàng đầy mê hoặc từ củ giống Hà Lan tuyển chọn theo phong cách tối giản Hàn Quốc.",
        careGuide: "Tulip ưa nước đá lạnh. Thả vài viên đá lạnh vào bình mỗi sáng để giữ cành luôn cứng cáp.",
        colorTone: "Hồng",
        price: 1200000,
        salePrice: 1050000,
        mainImageUrl: "https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 4.9,
        reviewCount: 19,
        isFlashSale: false,
        isBestSeller: true
    },
    {
        id: 3,
        categoryId: 2,
        occasionId: 5,
        name: "Giỏ Hoa Mây 'Pastel Harmony'",
        slug: "gio-hoa-may-pastel-harmony",
        sku: "FL-PAS-03",
        shortDesc: "Giỏ hoa phối tone màu pastel thanh nhã gồm Cẩm Tú Cầu, Hồng Ohara và Cúc Tana",
        fullDesc: "Giỏ hoa nghệ thuật đan mây tự nhiên 'Pastel Harmony' phối màu pastel mềm mại, tạo cảm giác thư thái và an yên.",
        careGuide: "Xịt phun sương nhẹ lên cánh hoa cẩm tú cầu vào buổi sáng, châm 100ml nước vào xốp cắm hoa mỗi ngày.",
        colorTone: "Hồng",
        price: 780000,
        salePrice: 690000,
        mainImageUrl: "https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 4.8,
        reviewCount: 14,
        isFlashSale: false,
        isBestSeller: false
    },
    {
        id: 4,
        categoryId: 4,
        occasionId: 2,
        name: "Hộp Hoa Mica Trong Suốt 'Eternal Glow'",
        slug: "hop-hoa-mica-eternal-glow",
        sku: "FL-GLO-04",
        shortDesc: "Hộp mica nắp vòm trong suốt kết hợp hoa hồng Ecuador và dây đèn LED ấm áp",
        fullDesc: "Kiệt tác quà tặng hiện đại: Hoa hồng tuyển chọn nằm gọn trong hộp mica chống bụi cao cấp, bao quanh là ánh sáng đom đóm lung linh của chuỗi đèn LED vàng ấm.",
        careGuide: "Hoa được cắm vào mút dưỡng ẩm sâu. Đèn LED sử dụng pin cúc áo có thể bật/tắt tùy ý.",
        colorTone: "Đỏ",
        price: 1450000,
        salePrice: 1350000,
        mainImageUrl: "https://images.unsplash.com/photo-1508615070457-7baeba4003ab?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 5.0,
        reviewCount: 32,
        isFlashSale: true,
        isBestSeller: true
    },
    {
        id: 5,
        categoryId: 5,
        occasionId: 3,
        name: "Kệ Hoa Khai Trương 'Đại Phát Tài'",
        slug: "ke-hoa-khai-truong-dai-phat-tai",
        sku: "FL-KTR-05",
        shortDesc: "Kệ hoa 2 tầng phong cách hiện đại tone vàng cam rực rỡ với Hướng Dương & Hồng Cam Spirit",
        fullDesc: "Kệ hoa chúc mừng khai trương 'Đại Phát Tài' tạo tác từ hoa Hướng Dương đại đóa tượng trưng cho tiền tài nở rộ, cùng hoa Thiên Điểu vươn cao bay xa.",
        careGuide: "Tưới nước ẩm vào xốp cắm hoa 2 lần mỗi ngày để hoa giữ độ tươi đẹp từ 4-6 ngày.",
        colorTone: "Vàng",
        price: 1850000,
        salePrice: 1650000,
        mainImageUrl: "https://images.unsplash.com/photo-1507290439931-a861b5a38200?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 4.9,
        reviewCount: 22,
        isFlashSale: false,
        isBestSeller: true
    },
    {
        id: 6,
        categoryId: 3,
        occasionId: 4,
        name: "Bình Lan Hồ Điệp 'Vương Giả Quý Phái'",
        slug: "binh-lan-ho-diep-vuong-gia",
        sku: "FL-LAN-06",
        shortDesc: "Chậu sứ trắng 5 cành Lan Hồ Điệp tím hồng đột biến hoa to đều tăm tắp",
        fullDesc: "Chậu Lan Hồ Điệp hoàng gia 5 cành uốn lượn phong thủy, gốc phủ rêu xanh tự nhiên và cành lũa mộc mạc.",
        careGuide: "Tưới gốc mỗi tuần 1 lần lượng nước vừa phải (khoảng 150ml). Đặt nơi thoáng mát.",
        colorTone: "Tím",
        price: 2400000,
        salePrice: 2200000,
        mainImageUrl: "https://images.unsplash.com/photo-1582794543139-8ac9cb0f7b11?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 5.0,
        reviewCount: 11,
        isFlashSale: false,
        isBestSeller: false
    },
    {
        id: 7,
        categoryId: 1,
        occasionId: 2,
        name: "Bó Hoa Baby Trắng Khổng Lồ 'Pure Angel'",
        slug: "bo-hoa-baby-trang-pure-angel",
        sku: "FL-BAB-07",
        shortDesc: "Bó hoa Baby Hà Lan trắng tinh khôi bồng bềnh tựa mây trời bọc giấy kraft vintage",
        fullDesc: "Bó hoa Baby trắng 'Pure Angel' kích thước khổng lồ tựa như một đám mây tuyết trắng ngần bồng bềnh. Hoa Baby có thể phơi khô chơi được trên 1 năm.",
        careGuide: "Sau khi chưng tươi 1 tuần, có thể treo ngược nơi khô ráo thoáng gió để hoa khô tự nhiên.",
        colorTone: "Trắng",
        price: 650000,
        salePrice: 580000,
        mainImageUrl: "https://images.unsplash.com/photo-1561181286-d3fee7d55364?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 4.9,
        reviewCount: 45,
        isFlashSale: false,
        isBestSeller: true
    },
    {
        id: 8,
        categoryId: 1,
        occasionId: 1,
        name: "Bó Hoa Hồng Kem Dâu 'Strawberry Milk'",
        slug: "bo-hoa-hong-kem-dau-strawberry-milk",
        sku: "FL-MILK-08",
        shortDesc: "Bó hoa phối tone hồng kem dâu ngọt ngào với Hồng Juliet và Cúc Pingpong",
        fullDesc: "Sự pha trộn màu sắc pastel đáng yêu ngọt ngào tựa ly trà sữa dâu tây. Rất thích hợp làm quà tặng sinh nhật bạn gái.",
        careGuide: "Cắt gốc chéo, chưng bình nước sạch, đặt nơi mát mẻ.",
        colorTone: "Hồng",
        price: 520000,
        salePrice: 480000,
        mainImageUrl: "https://images.unsplash.com/photo-1563245372-f21724e3856d?auto=format&fit=crop&w=800&q=80",
        ratingAvg: 4.8,
        reviewCount: 17,
        isFlashSale: false,
        isBestSeller: false
    }
];

const addons = [
    { id: 1, name: "Gấu Bông Teddy Nơ Lụa (35cm)", price: 180000, imageUrl: "https://images.unsplash.com/photo-1559454403-b8fb88521f11?auto=format&fit=crop&w=400&q=80" },
    { id: 2, name: "Hộp Chocolate Ferrero Rocher (16 viên)", price: 220000, imageUrl: "https://images.unsplash.com/photo-1549007994-cb92caebd54b?auto=format&fit=crop&w=400&q=80" },
    { id: 3, name: "Nến Thơm Tinh Dầu Lavender & Hoa Nhài", price: 190000, imageUrl: "https://images.unsplash.com/photo-1603006905003-be475563bc59?auto=format&fit=crop&w=400&q=80" },
    { id: 4, name: "Bóng Bay Khí Heli Trái Tim Rực Rỡ", price: 90000, imageUrl: "https://images.unsplash.com/photo-1530103862676-de8c9debad1d?auto=format&fit=crop&w=400&q=80" }
];

// Sample Orders
const orders = [
    {
        id: 1,
        orderCode: "FL202609088831",
        senderName: "Nguyễn Hoàng Nam",
        senderPhone: "0908123456",
        recipientName: "Trần Mai Anh",
        recipientPhone: "0912345678",
        recipientAddress: "Tòa nhà Landmark 81, 720A Điện Biên Phủ, Phường 22",
        district: "Bình Thạnh",
        city: "Hồ Chí Minh",
        deliveryDate: "2026-09-09",
        deliveryTimeSlot: "10:00 - 12:00",
        isAnonymous: false,
        cardMessage: "Chúc em tuổi mới luôn rực rỡ và hạnh phúc như những đóa hoa này!",
        totalAmount: 850000,
        paymentMethod: "VNPAY",
        paymentStatus: "Paid",
        orderStatus: "ProofReady",
        proofPhotoUrl: "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80",
        items: [{ name: "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'", qty: 1, price: 850000 }]
    }
];

// Global Cart
let cart = {
    items: [
        { productId: 1, name: "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'", slug: "bo-hoa-hong-do-ecuador-red-romance", price: 850000, qty: 1, image: "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80" }
    ],
    addons: [],
    voucher: null
};

function formatMoney(num) {
    return new Intl.NumberFormat('vi-VN').format(num) + ' đ';
}

function getCartTotals() {
    const subTotal = cart.items.reduce((s, i) => s + (i.price * i.qty), 0);
    const addonTotal = cart.addons.reduce((s, a) => s + (a.price * a.qty), 0);
    let discount = 0;
    if (cart.voucher === 'WELCOME50' && (subTotal + addonTotal >= 400000)) discount = 50000;
    if (cart.voucher === 'BLOOM10' && (subTotal + addonTotal >= 500000)) discount = Math.min(100000, (subTotal + addonTotal) * 0.1);
    const shippingFee = (subTotal + addonTotal >= 500000) ? 0 : 35000;
    const total = Math.max(0, subTotal + addonTotal - discount + shippingFee);
    const count = cart.items.reduce((s, i) => s + i.qty, 0);
    return { subTotal, addonTotal, discount, shippingFee, total, count };
}

// Master Layout HTML Template
function renderLayout(title, content, activeNav = 'home') {
    const totals = getCartTotals();
    return `<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>${title} - Bloom & Grace</title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,600;0,700;1,400&family=Plus+Jakarta+Sans:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link rel="stylesheet" href="/Content/css/style.css" />
    <link rel="stylesheet" href="/Content/css/chatbot.css" />
    <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
</head>
<body>
    <div class="announcement-bar">
        <span><i class="fa-solid fa-truck-fast"></i> Giao hoa hỏa tốc 60 - 120 phút nội thành TP.HCM & Hà Nội</span>
        <span>•</span>
        <span><i class="fa-solid fa-gift"></i> Tặng kèm thiệp hoa nghệ thuật & banner cao cấp mọi đơn hàng</span>
        <span>•</span>
        <span><i class="fa-solid fa-phone"></i> Hotline 24/7: <strong>1900 6868</strong></span>
    </div>

    <header class="main-header">
        <div class="header-container">
            <a href="/" class="brand-logo">
                <i class="fa-solid fa-spa logo-icon"></i>
                <div class="logo-text">Bloom<span>&Grace</span></div>
            </a>
            <div class="header-search">
                <i class="fa-solid fa-magnifying-glass search-icon"></i>
                <input type="text" id="headerSearchInput" placeholder="Tìm hoa hồng, tulip, hoa sinh nhật..." autocomplete="off" />
                <div class="search-results-dropdown" id="searchResultsDropdown"></div>
            </div>
            <ul class="nav-menu">
                <li><a href="/" class="${activeNav === 'home' ? 'active' : ''}">Trang Chủ</a></li>
                <li><a href="/Product" class="${activeNav === 'product' ? 'active' : ''}">Bộ Sưu Tập Hoa</a></li>
                <li><a href="/tu-thiet-ke-bo-hoa" class="${activeNav === 'custom' ? 'active' : ''}"><i class="fa-solid fa-wand-magic-sparkles" style="color: var(--primary); margin-right: 4px;"></i>Tự Cắm Hoa</a></li>
                <li><a href="/tra-cuu-don-hang" class="${activeNav === 'tracking' ? 'active' : ''}">Tra Cứu Đơn</a></li>
                <li><a href="/Home/About" class="${activeNav === 'about' ? 'active' : ''}">Về Chúng Tôi</a></li>
            </ul>
            <div class="header-actions">
                <a href="/tra-cuu-don-hang" class="btn-action-icon" title="Tra cứu tiến độ đơn hàng">
                    <i class="fa-solid fa-truck-ramp-box"></i>
                </a>
                <a href="/Cart" class="btn-action-icon js-cart-toggle" title="Giỏ hoa của bạn">
                    <i class="fa-solid fa-basket-shopping"></i>
                    <span class="badge-count js-cart-count">${totals.count}</span>
                </a>
                <a href="/Admin/Dashboard" class="btn btn-secondary btn-sm" style="margin-left: 8px;">
                    <i class="fa-solid fa-lock" style="font-size: 0.8rem;"></i> Admin
                </a>
            </div>
        </div>
    </header>

    <main>
        ${content}
    </main>

    <!-- Off-Canvas Mini Cart Drawer -->
    <div class="cart-drawer-overlay" id="cartOverlay"></div>
    <div class="cart-drawer" id="cartDrawer">
        <div id="miniCartBody">
            <div class="cart-drawer-header">
                <h3 class="cart-drawer-title font-serif">Giỏ Hoa Của Bạn (<span class="js-cart-count">${totals.count}</span>)</h3>
                <button type="button" class="btn-close-drawer" id="cartCloseBtn"><i class="fa-solid fa-xmark"></i></button>
            </div>
            <div class="cart-drawer-body">
                <div class="cart-free-shipping-progress">
                    ${totals.subTotal + totals.addonTotal >= 500000 
                        ? `<div style="color: #2e7d32; font-weight: 600;"><i class="fa-solid fa-circle-check"></i> Đã được <strong>Miễn phí giao hoa</strong>!</div>`
                        : `<div>Mua thêm <strong>${formatMoney(500000 - (totals.subTotal + totals.addonTotal))}</strong> để được <strong>Free Ship</strong>!</div>`
                    }
                </div>
                <div style="display: flex; flex-direction: column; gap: 14px;">
                    ${cart.items.map(item => `
                        <div style="display: flex; gap: 12px; padding-bottom: 14px; border-bottom: 1px solid #f2e6e8; align-items: center;">
                            <img src="${item.image}" style="width: 60px; height: 60px; border-radius: 8px; object-fit: cover;" />
                            <div style="flex: 1;">
                                <div style="font-weight: 600; font-size: 0.9rem; color: #2e2426;">${item.name}</div>
                                <div style="color: #c84b61; font-weight: 700; font-size: 0.85rem; margin-top: 4px;">${formatMoney(item.price)} x ${item.qty}</div>
                            </div>
                            <form action="/Cart/RemoveItem" method="post" style="margin: 0;">
                                <input type="hidden" name="productId" value="${item.productId}" />
                                <button type="submit" style="background: none; border: none; color: #b5a9ab; cursor: pointer;"><i class="fa-regular fa-trash-can"></i></button>
                            </form>
                        </div>
                    `).join('')}
                </div>
            </div>
            <div class="cart-drawer-footer">
                <div style="display: flex; justify-content: space-between; margin-bottom: 14px; font-size: 1.15rem; font-weight: 700; color: #c84b61;">
                    <span>Tổng cộng:</span>
                    <span>${formatMoney(totals.total)}</span>
                </div>
                <div style="display: flex; gap: 10px;">
                    <a href="/Cart" class="btn btn-secondary" style="flex: 1; padding: 12px;">Xem Giỏ</a>
                    <a href="/Checkout" class="btn btn-primary" style="flex: 1; padding: 12px;">Đặt Hàng</a>
                </div>
            </div>
        </div>
    </div>

    <!-- AI Chatbot Widget -->
    <div class="chatbot-widget-container">
        <div class="chatbot-trigger-btn" id="chatbotTriggerBtn" title="Tư vấn chọn hoa cùng AI Florist">
            <i class="fa-solid fa-wand-magic-sparkles"></i>
            <div class="chatbot-badge-pulse"></div>
        </div>
        <div class="chatbot-window" id="chatbotWindow">
            <div class="chatbot-header">
                <div class="chatbot-florist-info">
                    <img src="https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=150&q=80" class="chatbot-avatar" />
                    <div>
                        <h4 class="chatbot-title">Hương Lan • Florist AI</h4>
                        <div class="chatbot-status"><span class="status-dot"></span> Sẵn sàng tư vấn hoa & quà</div>
                    </div>
                </div>
                <button type="button" class="btn-close-chatbot" id="chatbotCloseBtn"><i class="fa-solid fa-xmark"></i></button>
            </div>
            <div class="chatbot-body" id="chatbotBody">
                <div class="chat-bubble chat-bubble-bot">
                    Dạ chào bạn! Em là <strong>Hương Lan</strong> - Trợ lý Florist AI của tiệm hoa 🌸<br/><br/>
                    Em có thể giúp bạn chọn mẫu hoa phù hợp ngân sách, gợi ý lời chúc thiệp ý nghĩa, hoặc tra cứu đơn hàng giúp bạn ạ!
                </div>
            </div>
            <div class="chatbot-quick-chips">
                <button type="button" class="chip-btn js-chat-chip" data-prompt="Tư vấn hoa sinh nhật bạn gái dưới 600k"><i class="fa-solid fa-cake-candles"></i> Sinh nhật bạn gái</button>
                <button type="button" class="chip-btn js-chat-chip" data-prompt="Gợi ý mẫu hoa tặng Mẹ ý nghĩa"><i class="fa-solid fa-heart"></i> Hoa tặng Mẹ</button>
                <button type="button" class="chip-btn js-chat-chip" data-prompt="Gợi ý lời chúc thiệp tình yêu lãng mạn"><i class="fa-solid fa-pen-nib"></i> Viết lời chúc thiệp</button>
                <button type="button" class="chip-btn js-chat-chip" data-prompt="Tra cứu đơn hàng FL202609088831"><i class="fa-solid fa-truck-fast"></i> Tra cứu đơn hàng</button>
            </div>
            <div class="chatbot-footer">
                <input type="text" id="chatbotInput" class="chatbot-input" placeholder="Hỏi mẫu hoa, ngân sách, dịp tặng..." />
                <button type="button" id="chatbotSendBtn" class="chatbot-send-btn"><i class="fa-solid fa-paper-plane"></i></button>
            </div>
        </div>
    </div>

    <!-- Footer -->
    <footer class="main-footer">
        <div class="footer-grid">
            <div class="footer-col">
                <div class="brand-logo" style="margin-bottom: 20px;">
                    <i class="fa-solid fa-spa logo-icon" style="color: var(--primary);"></i>
                    <div class="logo-text" style="color: #fff;">Bloom<span style="color: var(--primary);">Grace</span></div>
                </div>
                <p style="margin-bottom: 20px; line-height: 1.7;">Boutique hoa tươi nghệ thuật & quà tặng sang trọng hàng đầu. Chuyển tải trọn vẹn yêu thương đến người nhận.</p>
                <div style="display: flex; gap: 10px;">
                    <span style="background: #fff; border-radius: 4px; padding: 4px 8px; font-size: 0.75rem; font-weight: 700; color: #005baa;">VNPAY</span>
                    <span style="background: #fff; border-radius: 4px; padding: 4px 8px; font-size: 0.75rem; font-weight: 700; color: #635bff;">STRIPE</span>
                    <span style="background: #fff; border-radius: 4px; padding: 4px 8px; font-size: 0.75rem; font-weight: 700; color: #005030;">VIETQR</span>
                </div>
            </div>
            <div class="footer-col">
                <h4>Chủ Đề Hoa Tươi</h4>
                <ul>
                    <li><a href="/Product?occasionId=1">Hoa Sinh Nhật Sang Trọng</a></li>
                    <li><a href="/Product?occasionId=2">Hoa Tình Yêu & Valentine</a></li>
                    <li><a href="/Product?occasionId=3">Hoa Mừng Khai Trương</a></li>
                </ul>
            </div>
            <div class="footer-col">
                <h4>Hỗ Trợ Khách Hàng</h4>
                <ul>
                    <li><a href="/tra-cuu-don-hang">Tra Cứu Vận Đơn</a></li>
                    <li><a href="/tu-thiet-ke-bo-hoa">Tự Thiết Kế Bó Hoa</a></li>
                    <li><a href="/Home/About">Chính Sách Hoa Tươi 100%</a></li>
                </ul>
            </div>
            <div class="footer-col">
                <h4>Showroom Trực Tiếp</h4>
                <div class="footer-contact-item"><i class="fa-solid fa-location-dot"></i> 128 Đường Hoa Hồng, Quận 1, TP.HCM</div>
                <div class="footer-contact-item"><i class="fa-solid fa-phone"></i> 1900 6868 - 0909 123 456</div>
            </div>
        </div>
        <div class="footer-bottom">
            <div>© ${new Date().getFullYear()} Bloom & Grace Boutique. ASP.NET MVC 5 + MySQL + VNPay + Stripe + AI Chatbot.</div>
        </div>
    </footer>

    <script src="/Scripts/js/main.js"></script>
    <script src="/Scripts/js/chatbot.js"></script>
</body>
</html>`;
}

// Request Handler
const server = http.createServer((req, res) => {
    const parsedUrl = url.parse(req.url, true);
    const pathname = parsedUrl.pathname;

    // 1. Phục vụ Static Files (CSS, JS, SQL, images)
    if (pathname.startsWith('/Content/') || pathname.startsWith('/Scripts/') || pathname.startsWith('/App_Data/')) {
        const filePath = path.join(BASE_DIR, pathname);
        if (fs.existsSync(filePath)) {
            let contentType = 'text/plain';
            if (filePath.endsWith('.css')) contentType = 'text/css';
            if (filePath.endsWith('.js')) contentType = 'application/javascript';
            if (filePath.endsWith('.png')) contentType = 'image/png';
            if (filePath.endsWith('.jpg') || filePath.endsWith('.jpeg')) contentType = 'image/jpeg';
            if (filePath.endsWith('.sql')) contentType = 'text/plain; charset=utf-8';

            res.writeHead(200, { 'Content-Type': contentType });
            fs.createReadStream(filePath).pipe(res);
            return;
        }
    }

    // 2. API Endpoint: Chatbot
    if (pathname === '/Chatbot/SendMessage' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            let msg = '';
            try { msg = JSON.parse(body).message || ''; } catch(e) { msg = body; }
            msg = msg.toLowerCase();

            let reply = "Dạ tiệm hoa có thể giúp gì cho bạn ạ?";
            let suggestedProducts = [];
            let orderSummary = null;

            if (msg.includes('fl202609088831') || msg.includes('đơn hàng') || msg.includes('tra cứu')) {
                const o = orders[0];
                reply = `Dạ em đã tìm thấy đơn hàng **${o.orderCode}** của bạn!\n📍 **Người nhận:** ${o.recipientName} - ${o.recipientAddress}\n⏰ **Khung giờ giao:** ${o.deliveryDate} (${o.deliveryTimeSlot})\n🌸 **Trạng thái:** Đã cắm xong - Chờ khách duyệt ảnh\n💰 **Tổng tiền:** ${formatMoney(o.totalAmount)} (${o.paymentStatus})\n📸 Ảnh hoa thực tế đã được nghệ nhân tải lên, bạn xem bên dưới nhé!`;
                orderSummary = {
                    OrderCode: o.orderCode,
                    StatusName: "Đã Cắm Xong - Chờ Khách Duyệt Ảnh",
                    DeliveryTime: `${o.deliveryDate} (${o.deliveryTimeSlot})`,
                    ProofPhotoUrl: o.proofPhotoUrl
                };
            } else if (msg.includes('lời chúc') || msg.includes('thiệp') || msg.includes('chúc mừng')) {
                reply = `💌 **Gợi ý lời chúc sinh nhật / tình yêu ý nghĩa:**\n\n1. *'Chúc em tuổi mới luôn rực rỡ, xinh đẹp và ngập tràn niềm vui. Cảm ơn em đã bước vào cuộc đời anh!'*\n\n2. *'Mỗi cánh hoa thay cho một lời yêu thương chân thành nhất anh dành tặng em. Happy Birthday my love!'*`;
            } else if (msg.includes('sinh nhật') || msg.includes('bạn gái') || msg.includes('người yêu')) {
                reply = `Dạ tặng sinh nhật bạn gái thì hoa Hồng Ecuador đỏ nhung hoặc Tulip hồng là tuyệt vời nhất, tượng trưng cho tình yêu ngọt ngào và sự nâng niu:`;
                suggestedProducts = [
                    { Id: 1, Name: "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'", ImageUrl: products[0].mainImageUrl, Price: products[0].salePrice, Slug: products[0].slug },
                    { Id: 2, Name: "Bó Hoa Tulip Hồng 'Sweet Whispers'", ImageUrl: products[1].mainImageUrl, Price: products[1].salePrice, Slug: products[1].slug }
                ];
            } else if (msg.includes('mẹ') || msg.includes('tri ân')) {
                reply = `Dạ tặng Mẹ, tiệm có mẫu Giỏ hoa Pastel Harmony thanh nhã với Cẩm Tú Cầu và Hồng Ohara thể hiện trọn vẹn lòng biết ơn sâu sắc:`;
                suggestedProducts = [
                    { Id: 3, Name: "Giỏ Hoa Mây 'Pastel Harmony'", ImageUrl: products[2].mainImageUrl, Price: products[2].salePrice, Slug: products[2].slug }
                ];
            } else {
                reply = `Dạ em gửi bạn tham khảo các mẫu hoa tươi nghệ thuật bán chạy nhất hôm nay của tiệm:`;
                suggestedProducts = [
                    { Id: 1, Name: products[0].name, ImageUrl: products[0].mainImageUrl, Price: products[0].salePrice, Slug: products[0].slug },
                    { Id: 4, Name: products[3].name, ImageUrl: products[3].mainImageUrl, Price: products[3].salePrice, Slug: products[3].slug }
                ];
            }

            res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
            res.end(JSON.stringify({ success: true, reply, products: suggestedProducts, orderSummary }));
        });
        return;
    }

    // 3. API Endpoint: Live Search AJAX
    if (pathname === '/Product/SearchAjax') {
        const q = (parsedUrl.query.q || '').toLowerCase();
        const results = products.filter(p => p.name.toLowerCase().includes(q) || p.shortDesc.toLowerCase().includes(q)).slice(0, 5).map(p => ({
            id: p.id,
            name: p.name,
            slug: p.slug,
            price: p.salePrice || p.price,
            image: p.mainImageUrl
        }));
        res.writeHead(200, { 'Content-Type': 'application/json; charset=utf-8' });
        res.end(JSON.stringify({ success: true, items: results }));
        return;
    }

    // 4. Cart Actions
    if (pathname === '/Cart/AddToCart' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            const params = querystring.parse(body);
            const pId = parseInt(params.productId, 10);
            const qty = parseInt(params.quantity || 1, 10);
            const pr = products.find(p => p.id === pId);
            if (pr) {
                const existing = cart.items.find(i => i.productId === pId);
                if (existing) existing.qty += qty;
                else cart.items.push({ productId: pr.id, name: pr.name, slug: pr.slug, price: pr.salePrice || pr.price, qty: qty, image: pr.mainImageUrl });
            }
            if (params.isAjax === 'true') {
                const totals = getCartTotals();
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(JSON.stringify({ success: true, totalItems: totals.count, subTotal: formatMoney(totals.subTotal) }));
            } else {
                res.writeHead(302, { Location: '/Cart' });
                res.end();
            }
        });
        return;
    }

    if (pathname === '/Cart/RemoveItem' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            const params = querystring.parse(body);
            const pId = parseInt(params.productId, 10);
            cart.items = cart.items.filter(i => i.productId !== pId);
            res.writeHead(302, { Location: '/Cart' });
            res.end();
        });
        return;
    }

    if (pathname === '/Cart/ApplyVoucher' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            const params = querystring.parse(body);
            const code = (params.code || '').trim().toUpperCase();
            if (code === 'WELCOME50' || code === 'BLOOM10') {
                cart.voucher = code;
            }
            res.writeHead(302, { Location: '/Cart' });
            res.end();
        });
        return;
    }

    // 5. Checkout Place Order
    if (pathname === '/Checkout/PlaceOrder' && req.method === 'POST') {
        let body = '';
        req.on('data', chunk => body += chunk);
        req.on('end', () => {
            const params = querystring.parse(body);
            const newCode = `FL20260908${Math.floor(1000 + Math.random() * 9000)}`;
            const totals = getCartTotals();
            const newOrder = {
                id: orders.length + 1,
                orderCode: newCode,
                senderName: params.SenderName || "Khách Hàng",
                senderPhone: params.SenderPhone || "0900000000",
                recipientName: params.RecipientName || "Người Nhận",
                recipientPhone: params.RecipientPhone || "0911111111",
                recipientAddress: params.RecipientAddress || "Quận 1, TP.HCM",
                district: params.District || "Quận 1",
                city: params.City || "TP.HCM",
                deliveryDate: params.DeliveryDate || "2026-09-10",
                deliveryTimeSlot: params.DeliveryTimeSlot || "08:00 - 10:00",
                isAnonymous: params.IsAnonymous === 'true',
                cardMessage: params.CardMessage || "",
                totalAmount: totals.total,
                paymentMethod: params.PaymentMethod || "COD",
                paymentStatus: (params.PaymentMethod === 'COD' ? 'Pending' : 'Paid'),
                orderStatus: "Confirmed",
                proofPhotoUrl: "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80",
                items: [...cart.items]
            };
            orders.unshift(newOrder);
            cart.items = []; // Xóa giỏ

            if (params.PaymentMethod === 'VIETQR') {
                res.writeHead(302, { Location: `/Payment/VietQrPay?orderCode=${newCode}` });
            } else if (params.PaymentMethod === 'STRIPE') {
                res.writeHead(302, { Location: `/Payment/StripeCheckout?orderCode=${newCode}` });
            } else {
                res.writeHead(302, { Location: `/Payment/Success?orderCode=${newCode}` });
            }
            res.end();
        });
        return;
    }

    // 6. Routes Pages

    // HOME PAGE
    if (pathname === '/' || pathname === '/Home' || pathname === '/Home/Index') {
        const flashSales = products.filter(p => p.isFlashSale);
        const bestSellers = products.filter(p => p.isBestSeller);
        const html = `
            <section class="hero-section">
                <div class="hero-container">
                    <div class="hero-content">
                        <div class="hero-tag"><i class="fa-solid fa-sparkles"></i> Mùa Hoa Mới • Tuyển Chọn Tinh Tế</div>
                        <h1 class="hero-title">Trao Gửi Cảm Xúc Qua Từng <em>Cánh Hoa Nghệ Thuật</em></h1>
                        <p class="hero-desc">Tuyển chọn những đóa hoa tươi nhập khẩu đẹp nhất thế giới. Nghệ nhân hoa thiết kế riêng cho từng khoảnh khắc đáng nhớ của bạn.</p>
                        <div class="hero-actions">
                            <a href="/Product" class="btn btn-primary">Khám Phá Bộ Sưu Tập <i class="fa-solid fa-arrow-right-long"></i></a>
                            <a href="/tu-thiet-ke-bo-hoa" class="btn btn-secondary"><i class="fa-solid fa-wand-magic-sparkles" style="color: var(--primary);"></i> Tự Thiết Kế Bó Hoa</a>
                        </div>
                        <div class="hero-badges">
                            <div class="hero-badge-item"><i class="fa-solid fa-clock-rotate-left"></i><span>Giao Nhanh 60 Phút</span></div>
                            <div class="hero-badge-item"><i class="fa-solid fa-camera"></i><span>Chụp Ảnh Duyệt Trước</span></div>
                            <div class="hero-badge-item"><i class="fa-solid fa-shield-heart"></i><span>Cam Kết Hoa Tươi 3 Ngày</span></div>
                        </div>
                    </div>
                    <div class="hero-visual">
                        <img src="https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=800&q=80" class="hero-img-main" />
                        <div class="hero-floating-card">
                            <div class="avatar"><i class="fa-solid fa-heart"></i></div>
                            <div>
                                <div style="font-size: 0.82rem; color: #888; text-transform: uppercase; font-weight: 600;">Khách Hàng Hài Lòng</div>
                                <div style="font-weight: 700; font-size: 1.1rem; color: #2e2426;">+15.000 Đơn Hoa Hạnh Phúc</div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <section class="section" style="padding-top: 50px;">
                <div class="flash-sale-box">
                    <div class="flash-sale-info">
                        <span style="background: #ff4757; color: #fff; padding: 4px 12px; border-radius: 20px; font-size: 0.78rem; font-weight: 700;">Ưu Đãi Khung Giờ Vàng</span>
                        <h3 class="font-serif" style="margin-top: 10px;">Flash Sale Hoa Tươi Trong Ngày</h3>
                        <p>Giảm giá đến 25% cho các mẫu hoa thiết kế giới hạn hôm nay. Miễn phí giao hàng hỏa tốc.</p>
                    </div>
                    <div class="flash-countdown">
                        <div class="countdown-unit"><span class="countdown-num" id="cdHours">06</span><span class="countdown-label">Giờ</span></div>
                        <div class="countdown-unit"><span class="countdown-num" id="cdMins">42</span><span class="countdown-label">Phút</span></div>
                        <div class="countdown-unit"><span class="countdown-num" id="cdSecs">15</span><span class="countdown-label">Giây</span></div>
                    </div>
                </div>

                <div class="products-grid">
                    ${flashSales.map(p => `
                        <div class="product-card">
                            <div class="product-badges"><span class="badge-tag badge-hot"><i class="fa-solid fa-bolt"></i> Flash Sale</span></div>
                            <div class="product-thumb">
                                <img src="${p.mainImageUrl}" />
                                <div class="product-quick-actions">
                                    <button type="button" class="btn-quick-add js-quick-add" data-id="${p.id}"><i class="fa-solid fa-bag-shopping"></i> Thêm Nhanh</button>
                                </div>
                            </div>
                            <div class="product-info">
                                <div class="product-occasion">Hoa Nghệ Thuật</div>
                                <h3 class="product-title font-serif"><a href="/hoa/${p.slug}">${p.name}</a></h3>
                                <div class="product-rating"><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i> <span>(${p.reviewCount})</span></div>
                                <div class="product-price-box">
                                    <span class="product-price">${formatMoney(p.salePrice || p.price)}</span>
                                    ${p.salePrice ? `<span class="product-old-price">${formatMoney(p.price)}</span>` : ''}
                                </div>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </section>

            <section class="section">
                <div class="section-header">
                    <span class="section-tag">Khám Phá Theo Dáng Cắm</span>
                    <h2 class="section-title">Danh Mục Hoa Tươi Nghệ Thuật</h2>
                </div>
                <div class="categories-grid">
                    ${categories.map(cat => `
                        <a href="/Product?categoryId=${cat.id}" class="category-card">
                            <div class="category-card-img"><img src="${cat.imageUrl}" /></div>
                            <div class="category-card-body">
                                <h3 class="category-card-title font-serif">${cat.name}</h3>
                                <div class="category-card-count">Khám phá ngay <i class="fa-solid fa-arrow-right" style="font-size: 0.75rem;"></i></div>
                            </div>
                        </a>
                    `).join('')}
                </div>
            </section>

            <section class="section">
                <div class="section-header">
                    <span class="section-tag">Yêu Thích Nhất</span>
                    <h2 class="section-title">Bó Hoa Best Seller Đang Thịnh Hành</h2>
                </div>
                <div class="products-grid">
                    ${bestSellers.map(p => `
                        <div class="product-card">
                            <div class="product-badges"><span class="badge-tag badge-sale">Best Seller</span></div>
                            <div class="product-thumb">
                                <img src="${p.mainImageUrl}" />
                                <div class="product-quick-actions">
                                    <button type="button" class="btn-quick-add js-quick-add" data-id="${p.id}"><i class="fa-solid fa-bag-shopping"></i> Thêm Nhanh</button>
                                </div>
                            </div>
                            <div class="product-info">
                                <div class="product-occasion">Boutique Collection</div>
                                <h3 class="product-title font-serif"><a href="/hoa/${p.slug}">${p.name}</a></h3>
                                <div class="product-rating"><i class="fa-solid fa-star"></i> <span>${p.ratingAvg} (${p.reviewCount} đánh giá)</span></div>
                                <div class="product-price-box">
                                    <span class="product-price">${formatMoney(p.salePrice || p.price)}</span>
                                </div>
                            </div>
                        </div>
                    `).join('')}
                </div>
                <div style="text-align: center; margin-top: 48px;">
                    <a href="/Product" class="btn btn-secondary">Xem Toàn Bộ Sản Phẩm</a>
                </div>
            </section>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Trang Chủ", html, 'home'));
        return;
    }

    // PRODUCT CATALOG
    if (pathname === '/Product') {
        const catId = parsedUrl.query.categoryId ? parseInt(parsedUrl.query.categoryId) : null;
        const occId = parsedUrl.query.occasionId ? parseInt(parsedUrl.query.occasionId) : null;
        let filtered = products;
        if (catId) filtered = filtered.filter(p => p.categoryId === catId);
        if (occId) filtered = filtered.filter(p => p.occasionId === occId);

        const html = `
            <div class="section" style="padding-top: 40px;">
                <div style="margin-bottom: 30px;">
                    <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 8px;"><a href="/">Trang Chủ</a> / <span style="color: var(--primary);">Bộ Sưu Tập Hoa</span></div>
                    <h1 class="section-title font-serif" style="text-align: left;">Bộ Sưu Tập Hoa Tươi & Quà Tặng</h1>
                    <p style="color: var(--text-muted);">Tìm thấy <strong>${filtered.length}</strong> tác phẩm hoa tươi phù hợp.</p>
                </div>
                <div style="display: grid; grid-template-columns: 260px 1fr; gap: 40px; align-items: start;">
                    <aside style="background: #fff; border: 1px solid var(--border); border-radius: 16px; padding: 24px;">
                        <h3 class="font-serif" style="font-size: 1.15rem; margin-bottom: 16px;"><i class="fa-solid fa-filter" style="color: var(--primary); margin-right: 6px;"></i> Lọc Theo Dịp</h3>
                        <div style="display: flex; flex-direction: column; gap: 8px; font-size: 0.88rem;">
                            <a href="/Product" style="font-weight: ${!occId ? '700; color: var(--primary)' : '400'}">• Tất cả các dịp</a>
                            ${occasions.map(o => `<a href="/Product?occasionId=${o.id}" style="font-weight: ${occId === o.id ? '700; color: var(--primary)' : '400'}">• ${o.name}</a>`).join('')}
                        </div>
                        <h3 class="font-serif" style="font-size: 1.15rem; margin-top: 24px; margin-bottom: 16px;"><i class="fa-solid fa-spa" style="color: var(--primary); margin-right: 6px;"></i> Lọc Dáng Cắm</h3>
                        <div style="display: flex; flex-direction: column; gap: 8px; font-size: 0.88rem;">
                            <a href="/Product" style="font-weight: ${!catId ? '700; color: var(--primary)' : '400'}">• Tất cả dáng cắm</a>
                            ${categories.map(c => `<a href="/Product?categoryId=${c.id}" style="font-weight: ${catId === c.id ? '700; color: var(--primary)' : '400'}">• ${c.name}</a>`).join('')}
                        </div>
                    </aside>
                    <div class="products-grid">
                        ${filtered.map(p => `
                            <div class="product-card">
                                <div class="product-thumb">
                                    <img src="${p.mainImageUrl}" />
                                    <div class="product-quick-actions">
                                        <button type="button" class="btn-quick-add js-quick-add" data-id="${p.id}"><i class="fa-solid fa-bag-shopping"></i> Thêm Nhanh</button>
                                    </div>
                                </div>
                                <div class="product-info">
                                    <h3 class="product-title font-serif"><a href="/hoa/${p.slug}">${p.name}</a></h3>
                                    <div class="product-price-box"><span class="product-price">${formatMoney(p.salePrice || p.price)}</span></div>
                                </div>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Bộ Sưu Tập Hoa", html, 'product'));
        return;
    }

    // PRODUCT DETAILS
    if (pathname.startsWith('/hoa/')) {
        const slug = pathname.replace('/hoa/', '');
        const p = products.find(prod => prod.slug === slug) || products[0];
        const html = `
            <div class="section" style="padding-top: 40px;">
                <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 24px;"><a href="/">Trang Chủ</a> / <a href="/Product">Hoa</a> / <span style="color: var(--primary);">${p.name}</span></div>
                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 50px; margin-bottom: 50px;">
                    <div>
                        <img src="${p.mainImageUrl}" style="width: 100%; height: 480px; object-fit: cover; border-radius: 20px; box-shadow: var(--shadow-sm);" />
                    </div>
                    <div>
                        <span class="badge-tag badge-sale">Boutique Exclusive</span>
                        <h1 class="font-serif" style="font-size: 2.2rem; margin: 12px 0;">${p.name}</h1>
                        <div style="color: #ffb800; margin-bottom: 20px;"><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i><i class="fa-solid fa-star"></i> <span style="color: #888;">(${p.reviewCount} đánh giá)</span></div>
                        <div style="background: var(--bg-body); padding: 18px 24px; border-radius: 16px; margin-bottom: 24px;">
                            <span style="font-size: 2rem; font-weight: 700; color: var(--primary);">${formatMoney(p.salePrice || p.price)}</span>
                        </div>
                        <p style="color: var(--text-muted); line-height: 1.7; margin-bottom: 28px;">${p.shortDesc}</p>
                        
                        <form action="/Cart/AddToCart" method="post" style="display: flex; gap: 16px; margin-bottom: 30px;">
                            <input type="hidden" name="productId" value="${p.id}" />
                            <input type="hidden" name="quantity" value="1" />
                            <button type="submit" class="btn btn-primary" style="flex: 1; padding: 14px 28px;">
                                <i class="fa-solid fa-bag-shopping"></i> Thêm Vào Giỏ Hoa
                            </button>
                            <a href="/Checkout" class="btn btn-secondary" style="padding: 14px 24px;">Mua Ngay</a>
                        </form>

                        <div style="background: #fdfafb; border: 1px solid var(--border); border-radius: 16px; padding: 18px;">
                            <div style="font-weight: 700; font-size: 0.9rem; margin-bottom: 8px;"><i class="fa-solid fa-droplet" style="color: var(--primary); margin-right: 6px;"></i> Bí quyết giữ hoa tươi lâu:</div>
                            <div style="font-size: 0.85rem; color: #666;">${p.careGuide}</div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout(p.name, html, 'product'));
        return;
    }

    // CUSTOM BOUQUET
    if (pathname === '/tu-thiet-ke-bo-hoa') {
        const html = `
            <div class="section" style="padding-top: 40px; max-width: 1100px;">
                <div class="section-header" style="text-align: left; margin-bottom: 36px;">
                    <span class="section-tag"><i class="fa-solid fa-wand-magic-sparkles"></i> Tự Cắm Hoa Online</span>
                    <h1 class="section-title">Tự Thiết Kế Bó Hoa Độc Bản Của Bạn</h1>
                    <p class="section-desc">Lựa chọn dáng cắm, loài hoa, số lượng cành và màu giấy gói theo ý thích.</p>
                </div>
                <div style="display: grid; grid-template-columns: 1.5fr 1fr; gap: 40px;">
                    <div style="display: flex; flex-direction: column; gap: 24px;">
                        <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                            <h3 class="font-serif" style="font-size: 1.2rem; margin-bottom: 14px;">1. Chọn Dáng Cắm</h3>
                            <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 12px;">
                                <div style="border: 2px solid var(--primary); border-radius: 12px; padding: 12px; text-align: center; cursor: pointer;">
                                    <i class="fa-solid fa-circle-notch" style="color: var(--primary); font-size: 1.5rem; margin-bottom: 6px;"></i>
                                    <div style="font-weight: 700; font-size: 0.88rem;">Bó Tròn</div>
                                </div>
                                <div style="border: 1px solid var(--border); border-radius: 12px; padding: 12px; text-align: center; cursor: pointer;">
                                    <i class="fa-solid fa-basket-shopping" style="color: #888; font-size: 1.5rem; margin-bottom: 6px;"></i>
                                    <div style="font-weight: 700; font-size: 0.88rem;">Giỏ Mây (+150k)</div>
                                </div>
                                <div style="border: 1px solid var(--border); border-radius: 12px; padding: 12px; text-align: center; cursor: pointer;">
                                    <i class="fa-solid fa-box-open" style="color: #888; font-size: 1.5rem; margin-bottom: 6px;"></i>
                                    <div style="font-weight: 700; font-size: 0.88rem;">Hộp Mica LED (+250k)</div>
                                </div>
                            </div>
                        </div>

                        <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                            <h3 class="font-serif" style="font-size: 1.2rem; margin-bottom: 14px;">2. Chọn Số Lượng Cành Hoa Ý Nghĩa</h3>
                            <div style="display: flex; gap: 10px; flex-wrap: wrap; margin-bottom: 14px;">
                                <span class="btn btn-secondary btn-sm" style="border-color: var(--primary); color: var(--primary);">19 cành (Chờ đợi)</span>
                                <span class="btn btn-secondary btn-sm">33 cành (Yêu say đắm)</span>
                                <span class="btn btn-secondary btn-sm">99 cành (Mãi mãi bên nhau)</span>
                            </div>
                        </div>
                    </div>

                    <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 28px; height: fit-content;">
                        <h3 class="font-serif" style="font-size: 1.3rem; margin-bottom: 16px; border-bottom: 1px solid var(--border); padding-bottom: 10px;">Ước Tính Chi Phí</h3>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 10px;"><span>Dáng cắm:</span><strong>Bó Tròn Chuẩn</strong></div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 10px;"><span>Số cành:</span><strong>19 Cành Hồng Ecuador</strong></div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 10px;"><span>Thiệp viết tay:</span><span style="color: #2e7d32; font-weight: 600;">Miễn phí</span></div>
                        <div style="font-size: 1.8rem; font-weight: 700; color: var(--primary); margin: 20px 0; text-align: center;">555.000 đ</div>
                        <form action="/Cart/AddToCart" method="post">
                            <input type="hidden" name="productId" value="1" />
                            <button type="submit" class="btn btn-primary" style="width: 100%;">Thêm Vào Giỏ & Đặt Ngay</button>
                        </form>
                    </div>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Tự Cắm Hoa Online", html, 'custom'));
        return;
    }

    // CART
    if (pathname === '/Cart') {
        const totals = getCartTotals();
        const html = `
            <div class="section" style="padding-top: 40px;">
                <h1 class="font-serif" style="font-size: 2.2rem; margin-bottom: 24px;">Giỏ Hoa Của Bạn</h1>
                <div style="display: grid; grid-template-columns: 2fr 1fr; gap: 40px; align-items: start;">
                    <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                        ${cart.items.length === 0 ? '<p>Giỏ hàng đang trống.</p>' : `
                            <table style="width: 100%; border-collapse: collapse;">
                                <thead>
                                    <tr style="border-bottom: 1px solid var(--border); text-align: left;">
                                        <th style="padding-bottom: 12px;">Sản Phẩm</th>
                                        <th style="padding-bottom: 12px; text-align: center;">Số Lượng</th>
                                        <th style="padding-bottom: 12px; text-align: right;">Thành Tiền</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${cart.items.map(i => `
                                        <tr style="border-bottom: 1px solid #f6eff0;">
                                            <td style="padding: 16px 0; display: flex; gap: 14px; align-items: center;">
                                                <img src="${i.image}" style="width: 60px; height: 60px; border-radius: 8px; object-fit: cover;" />
                                                <strong>${i.name}</strong>
                                            </td>
                                            <td style="text-align: center;">x ${i.qty}</td>
                                            <td style="text-align: right; font-weight: 700;">${formatMoney(i.price * i.qty)}</td>
                                            <td style="text-align: right;">
                                                <form action="/Cart/RemoveItem" method="post" style="margin: 0;">
                                                    <input type="hidden" name="productId" value="${i.productId}" />
                                                    <button type="submit" style="background: none; border: none; cursor: pointer; color: #b5a9ab;"><i class="fa-regular fa-trash-can"></i></button>
                                                </form>
                                            </td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        `}
                    </div>
                    <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                        <h3 class="font-serif" style="font-size: 1.25rem; margin-bottom: 16px;">Tạm Tính</h3>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 10px;"><span>Tiền hoa:</span><strong>${formatMoney(totals.subTotal)}</strong></div>
                        <div style="display: flex; justify-content: space-between; margin-bottom: 10px;"><span>Phí giao hoa:</span><span>${totals.shippingFee === 0 ? 'Miễn phí' : formatMoney(totals.shippingFee)}</span></div>
                        <div style="display: flex; justify-content: space-between; font-size: 1.3rem; font-weight: 700; color: var(--primary); border-top: 1px solid var(--border); padding-top: 14px; margin-bottom: 24px;">
                            <span>Tổng tiền:</span><span>${formatMoney(totals.total)}</span>
                        </div>
                        <a href="/Checkout" class="btn btn-primary" style="width: 100%; padding: 14px;">Tiến Hành Đặt Hoa <i class="fa-solid fa-arrow-right-long"></i></a>
                    </div>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Giỏ Hàng", html));
        return;
    }

    // CHECKOUT
    if (pathname === '/Checkout') {
        const totals = getCartTotals();
        const html = `
            <div class="section" style="padding-top: 40px; max-width: 1100px;">
                <h1 class="font-serif" style="font-size: 2.2rem; margin-bottom: 28px;">Thanh Toán & Đặt Hoa</h1>
                <form action="/Checkout/PlaceOrder" method="post">
                    <div style="display: grid; grid-template-columns: 1.6fr 1fr; gap: 40px;">
                        <div style="display: flex; flex-direction: column; gap: 24px;">
                            <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                                <h3 class="font-serif" style="font-size: 1.2rem; margin-bottom: 14px;">1. Người Đặt Hoa (Người Gửi)</h3>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
                                    <input type="text" name="SenderName" placeholder="Họ và tên của bạn" required style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;" />
                                    <input type="tel" name="SenderPhone" placeholder="Số điện thoại của bạn" required style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;" />
                                </div>
                            </div>
                            <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px;">
                                    <h3 class="font-serif" style="font-size: 1.2rem; margin: 0;">2. Người Nhận Hoa</h3>
                                    <label style="color: var(--primary); font-size: 0.85rem; font-weight: 600; cursor: pointer;">
                                        <input type="checkbox" name="IsAnonymous" value="true" /> Tặng bí mật (Ẩn danh)
                                    </label>
                                </div>
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 14px;">
                                    <input type="text" name="RecipientName" placeholder="Tên người nhận hoa" required style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;" />
                                    <input type="tel" name="RecipientPhone" placeholder="Số ĐT người nhận" required style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;" />
                                </div>
                                <input type="text" name="RecipientAddress" placeholder="Địa chỉ giao hoa chi tiết (Số nhà, tên đường, quận/huyện)" required style="width: 100%; padding: 10px; border: 1px solid var(--border); border-radius: 8px; margin-bottom: 14px;" />
                                <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
                                    <input type="date" name="DeliveryDate" value="${new Date().toISOString().split('T')[0]}" style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;" />
                                    <select name="DeliveryTimeSlot" style="padding: 10px; border: 1px solid var(--border); border-radius: 8px;">
                                        <option>08:00 - 10:00 (Buổi sáng)</option>
                                        <option>10:00 - 12:00 (Trưa)</option>
                                        <option>14:00 - 16:00 (Chiều)</option>
                                        <option>18:00 - 20:00 (Tối lãng mạn)</option>
                                        <option>Hỏa tốc trong 2 giờ</option>
                                    </select>
                                </div>
                            </div>
                            <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px;">
                                <h3 class="font-serif" style="font-size: 1.2rem; margin-bottom: 14px;">3. Chọn Cổng Thanh Toán</h3>
                                <div style="display: flex; flex-direction: column; gap: 10px;">
                                    <label style="padding: 12px; border: 2px solid var(--primary); border-radius: 10px; cursor: pointer; display: flex; align-items: center; gap: 10px;">
                                        <input type="radio" name="PaymentMethod" value="VNPAY" checked />
                                        <strong><i class="fa-solid fa-qrcode"></i> Cổng VNPAY (QR Pay, Thẻ ATM 30+ Ngân Hàng, Visa)</strong>
                                    </label>
                                    <label style="padding: 12px; border: 1px solid var(--border); border-radius: 10px; cursor: pointer; display: flex; align-items: center; gap: 10px;">
                                        <input type="radio" name="PaymentMethod" value="STRIPE" />
                                        <strong><i class="fa-brands fa-stripe" style="color: #635bff;"></i> Cổng Stripe (Thẻ Quốc Tế Visa/Mastercard 3D Secure)</strong>
                                    </label>
                                    <label style="padding: 12px; border: 1px solid var(--border); border-radius: 10px; cursor: pointer; display: flex; align-items: center; gap: 10px;">
                                        <input type="radio" name="PaymentMethod" value="VIETQR" />
                                        <strong><i class="fa-solid fa-building-columns" style="color: #005030;"></i> Quét Mã VietQR Chuyển Khoản 24/7</strong>
                                    </label>
                                    <label style="padding: 12px; border: 1px solid var(--border); border-radius: 10px; cursor: pointer; display: flex; align-items: center; gap: 10px;">
                                        <input type="radio" name="PaymentMethod" value="COD" />
                                        <strong><i class="fa-solid fa-money-bill-wave"></i> Thanh Toán Khi Nhận Hoa (COD)</strong>
                                    </label>
                                </div>
                            </div>
                        </div>

                        <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px; height: fit-content; position: sticky; top: 100px;">
                            <h3 class="font-serif" style="font-size: 1.25rem; margin-bottom: 16px;">Đơn Hàng</h3>
                            <div style="font-size: 1.4rem; font-weight: 700; color: var(--primary); margin-bottom: 20px;">${formatMoney(totals.total)}</div>
                            <button type="submit" class="btn btn-primary" style="width: 100%; padding: 14px;">Xác Nhận & Đặt Hoa</button>
                        </div>
                    </div>
                </form>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Thanh Toán Đặt Hoa", html));
        return;
    }

    // ORDER TRACKING
    if (pathname === '/tra-cuu-don-hang' || pathname === '/OrderTracking') {
        const oCode = parsedUrl.query.orderCode || (orders[0] ? orders[0].orderCode : '');
        const found = orders.find(o => o.orderCode.toLowerCase() === oCode.toLowerCase()) || orders[0];

        const html = `
            <div class="section" style="padding-top: 40px; max-width: 850px;">
                <div class="section-header">
                    <span class="section-tag"><i class="fa-solid fa-truck-fast"></i> Minh Bạch & Tận Tâm</span>
                    <h1 class="section-title">Theo Dõi Vận Đơn & Duyệt Ảnh Hoa Thực Tế</h1>
                </div>
                <div style="background: #fff; border: 1px solid var(--border); border-radius: 18px; padding: 24px; margin-bottom: 30px;">
                    <form action="/tra-cuu-don-hang" method="get" style="display: flex; gap: 12px;">
                        <input type="text" name="orderCode" value="${found ? found.orderCode : ''}" placeholder="Nhập mã đơn hàng..." style="flex: 1; padding: 12px; border: 1px solid var(--border); border-radius: 8px; text-transform: uppercase;" />
                        <button type="submit" class="btn btn-primary">Tra Cứu</button>
                    </form>
                </div>

                ${found ? `
                    <div style="background: #fff; border: 1px solid var(--border); border-radius: 20px; padding: 32px; box-shadow: var(--shadow-sm);">
                        <div style="display: flex; justify-content: space-between; border-bottom: 1px solid var(--border); padding-bottom: 16px; margin-bottom: 24px;">
                            <div>
                                <span style="font-size: 0.8rem; color: #888;">MÃ ĐƠN HÀNG:</span>
                                <h2 class="font-serif" style="font-size: 1.5rem; color: var(--primary); margin: 0;">#${found.orderCode}</h2>
                            </div>
                            <div style="text-align: right;">
                                <span style="font-size: 0.8rem; color: #888;">TRẠNG THÁI:</span>
                                <div style="font-weight: 700; color: #2e7d32;">Đã Cắm Xong - Chờ Khách Duyệt Ảnh</div>
                            </div>
                        </div>

                        <!-- 5-Step Timeline -->
                        <div style="display: flex; justify-content: space-between; position: relative; margin-bottom: 36px; text-align: center;">
                            <div style="flex: 1;">
                                <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--primary); color: #fff; display: flex; align-items: center; justify-content: center; margin: 0 auto 6px;"><i class="fa-solid fa-check"></i></div>
                                <div style="font-size: 0.78rem; font-weight: 700;">Đã Nhận</div>
                            </div>
                            <div style="flex: 1;">
                                <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--primary); color: #fff; display: flex; align-items: center; justify-content: center; margin: 0 auto 6px;"><i class="fa-solid fa-check"></i></div>
                                <div style="font-size: 0.78rem; font-weight: 700;">Đã Xác Nhận</div>
                            </div>
                            <div style="flex: 1;">
                                <div style="width: 36px; height: 36px; border-radius: 50%; background: var(--primary); color: #fff; display: flex; align-items: center; justify-content: center; margin: 0 auto 6px; box-shadow: 0 0 0 4px #ffd4dc;"><i class="fa-solid fa-camera"></i></div>
                                <div style="font-size: 0.78rem; font-weight: 700; color: var(--primary);">Duyệt Ảnh</div>
                            </div>
                            <div style="flex: 1;">
                                <div style="width: 36px; height: 36px; border-radius: 50%; background: #eee; color: #888; display: flex; align-items: center; justify-content: center; margin: 0 auto 6px;">4</div>
                                <div style="font-size: 0.78rem; color: #888;">Đang Giao</div>
                            </div>
                            <div style="flex: 1;">
                                <div style="width: 36px; height: 36px; border-radius: 50%; background: #eee; color: #888; display: flex; align-items: center; justify-content: center; margin: 0 auto 6px;">5</div>
                                <div style="font-size: 0.78rem; color: #888;">Hoàn Tất</div>
                            </div>
                        </div>

                        <!-- Proof Photo -->
                        <div style="background: var(--bg-body); border-radius: 16px; padding: 20px; border: 2px dashed #ffd4dc; display: flex; gap: 20px; align-items: center;">
                            <img src="${found.proofPhotoUrl}" style="width: 180px; height: 180px; border-radius: 12px; object-fit: cover;" />
                            <div>
                                <h3 class="font-serif" style="font-size: 1.2rem; margin-bottom: 6px;">Ảnh Chụp Hoa Thực Tế Tại Xưởng</h3>
                                <p style="font-size: 0.88rem; color: var(--text-muted); margin-bottom: 14px;">Bó hoa của bạn đã được hoàn thiện đúng tiêu chuẩn. Hãy xem và duyệt để chúng tôi giao ngay nhé!</p>
                                <button type="button" class="btn btn-primary btn-sm" onclick="Swal.fire('Đã duyệt!', 'Shipper sẽ xuất phát giao hoa ngay!', 'success')"><i class="fa-solid fa-check"></i> Tôi Hài Lòng, Giao Ngay</button>
                            </div>
                        </div>
                    </div>
                ` : ''}
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Tra Cứu Vận Đơn", html, 'tracking'));
        return;
    }

    // PAYMENT VIETQR
    if (pathname === '/Payment/VietQrPay') {
        const oCode = parsedUrl.query.orderCode || 'FL202609088831';
        const o = orders.find(ord => ord.orderCode === oCode) || orders[0];
        const qrUrl = `https://img.vietqr.io/image/MB-0909123456888-compact2.png?amount=${o.totalAmount}&addInfo=THANH%20TOAN%20${o.orderCode}&accountName=TIEM%20HOA%20FLOWER%20BOUTIQUE`;
        const html = `
            <div class="section" style="padding-top: 50px; max-width: 650px; text-align: center;">
                <div style="background: #fff; border: 1px solid var(--border); border-radius: 24px; padding: 40px;">
                    <h1 class="font-serif" style="font-size: 1.8rem; margin-bottom: 8px;">Quét Mã VietQR Chuyển Khoản</h1>
                    <p style="color: #666; margin-bottom: 24px;">Mở app ngân hàng bất kỳ để quét mã tự động điền tiền và nội dung.</p>
                    <img src="${qrUrl}" style="width: 260px; height: auto; border-radius: 12px; border: 2px solid var(--border); margin: 0 auto 20px;" />
                    <div style="background: var(--bg-body); padding: 16px; border-radius: 12px; text-align: left; font-size: 0.9rem; line-height: 1.8; margin-bottom: 24px;">
                        <div>Mã đơn: <strong>${o.orderCode}</strong></div>
                        <div>Số tiền: <strong style="color: var(--primary);">${formatMoney(o.totalAmount)}</strong></div>
                        <div>Nội dung: <strong style="color: #005030;">THANH TOAN ${o.orderCode}</strong></div>
                    </div>
                    <a href="/Payment/Success?orderCode=${o.orderCode}" class="btn btn-primary">Tôi Đã Chuyển Khoản Thành Công</a>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Quét Mã VietQR", html));
        return;
    }

    // PAYMENT STRIPE
    if (pathname === '/Payment/StripeCheckout') {
        const oCode = parsedUrl.query.orderCode || 'FL202609088831';
        const o = orders.find(ord => ord.orderCode === oCode) || orders[0];
        const html = `
            <div class="section" style="padding-top: 50px; max-width: 600px;">
                <div style="background: #fff; border: 1px solid var(--border); border-radius: 24px; padding: 36px;">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 20px;">
                        <i class="fa-brands fa-stripe" style="font-size: 3rem; color: #635bff;"></i>
                        <div>
                            <h2 class="font-serif" style="font-size: 1.4rem; margin: 0;">Thanh Toán Thẻ Quốc Tế</h2>
                            <span style="font-size: 0.8rem; color: #888;">Stripe 3D Secure Protection</span>
                        </div>
                    </div>
                    <div style="background: var(--bg-body); padding: 14px; border-radius: 10px; margin-bottom: 20px;">
                        <div>Đơn hàng: <strong>${o.orderCode}</strong> - Tổng tiền: <strong style="color: var(--primary);">${formatMoney(o.totalAmount)}</strong></div>
                    </div>
                    <div style="margin-bottom: 20px;">
                        <label style="font-size: 0.85rem; font-weight: 600; display: block; margin-bottom: 6px;">Số Thẻ Tín Dụng (Visa, Master, JCB):</label>
                        <input type="text" placeholder="4242 •••• •••• 4242" value="4242 4242 4242 4242" style="width: 100%; padding: 12px; border: 1px solid var(--border); border-radius: 8px; margin-bottom: 10px;" />
                        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 10px;">
                            <input type="text" placeholder="MM/YY" value="12/28" style="padding: 12px; border: 1px solid var(--border); border-radius: 8px;" />
                            <input type="text" placeholder="CVC" value="123" style="padding: 12px; border: 1px solid var(--border); border-radius: 8px;" />
                        </div>
                    </div>
                    <a href="/Payment/Success?orderCode=${o.orderCode}" class="btn btn-primary" style="width: 100%; padding: 14px; background: #635bff;"><i class="fa-solid fa-lock"></i> Xác Nhận Thanh Toán Ngay</a>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Thanh Toán Stripe", html));
        return;
    }

    // PAYMENT SUCCESS
    if (pathname === '/Payment/Success') {
        const oCode = parsedUrl.query.orderCode || 'FL202609088831';
        const o = orders.find(ord => ord.orderCode === oCode) || orders[0];
        const html = `
            <div class="section" style="padding-top: 50px; max-width: 700px; text-align: center;">
                <div style="background: #fff; border: 1px solid var(--border); border-radius: 24px; padding: 40px;">
                    <div style="width: 70px; height: 70px; border-radius: 50%; background: #e8f5e9; color: #2e7d32; display: flex; align-items: center; justify-content: center; font-size: 2.2rem; margin: 0 auto 20px;">
                        <i class="fa-solid fa-check"></i>
                    </div>
                    <h1 class="font-serif" style="font-size: 2rem; margin-bottom: 10px;">Đặt Hoa Thành Công!</h1>
                    <p style="color: #666; margin-bottom: 24px;">Đơn hàng <strong>#${o.orderCode}</strong> đã được tiếp nhận. Nghệ nhân đang bắt đầu cắm hoa cho bạn.</p>
                    <div style="display: flex; gap: 12px; justify-content: center;">
                        <a href="/tra-cuu-don-hang?orderCode=${o.orderCode}" class="btn btn-primary">Theo Dõi Vận Đơn & Xem Ảnh Hoa</a>
                        <a href="/" class="btn btn-secondary">Về Trang Chủ</a>
                    </div>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Đặt Hoa Thành Công", html));
        return;
    }

    // ADMIN DASHBOARD
    if (pathname === '/Admin' || pathname === '/Admin/Dashboard') {
        const adminHtml = `<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <title>Admin Dashboard - Bloom & Grace</title>
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
    <link rel="stylesheet" href="/Content/css/admin.css" />
</head>
<body class="admin-body">
    <aside class="admin-sidebar">
        <div class="admin-brand"><i class="fa-solid fa-spa"></i><h2>Bloom & Grace</h2></div>
        <ul class="admin-menu">
            <li><a href="/Admin/Dashboard" class="active"><i class="fa-solid fa-chart-pie"></i> Tổng Quan</a></li>
            <li><a href="/Admin/Order"><i class="fa-solid fa-boxes-packing"></i> Quản Lý Đơn</a></li>
            <li><a href="/Admin/Product"><i class="fa-solid fa-fan"></i> Mẫu Hoa</a></li>
            <li><a href="/Admin/Settings"><i class="fa-solid fa-sliders"></i> Cấu Hình Cổng TT</a></li>
            <li style="margin-top: auto;"><a href="/" target="_blank" style="color: #ffd4dc;"><i class="fa-solid fa-arrow-up-right-from-square"></i> Xem Store</a></li>
        </ul>
    </aside>
    <div class="admin-main">
        <header class="admin-topbar">
            <div style="font-weight: 700;">Hệ Thống Quản Trị Cửa Hàng Hoa</div>
            <div style="display: flex; align-items: center; gap: 10px;">
                <span class="status-pill pill-delivered">Admin Online</span>
            </div>
        </header>
        <main class="admin-content">
            <h1 style="font-size: 1.6rem; margin-bottom: 24px;">Tổng Quan Hoạt Động</h1>
            <div class="metrics-grid">
                <div class="metric-card"><div><div class="metric-title">Doanh Thu Hôm Nay</div><div class="metric-value">4.250.000 đ</div></div><div class="metric-icon metric-primary"><i class="fa-solid fa-coins"></i></div></div>
                <div class="metric-card"><div><div class="metric-title">Doanh Thu Tháng</div><div class="metric-value">68.500.000 đ</div></div><div class="metric-icon metric-success"><i class="fa-solid fa-chart-line"></i></div></div>
                <div class="metric-card"><div><div class="metric-title">Đơn Giao Gấp</div><div class="metric-value" style="color: #c84b61;">3 đơn</div></div><div class="metric-icon metric-warning"><i class="fa-solid fa-clock"></i></div></div>
                <div class="metric-card"><div><div class="metric-title">Tổng Mẫu Hoa</div><div class="metric-value">${products.length} mẫu</div></div><div class="metric-icon metric-info"><i class="fa-solid fa-seedling"></i></div></div>
            </div>
            <div class="admin-card-box">
                <h3 style="margin-bottom: 16px;">Đơn Hoa Mới Nhất</h3>
                <table class="admin-table">
                    <thead><tr><th>Mã Đơn</th><th>Người Nhận</th><th>Giờ Giao</th><th>Tổng Tiền</th><th>Cổng TT</th><th>Trạng Thái</th></tr></thead>
                    <tbody>
                        ${orders.map(o => `
                            <tr>
                                <td><strong>${o.orderCode}</strong></td>
                                <td>${o.recipientName}</td>
                                <td>${o.deliveryDate} (${o.deliveryTimeSlot})</td>
                                <td><strong>${formatMoney(o.totalAmount)}</strong></td>
                                <td><span class="status-pill pill-confirmed">${o.paymentMethod}</span></td>
                                <td><span class="status-pill pill-delivered">${o.orderStatus}</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </main>
    </div>
</body>
</html>`;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(adminHtml);
        return;
    }

    // ADMIN ORDERS
    if (pathname === '/Admin/Order') {
        res.writeHead(302, { Location: '/Admin/Dashboard' });
        res.end();
        return;
    }

    // ABOUT PAGE
    if (pathname === '/Home/About') {
        const html = `
            <div class="section" style="padding-top: 50px; max-width: 900px;">
                <div class="section-header">
                    <span class="section-tag">Câu Chuyện Thương Hiệu</span>
                    <h1 class="section-title">Bloom & Grace Floral Boutique</h1>
                </div>
                <div style="background: #fff; border-radius: 20px; border: 1px solid var(--border); padding: 36px; line-height: 1.8;">
                    <img src="https://images.unsplash.com/photo-1526047932273-341f2a7631f9?auto=format&fit=crop&w=1000&q=80" style="width: 100%; height: 350px; object-fit: cover; border-radius: 12px; margin-bottom: 24px;" />
                    <h3 class="font-serif" style="font-size: 1.5rem; margin-bottom: 12px;">Cam Kết Hoa Tươi 100% Tuyển Chọn</h3>
                    <p>Tại Bloom & Grace, từng cành hoa đều được chăm chút kỹ lưỡng và cắm bởi nghệ nhân hoa hàng đầu. Chúng tôi luôn chụp ảnh gửi khách hàng duyệt trước khi giao hàng để đảm bảo trải nghiệm trọn vẹn nhất.</p>
                </div>
            </div>
        `;
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(renderLayout("Về Chúng Tôi", html, 'about'));
        return;
    }

    // 404 Fallback
    res.writeHead(302, { Location: '/' });
    res.end();
});

server.listen(PORT, '0.0.0.0', () => {
    console.log(`🌸 Bloom & Grace Floral Boutique running on http://localhost:${PORT}`);
});
