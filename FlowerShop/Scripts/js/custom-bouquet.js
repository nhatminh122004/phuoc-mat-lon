/**
 * FLOWER BOUTIQUE - CUSTOM BOUQUET BUILDER
 */

document.addEventListener('DOMContentLoaded', function () {
    const wrapOptions = document.querySelectorAll('input[name="wrapStyle"]');
    const flowerRadios = document.querySelectorAll('input[name="mainFlowerId"]');
    const qtyInput = document.getElementById('mainFlowerQty');
    const priceDisplay = document.getElementById('customBouquetPrice');

    function calculatePrice() {
        if (!priceDisplay) return;

        let wrapCost = 80000;
        const selectedWrap = document.querySelector('input[name="wrapStyle"]:checked');
        if (selectedWrap) {
            if (selectedWrap.value === 'GioHoa') wrapCost = 150000;
            if (selectedWrap.value === 'HopMica') wrapCost = 250000;
        }

        const qty = parseInt(qtyInput ? qtyInput.value : 19, 10) || 19;
        const unitFlowerCost = 25000;

        const total = wrapCost + (qty * unitFlowerCost);
        priceDisplay.textContent = new Intl.NumberFormat('vi-VN').format(total) + ' đ';
    }

    wrapOptions.forEach(opt => opt.addEventListener('change', calculatePrice));
    flowerRadios.forEach(opt => opt.addEventListener('change', calculatePrice));
    if (qtyInput) qtyInput.addEventListener('input', calculatePrice);

    // Initial calculation
    calculatePrice();
});
