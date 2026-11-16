/**
 * FLOWER BOUTIQUE - CUSTOM BOUQUET BUILDER (server-driven pricing)
 * -----------------------------------------------------------------------------
 * Giá được server quyết định: form #customBouquetForm có data-attributes
 * (data-wrap-costs, data-stem-unit-price) do CustomBouquetController render.
 * Client chỉ mô phỏng lại đúng công thức đó để hiển thị tức thời;
 * giá chính thức vẫn do server tính lại khi thêm vào giỏ (AddToCart).
 */
document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('customBouquetForm');
    const qtyInput = document.getElementById('mainFlowerQty');
    const priceDisplay = document.getElementById('customBouquetPrice');
    const unitPriceHint = document.getElementById('customBouquetUnitPrice');

    if (!form || !priceDisplay) return;

    const wrapCosts = parseJsonMap(form.dataset.wrapCosts);
    const defaultStemUnitPrice = parseFloat(form.dataset.stemUnitPrice || '0') || 25000;

    function parseJsonMap(raw) {
        try {
            return raw ? JSON.parse(raw) : {};
        } catch (e) {
            return {};
        }
    }

    function formatMoney(amount) {
        return new Intl.NumberFormat('vi-VN').format(Math.round(amount)) + ' đ';
    }

    function selectedWrapStyle() {
        const checked = form.querySelector('input[name="wrapStyle"]:checked');
        return checked ? checked.value : 'BoTron';
    }

    function selectedFlowerUnitPrice() {
        const checked = form.querySelector('input[name="mainFlowerId"]:checked');
        if (!checked) return defaultStemUnitPrice;

        const price = parseFloat(checked.dataset.unitPrice || '0');
        return price > 0 ? price : defaultStemUnitPrice;
    }

    function calculatePrice() {
        const wrapCost = parseFloat(wrapCosts[selectedWrapStyle()] || 0);
        const stemUnitPrice = selectedFlowerUnitPrice();
        const qty = parseInt(qtyInput ? qtyInput.value : 19, 10) || 19;

        const total = wrapCost + (qty * stemUnitPrice);

        priceDisplay.textContent = formatMoney(total);

        if (unitPriceHint) {
            unitPriceHint.textContent = formatMoney(stemUnitPrice) + ' / cành · ' + formatMoney(wrapCost) + ' phí dáng cắm';
        }
    }

    form.querySelectorAll('input[name="wrapStyle"], input[name="mainFlowerId"]').forEach(function (input) {
        input.addEventListener('change', calculatePrice);
    });

    if (qtyInput) {
        qtyInput.addEventListener('input', calculatePrice);
        qtyInput.addEventListener('change', calculatePrice);
    }

    calculatePrice();
});
