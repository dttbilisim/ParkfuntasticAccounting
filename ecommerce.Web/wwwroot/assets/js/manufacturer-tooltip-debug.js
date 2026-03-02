// Manufacturer Tooltip - Pozisyon ayarla
window.debugManufacturerTooltip = function() {
    const items = document.querySelectorAll('.manufacturer-item');
    console.log(`✅ Found ${items.length} manufacturer items`);
    
    items.forEach((item, index) => {
        const span = item.querySelector('span');
        
        if (!span) return;
        
        item.addEventListener('mouseenter', function(e) {
            const rect = item.getBoundingClientRect();
            span.style.left = (rect.left + rect.width / 2) + 'px';
            span.style.top = (rect.bottom + 8) + 'px';
            span.style.transform = 'translateX(-50%)';
        });
    });
    
    console.log('✅ Tooltip position handler initialized');
};

