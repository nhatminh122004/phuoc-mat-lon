/**
 * FLOWER BOUTIQUE - CART (server-driven)
 * -----------------------------------------------------------------------------
 * Nguyên tắc: SERVER LÀ NGUỒN SỰ THẬT DUY NHẤT.
 *  - Không còn localStorage cart, không tự tính tiền ở client.
 *  - Mọi con số (tạm tính, phí ship, giảm giá, tổng) lấy từ /Cart/GetCartJson
 *    (server dùng PricingService - cùng công thức với Checkout).
 */
const BloomCart = (function () {
    'use strict';

    const API = {
        get: '/Cart/GetCartJson',
        miniCart: '/Cart/GetMiniCart',
        add: '/Cart/AddToCart',
        update: '/Cart/UpdateQuantity',
        remove: '/Cart/RemoveItem',
        clear: '/Cart/ClearCart',
        addAddon: '/Cart/AddAddon',
        removeAddon: '/Cart/RemoveAddon',
        voucher: '/Cart/ApplyVoucher'
    };

    let state = emptyState();

    function emptyState() {
        return {
            items: [],
            addons: [],
            totalItems: 0,
            subTotal: 0,
            addonTotal: 0,
            shippingFee: 0,
            discountAmount: 0,
            totalAmount: 0,
            voucherCode: '',
            freeShipping: false,
            amountToFreeShipping: 0,
            freeShippingThreshold: 500000
        };
    }

    function formatMoney(amount) {
        return new Intl.NumberFormat('vi-VN').format(amount || 0) + ' đ';
    }

    function applyServerResponse(data) {
        if (!data) return;

        state = {
            items: data.items || [],
            addons: data.addons || [],
            totalItems: data.totalItems || 0,
            subTotal: data.subTotal || 0,
            addonTotal: data.addonTotal || 0,
            shippingFee: data.shippingFee || 0,
            discountAmount: data.discountAmount || 0,
            totalAmount: data.totalAmount || 0,
            voucherCode: data.voucherCode || '',
            freeShipping: !!data.freeShipping,
            amountToFreeShipping: data.amountToFreeShipping || 0,
            freeShippingThreshold: data.freeShippingThreshold || 500000
        };

        updateHeaderBadge();
        renderCartPage();
        renderMiniCartBadge();
    }

    function syncFromServer() {
        return fetch(API.get, {
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
            credentials: 'same-origin'
        })
            .then(function (res) { return res.json(); })
            .then(function (data) { applyServerResponse(data); return state; })
            .catch(function () { return state; });
    }

    function updateHeaderBadge() {
        document.querySelectorAll('.js-cart-count').forEach(function (el) {
            el.textContent = state.totalItems;
        });
    }

    function renderMiniCartBadge() {
        const badge = document.querySelector('.js-mini-cart-count');
        if (badge) {
            badge.textContent = state.totalItems;
        }
    }

    function post(url, params) {
        const body = window.BloomCsrf ? window.BloomCsrf.withToken(params) : params;
        return fetch(url, {
            method: 'POST',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
            body: body.toString(),
            credentials: 'same-origin'
        }).then(function (res) { return res.json(); });
    }

    function notify(title, text, icon) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({ icon: icon || 'success', title: title, text: text, confirmButtonColor: '#0284C7', timer: icon === 'success' ? 1800 : undefined, showConfirmButton: icon !== 'success' });
        }
    }

    function addToCart(productId, quantity) {
        const params = new URLSearchParams();
        params.append('productId', productId);
        params.append('quantity', quantity || 1);
        params.append('isAjax', 'true');

        return post(API.add, params).then(function (data) {
            applyServerResponse(data);
            if (data.success) {
                notify('Đã thêm vào giỏ!', data.message || 'Sản phẩm đã được thêm vào giỏ hoa của bạn.', 'success');
            } else {
                notify('Không thể thêm', data.message || 'Vui lòng thử lại.', 'error');
            }
            return data;
        });
    }

    function updateQuantity(productId, newQty) {
        const params = new URLSearchParams();
        params.append('productId', productId);
        params.append('quantity', newQty);
        params.append('isAjax', 'true');

        return post(API.update, params).then(function (data) { applyServerResponse(data); return data; });
    }

    function removeItem(productId) {
        const params = new URLSearchParams();
        params.append('productId', productId);
        params.append('isAjax', 'true');

        return post(API.remove, params).then(function (data) { applyServerResponse(data); return data; });
    }

    function clearCart() {
        const params = new URLSearchParams();
        params.append('isAjax', 'true');

        return post(API.clear, params).then(function (data) { applyServerResponse(data); return data; });
    }

    function addAddon(addonId) {
        const params = new URLSearchParams();
        params.append('addonId', addonId);
        params.append('isAjax', 'true');

        return post(API.addAddon, params).then(function (data) {
            applyServerResponse(data);
            if (data.success) {
                notify('Đã thêm quà tặng', data.message, 'success');
            } else {
                notify('Không thể thêm', data.message, 'error');
            }
            return data;
        });
    }

    function removeAddon(addonId) {
        const params = new URLSearchParams();
        params.append('addonId', addonId);
        params.append('isAjax', 'true');

        return post(API.removeAddon, params).then(function (data) { applyServerResponse(data); return data; });
    }

    function applyVoucher(code) {
        const params = new URLSearchParams();
        params.append('code', (code || '').trim());
        params.append('isAjax', 'true');

        return post(API.voucher, params).then(function (data) {
            applyServerResponse(data);
            if (data.success) {
                notify('Áp dụng thành công', data.message, 'success');
            } else {
                notify('Không thể áp dụng', data.message || 'Mã giảm giá không hợp lệ.', 'error');
            }
            return data;
        });
    }

    function renderCartPage() {
        const container = document.getElementById('cartPageContainer');
        if (!container) return;

        if (!state.items.length && !state.addons.length) {
            container.innerHTML = `
                <div class="empty-cart-state">
                    <div class="empty-cart-icon"><i class="fa-solid fa-basket-shopping"></i></div>
                    <h3 class="font-serif">Giỏ hàng của bạn đang trống</h3>
                    <p>Có vẻ như bạn chưa chọn được bó hoa ưng ý. Hãy khám phá ngay các bộ sưu tập hoa tươi nghệ thuật mới nhất của Hữu Phước Florist!</p>
                    <a href="/Product" class="btn btn-primary btn-lg">
                        <i class="fa-solid fa-seedling"></i> Khám Phá Hoa Tươi Ngay
                    </a>
                </div>`;
            return;
        }

        const merchandise = state.subTotal + state.addonTotal;
        const progress = Math.min(100, Math.round((merchandise / state.freeShippingThreshold) * 100));

        container.innerHTML = `
            <div class="cart-grid-container">
                <div>
                    <div class="free-shipping-card">
                        <div class="free-shipping-card-text">
                            <i class="fa-solid fa-truck-fast"></i>
                            ${progress >= 100
                                ? '<span style="color: #0284C7;">Chúc mừng! Đơn hàng của bạn đã đủ điều kiện <strong>Miễn Phí Giao Hoa Hỏa Tốc</strong></span>'
                                : `<span>Mua thêm <strong>${formatMoney(state.amountToFreeShipping)}</strong> để nhận <strong>Miễn Phí Giao Hoa (Đơn từ ${formatMoney(state.freeShippingThreshold)})</strong></span>`}
                        </div>
                        <div class="fs-progress-bg"><div class="fs-progress-fill" style="width: ${progress}%;"></div></div>
                    </div>

                    <div class="cart-items-wrapper">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; padding-bottom: 12px; border-bottom: 1px solid var(--border);">
                            <span style="font-weight: 700; font-size: 1.05rem; color: var(--dark);">Sản phẩm hoa tươi (${state.items.length})</span>
                            <button type="button" onclick="BloomCart.clearCart()" style="background: none; border: none; color: #EF4444; font-size: 0.85rem; font-weight: 600; cursor: pointer;">
                                <i class="fa-regular fa-trash-can"></i> Xóa tất cả
                            </button>
                        </div>

                        ${state.items.map(function (item) {
                            return `
                            <div class="cart-item-card" id="cart-item-${item.productId}">
                                <img src="${item.image}" alt="${item.name}" class="cart-item-img" />
                                <div class="cart-item-info">
                                    <span class="cart-item-category">Hoa Nghệ Thuật</span>
                                    <a href="/hoa/${item.slug}" class="cart-item-title">${item.name}</a>
                                    <div class="cart-item-price">Đơn giá: <strong>${formatMoney(item.price)}</strong></div>
                                </div>
                                <div class="qty-stepper">
                                    <button type="button" onclick="BloomCart.updateQuantity(${item.productId}, ${item.qty - 1})">-</button>
                                    <span>${item.qty}</span>
                                    <button type="button" onclick="BloomCart.updateQuantity(${item.productId}, ${item.qty + 1})">+</button>
                                </div>
                                <div class="cart-item-subtotal">${formatMoney(item.totalPrice)}</div>
                                <button type="button" class="btn-remove-item" title="Xóa sản phẩm" onclick="BloomCart.removeItem(${item.productId})">
                                    <i class="fa-regular fa-trash-can"></i>
                                </button>
                            </div>`;
                        }).join('')}

                        ${state.addons.length > 0 ? `
                            <div style="margin-top: 20px; padding-top: 16px; border-top: 1px dashed var(--border);">
                                <div style="font-weight: 700; font-size: 0.95rem; color: var(--dark); margin-bottom: 12px;">
                                    <i class="fa-solid fa-gift" style="color: var(--primary);"></i> Quà Tặng Kèm Đã Chọn (${state.addons.length})
                                </div>
                                ${state.addons.map(function (addon) {
                                    return `
                                    <div class="cart-item-card" style="padding: 12px; background: #f8fafc;">
                                        <img src="${addon.image}" alt="${addon.name}" style="width: 54px; height: 54px; border-radius: 8px; object-fit: cover;" />
                                        <div class="cart-item-info">
                                            <div class="cart-item-title" style="font-size: 0.95rem;">${addon.name}</div>
                                            <div class="cart-item-price">Số lượng: ${addon.qty} x ${formatMoney(addon.price)}</div>
                                        </div>
                                        <div class="cart-item-subtotal">${formatMoney(addon.totalPrice)}</div>
                                        <button type="button" class="btn-remove-item" title="Xóa phụ kiện" onclick="BloomCart.removeAddon(${addon.addonId})">
                                            <i class="fa-regular fa-trash-can"></i>
                                        </button>
                                    </div>`;
                                }).join('')}
                            </div>` : ''}
                    </div>

                    <div style="margin-top: 20px;">
                        <a href="/Product" class="btn btn-secondary" style="border-radius: var(--radius-full);">
                            <i class="fa-solid fa-arrow-left"></i> Tiếp Tục Chọn Hoa
                        </a>
                    </div>
                </div>

                <div class="order-summary-card">
                    <h3 class="summary-title font-serif">Tóm Tắt Đơn Hàng</h3>

                    <div class="summary-row">
                        <span>Tổng số lượng hoa:</span>
                        <strong>${state.totalItems} sản phẩm</strong>
                    </div>
                    <div class="summary-row">
                        <span>Tạm tính tiền hoa:</span>
                        <span class="val">${formatMoney(state.subTotal)}</span>
                    </div>
                    ${state.addonTotal > 0 ? `
                        <div class="summary-row"><span>Quà tặng kèm:</span><span class="val">${formatMoney(state.addonTotal)}</span></div>` : ''}
                    <div class="summary-row">
                        <span>Phí vận chuyển:</span>
                        <span class="val" style="${state.shippingFee === 0 ? 'color: #0284C7; font-weight: 700;' : ''}">
                            ${state.shippingFee === 0 ? 'Miễn phí' : formatMoney(state.shippingFee)}
                        </span>
                    </div>
                    ${state.discountAmount > 0 ? `
                        <div class="summary-row" style="color: #0284C7;">
                            <span>Mã giảm giá (${state.voucherCode}):</span>
                            <span class="val" style="color: #0284C7;">-${formatMoney(state.discountAmount)}</span>
                        </div>` : ''}

                    <div style="margin-top: 18px;">
                        <label style="display: block; font-size: 0.85rem; font-weight: 600; margin-bottom: 6px; color: var(--dark);">Mã Ưu Đãi (Voucher):</label>
                        <div class="voucher-form">
                            <input type="text" id="cartVoucherInput" placeholder="Nhập mã giảm giá của bạn" value="${state.voucherCode || ''}" />
                            <button type="button" onclick="BloomCart.applyVoucher(document.getElementById('cartVoucherInput').value)">Áp Dụng</button>
                        </div>
                    </div>

                    <div class="summary-divider"></div>
                    <div class="summary-total-row">
                        <span>Tổng thanh toán:</span>
                        <span class="total-amount">${formatMoney(state.totalAmount)}</span>
                    </div>

                    <a href="/Checkout" class="btn btn-primary" style="width: 100%; padding: 15px; font-size: 1.05rem; margin-bottom: 12px;">
                        Tiến Hành Đặt Hoa <i class="fa-solid fa-arrow-right-long"></i>
                    </a>

                    <div class="cart-guarantee-box">
                        <div class="cart-guarantee-item"><i class="fa-solid fa-shield-halved"></i><span>Bảo mật thanh toán 100% qua SSL 256-bit</span></div>
                        <div class="cart-guarantee-item"><i class="fa-solid fa-camera"></i><span>Nghệ nhân chụp ảnh hoa thật gửi khách duyệt trước khi giao</span></div>
                        <div class="cart-guarantee-item"><i class="fa-solid fa-rotate-left"></i><span>Cam kết hoa tươi tối thiểu 3 - 5 ngày</span></div>
                    </div>
                </div>
            </div>`;
    }

    document.addEventListener('DOMContentLoaded', function () {
        syncFromServer();
    });

    return {
        getCart: function () { return state.items; },
        getTotals: function () { return state; },
        addToCart: addToCart,
        updateQuantity: updateQuantity,
        removeItem: removeItem,
        clearCart: clearCart,
        addAddon: addAddon,
        removeAddon: removeAddon,
        applyVoucher: applyVoucher,
        renderCartPage: renderCartPage,
        updateHeaderBadge: updateHeaderBadge,
        syncFromServer: syncFromServer
    };
})();
