/**
 * FLOWER BOUTIQUE - MAIN JAVASCRIPT
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Off-Canvas Cart Drawer
    const cartToggleBtns = document.querySelectorAll('.js-cart-toggle');
    const cartDrawer = document.getElementById('cartDrawer');
    const cartOverlay = document.getElementById('cartOverlay');
    const cartCloseBtn = document.getElementById('cartCloseBtn');

    function openCartDrawer() {
        if (cartDrawer && cartOverlay) {
            cartDrawer.classList.add('active');
            cartOverlay.classList.add('active');
            loadMiniCart();
        }
    }

    function closeCartDrawer() {
        if (cartDrawer && cartOverlay) {
            cartDrawer.classList.remove('active');
            cartOverlay.classList.remove('active');
        }
    }

    cartToggleBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            openCartDrawer();
        });
    });

    if (cartCloseBtn) cartCloseBtn.addEventListener('click', closeCartDrawer);
    if (cartOverlay) cartOverlay.addEventListener('click', closeCartDrawer);

    // 2. Load Mini Cart Content
    function loadMiniCart() {
        const miniCartContent = document.getElementById('miniCartBody');
        if (!miniCartContent) return;

        fetch('/Cart/GetMiniCart')
            .then(res => res.text())
            .then(html => {
                miniCartContent.innerHTML = html;
            })
            .catch(err => console.error('Error loading mini cart:', err));
    }

    // 3. Quick Add to Cart with AJAX & BloomCart Sync
    const quickAddBtns = document.querySelectorAll('.js-quick-add');
    quickAddBtns.forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const productId = this.getAttribute('data-id');
            const qty = this.getAttribute('data-qty') || 1;

            if (typeof BloomCart !== 'undefined') {
                const card = this.closest('.product-card');
                const title = card ? (card.querySelector('.product-title a') || card.querySelector('.product-title')).innerText.trim() : 'Bó Hoa Tươi Nghệ Thuật';
                const img = card ? (card.querySelector('.product-thumb img') || {}).src : '';
                const priceText = card ? (card.querySelector('.product-price') || {}).innerText.replace(/[^\d]/g, '') : '850000';

                BloomCart.addToCart({
                    productId: parseInt(productId, 10),
                    name: title,
                    image: img,
                    price: parseInt(priceText, 10) || 850000
                }, parseInt(qty, 10) || 1);
            }

            const formData = new FormData();
            formData.append('productId', productId);
            formData.append('quantity', qty);
            formData.append('isAjax', 'true');

            fetch('/Cart/AddToCart', {
                method: 'POST',
                body: formData
            })
            .then(res => res.json())
            .then(data => {
                if (data && data.success) {
                    const badge = document.querySelector('.js-cart-count');
                    if (badge) badge.textContent = data.totalItems;
                }
            })
            .catch(err => console.log('Cart backend sync:', err));
        });
    });

    // 4. Live AJAX Search with Debounce
    const searchInput = document.getElementById('headerSearchInput');
    const searchDropdown = document.getElementById('searchResultsDropdown');
    let searchTimeout = null;

    if (searchInput && searchDropdown) {
        searchInput.addEventListener('input', function () {
            clearTimeout(searchTimeout);
            const query = this.value.trim();

            if (query.length < 2) {
                searchDropdown.style.display = 'none';
                searchDropdown.innerHTML = '';
                return;
            }

            searchTimeout = setTimeout(() => {
                fetch(`/Product/SearchAjax?q=${encodeURIComponent(query)}`)
                    .then(res => res.json())
                    .then(res => {
                        if (res.success && res.items.length > 0) {
                            let html = '<div style="padding: 10px 0;">';
                            res.items.forEach(item => {
                                html += `
                                    <a href="/hoa/${item.slug}" style="display: flex; align-items: center; gap: 12px; padding: 10px 16px; border-bottom: 1px solid #f2e9eb; text-decoration: none;">
                                        <img src="${item.image}" style="width: 44px; height: 44px; border-radius: 8px; object-fit: cover;" />
                                        <div>
                                            <div style="font-weight: 600; font-size: 0.88rem; color: #2e2426;">${item.name}</div>
                                            <div style="color: #c84b61; font-weight: 700; font-size: 0.82rem;">${new Intl.NumberFormat('vi-VN').format(item.price)} đ</div>
                                        </div>
                                    </a>
                                `;
                            });
                            html += '</div>';
                            searchDropdown.innerHTML = html;
                            searchDropdown.style.display = 'block';
                        } else {
                            searchDropdown.innerHTML = '<div style="padding: 16px; text-align: center; color: #888; font-size: 0.85rem;">Không tìm thấy mẫu hoa phù hợp</div>';
                            searchDropdown.style.display = 'block';
                        }
                    });
            }, 300);
        });

        document.addEventListener('click', function (e) {
            if (!searchInput.contains(e.target) && !searchDropdown.contains(e.target)) {
                searchDropdown.style.display = 'none';
            }
        });
    }

    // 5. Flash Sale Countdown Timer
    const countdownHours = document.getElementById('cdHours');
    const countdownMins = document.getElementById('cdMins');
    const countdownSecs = document.getElementById('cdSecs');

    if (countdownHours && countdownMins && countdownSecs) {
        let totalSeconds = 6 * 3600 + 42 * 60 + 15; // 6h 42m 15s

        setInterval(() => {
            if (totalSeconds > 0) {
                totalSeconds--;
                const h = Math.floor(totalSeconds / 3600);
                const m = Math.floor((totalSeconds % 3600) / 60);
                const s = totalSeconds % 60;

                countdownHours.textContent = String(h).padStart(2, '0');
                countdownMins.textContent = String(m).padStart(2, '0');
                countdownSecs.textContent = String(s).padStart(2, '0');
            }
        }, 1000);
    }
});
