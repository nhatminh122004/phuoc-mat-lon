/**
 * HỮU PHƯỚC FLORIST - MODERN E-COMMERCE CART & CLIENT-SIDE STATE
 * Handles real-time Cart operations, Voucher validation, LocalStorage persistence,
 * and Header badge synchronization.
 */

const BloomCart = (function () {
    const STORAGE_KEY = 'bloom_grace_cart';
    const FREE_SHIPPING_THRESHOLD = 1000000; // 1.000.000 đ
    const DEFAULT_SHIPPING_FEE = 30000;      // 30.000 đ

    // Sample initial items if user visits for the first time
    const defaultInitialItems = [
        {
            productId: 1,
            name: "Bó Hoa Hồng Đỏ Ecuador 'Red Romance'",
            slug: "bo-hoa-hong-do-ecuador-red-romance",
            category: "Bó Hoa Tươi",
            price: 850000,
            qty: 1,
            image: "https://images.unsplash.com/photo-1518895949257-7621c3c786d7?auto=format&fit=crop&w=400&q=80"
        },
        {
            productId: 2,
            name: "Bó Hoa Tulip Hồng 'Sweet Whispers'",
            slug: "bo-hoa-tulip-hong-sweet-whispers",
            category: "Bó Hoa Tươi",
            price: 1050000,
            qty: 1,
            image: "https://images.unsplash.com/photo-1520763185298-1b434c919102?auto=format&fit=crop&w=400&q=80"
        }
    ];

    function getCart() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) {
                localStorage.setItem(STORAGE_KEY, JSON.stringify(defaultInitialItems));
                return defaultInitialItems;
            }
            return JSON.parse(raw);
        } catch (e) {
            return defaultInitialItems;
        }
    }

    function saveCart(items) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        updateHeaderBadge();
    }

    function formatMoney(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount) + ' đ';
    }

    function calculateTotals(items) {
        const subTotal = items.reduce((sum, item) => sum + (item.price * item.qty), 0);
        const count = items.reduce((sum, item) => sum + item.qty, 0);
        const shippingFee = (subTotal >= FREE_SHIPPING_THRESHOLD || subTotal === 0) ? 0 : DEFAULT_SHIPPING_FEE;
        
        let discount = 0;
        const voucherCode = sessionStorage.getItem('bloom_voucher') || '';
        if (voucherCode === 'BLOOM10') {
            discount = Math.round(subTotal * 0.1);
        } else if (voucherCode === 'WELCOME50') {
            discount = Math.min(50000, subTotal);
        } else if (voucherCode === 'SKYBLUE') {
            discount = Math.min(100000, subTotal);
        }

        const total = Math.max(0, subTotal + shippingFee - discount);

        return {
            subTotal,
            count,
            shippingFee,
            discount,
            voucherCode,
            total
        };
    }

    function updateHeaderBadge() {
        const items = getCart();
        const totals = calculateTotals(items);
        const badges = document.querySelectorAll('.js-cart-count');
        badges.forEach(b => {
            b.textContent = totals.count;
            b.style.display = totals.count > 0 ? 'flex' : 'none';
        });
    }

    function addToCart(product, quantity = 1) {
        const items = getCart();
        const existing = items.find(i => i.productId === product.productId);
        if (existing) {
            existing.qty += quantity;
        } else {
            items.push({
                productId: product.productId,
                name: product.name,
                slug: product.slug || '',
                category: product.category || 'Hoa Tươi',
                price: product.price,
                qty: quantity,
                image: product.image
            });
        }
        saveCart(items);

        // Show elegant toast
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Đã thêm vào giỏ hoa!',
                text: product.name,
                showConfirmButton: false,
                timer: 2200,
                timerProgressBar: true
            });
        }
    }

    function updateQuantity(productId, newQty) {
        let items = getCart();
        if (newQty <= 0) {
            removeItem(productId);
            return;
        }
        const item = items.find(i => i.productId === productId);
        if (item) {
            item.qty = newQty;
            saveCart(items);
            renderCartPage();
        }
    }

    function removeItem(productId) {
        let items = getCart();
        items = items.filter(i => i.productId !== productId);
        saveCart(items);
        renderCartPage();

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'info',
                title: 'Đã xóa sản phẩm khỏi giỏ!',
                showConfirmButton: false,
                timer: 1800,
                timerProgressBar: true
            });
        }
    }

    function clearCart() {
        saveCart([]);
        renderCartPage();
    }

    function applyVoucher(code) {
        const cleanCode = (code || '').trim().toUpperCase();
        if (cleanCode === 'BLOOM10' || cleanCode === 'WELCOME50' || cleanCode === 'SKYBLUE') {
            sessionStorage.setItem('bloom_voucher', cleanCode);
            renderCartPage();
            if (typeof Swal !== 'undefined') {
                Swal.fire('Áp Dụng Mã Thành Công!', `Mã ${cleanCode} đã được kích hoạt.`, 'success');
            }
            return true;
        } else {
            if (typeof Swal !== 'undefined') {
                Swal.fire('Mã Không Hợp Lệ', 'Vui lòng thử các mã: BLOOM10, WELCOME50 hoặc SKYBLUE', 'error');
            }
            return false;
        }
    }

    function renderCartPage() {
        const container = document.getElementById('cartPageContainer');
        if (!container) return;

        const items = getCart();
        const totals = calculateTotals(items);

        if (items.length === 0) {
            container.innerHTML = `
                <div class="empty-cart-state">
                    <div class="empty-cart-icon">
                        <i class="fa-solid fa-basket-shopping"></i>
                    </div>
                    <h3 class="font-serif">Giỏ hàng của bạn đang trống</h3>
                    <p>Có vẻ như bạn chưa chọn được bó hoa ưng ý. Hãy khám phá ngay các bộ sưu tập hoa tươi nghệ thuật mới nhất của Hữu Phước Florist!</p>
                    <a href="/Product" class="btn btn-primary btn-lg">
                        <i class="fa-solid fa-seedling"></i> Khám Phá Hoa Tươi Ngay
                    </a>
                </div>
            `;
            return;
        }

        // Calculate progress percentage for free shipping
        const fsProgress = Math.min(100, Math.round((totals.subTotal / FREE_SHIPPING_THRESHOLD) * 100));
        const neededForFs = Math.max(0, FREE_SHIPPING_THRESHOLD - totals.subTotal);

        container.innerHTML = `
            <div class="cart-grid-container">
                <!-- Left: Products List -->
                <div>
                    <!-- Free Shipping Bar -->
                    <div class="free-shipping-card">
                        <div class="free-shipping-card-text">
                            <i class="fa-solid fa-truck-fast"></i>
                            ${fsProgress >= 100 
                                ? '<span style="color: #0284C7;">Chúc mừng! Đơn hàng của bạn đã đủ điều kiện <strong>Miễn Phí Giao Hoa Toàn Quốc</strong></span>'
                                : `<span>Mua thêm <strong>${formatMoney(neededForFs)}</strong> để nhận <strong>Miễn Phí Giao Hoa</strong></span>`
                            }
                        </div>
                        <div class="fs-progress-bg">
                            <div class="fs-progress-fill" style="width: ${fsProgress}%;"></div>
                        </div>
                    </div>

                    <!-- Cart Item Cards -->
                    <div class="cart-items-wrapper">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; padding-bottom: 12px; border-bottom: 1px solid var(--border);">
                            <span style="font-weight: 700; font-size: 1.05rem; color: var(--dark);">Sản phẩm trong giỏ (${items.length})</span>
                            <button type="button" onclick="BloomCart.clearCart()" style="background: none; border: none; color: #EF4444; font-size: 0.85rem; font-weight: 600; cursor: pointer;">
                                <i class="fa-regular fa-trash-can"></i> Xóa tất cả
                            </button>
                        </div>

                        ${items.map(item => `
                            <div class="cart-item-card" id="cart-item-${item.productId}">
                                <img src="${item.image}" alt="${item.name}" class="cart-item-img" />
                                <div class="cart-item-info">
                                    <span class="cart-item-category">${item.category || 'Hoa Tươi'}</span>
                                    <a href="/hoa/${item.slug}" class="cart-item-title">${item.name}</a>
                                    <div class="cart-item-price">Đơn giá: <strong>${formatMoney(item.price)}</strong></div>
                                </div>
                                <div class="qty-stepper">
                                    <button type="button" onclick="BloomCart.updateQuantity(${item.productId}, ${item.qty - 1})">-</button>
                                    <span>${item.qty}</span>
                                    <button type="button" onclick="BloomCart.updateQuantity(${item.productId}, ${item.qty + 1})">+</button>
                                </div>
                                <div class="cart-item-subtotal">
                                    ${formatMoney(item.price * item.qty)}
                                </div>
                                <button type="button" class="btn-remove-item" title="Xóa sản phẩm" onclick="BloomCart.removeItem(${item.productId})">
                                    <i class="fa-regular fa-trash-can"></i>
                                </button>
                            </div>
                        `).join('')}
                    </div>

                    <div style="margin-top: 20px;">
                        <a href="/Product" class="btn btn-secondary" style="border-radius: var(--radius-full);">
                            <i class="fa-solid fa-arrow-left"></i> Tiếp Tục Chọn Hoa
                        </a>
                    </div>
                </div>

                <!-- Right: Order Summary -->
                <div class="order-summary-card">
                    <h3 class="summary-title font-serif">Tóm Tắt Đơn Hàng</h3>

                    <div class="summary-row">
                        <span>Tổng số lượng:</span>
                        <strong>${totals.count} sản phẩm</strong>
                    </div>

                    <div class="summary-row">
                        <span>Tạm tính tiền hoa:</span>
                        <span class="val">${formatMoney(totals.subTotal)}</span>
                    </div>

                    <div class="summary-row">
                        <span>Phí vận chuyển:</span>
                        <span class="val" style="${totals.shippingFee === 0 ? 'color: #0284C7; font-weight: 700;' : ''}">
                            ${totals.shippingFee === 0 ? 'Miễn phí' : formatMoney(totals.shippingFee)}
                        </span>
                    </div>

                    ${totals.discount > 0 ? `
                        <div class="summary-row" style="color: #0284C7;">
                            <span>Mã giảm giá (${totals.voucherCode}):</span>
                            <span class="val" style="color: #0284C7;">-${formatMoney(totals.discount)}</span>
                        </div>
                    ` : ''}

                    <!-- Voucher code input -->
                    <div style="margin-top: 18px;">
                        <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 6px; color: var(--dark);">Mã Ưu Đãi (Voucher):</label>
                        <div class="voucher-form">
                            <input type="text" id="cartVoucherInput" placeholder="VD: SKYBLUE, BLOOM10" value="${totals.voucherCode || ''}" />
                            <button type="button" onclick="BloomCart.applyVoucher(document.getElementById('cartVoucherInput').value)">Áp Dụng</button>
                        </div>
                    </div>

                    <div class="summary-divider"></div>

                    <div class="summary-total-row">
                        <span>Tổng thanh toán:</span>
                        <span class="total-amount">${formatMoney(totals.total)}</span>
                    </div>

                    <a href="/Checkout" class="btn btn-primary" style="width: 100%; padding: 15px; font-size: 1.05rem; margin-bottom: 12px;">
                        Tiến Hành Đặt Hoa <i class="fa-solid fa-arrow-right-long"></i>
                    </a>

                    <div class="cart-guarantee-box">
                        <div class="cart-guarantee-item">
                            <i class="fa-solid fa-shield-halved"></i>
                            <span>Bảo mật thanh toán 100% qua SSL 256-bit</span>
                        </div>
                        <div class="cart-guarantee-item">
                            <i class="fa-solid fa-camera"></i>
                            <span>Nghệ nhân chụp ảnh hoa thật gửi khách duyệt trước khi giao</span>
                        </div>
                        <div class="cart-guarantee-item">
                            <i class="fa-solid fa-rotate-left"></i>
                            <span>Cam kết hoa tươi tối thiểu 3 - 5 ngày</span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    // Auto-initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        updateHeaderBadge();
        renderCartPage();
    });

    return {
        getCart,
        addToCart,
        updateQuantity,
        removeItem,
        clearCart,
        applyVoucher,
        renderCartPage,
        updateHeaderBadge
    };
})();
