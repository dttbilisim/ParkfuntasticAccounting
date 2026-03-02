// Manufacturer Slider - Manual Horizontal Scroll Only (No Auto-Play)
window.initManufacturerSlider = function() {
    var el = document.getElementById('manufacturerSlider');
    if (!el) return;
    
    // Guard: Prevent duplicate bindings
    if (el.__scrollBound) return;
    el.__scrollBound = true;

    // Mouse wheel → horizontal scroll (yumuşak ve kolay)
    el.addEventListener('wheel', function(ev){
        // Dikey scroll'u yataya çevir
        if (Math.abs(ev.deltaY) > 0) {
            el.scrollLeft += ev.deltaY * 0.5; // Yavaş ve kontrollü
            ev.preventDefault();
        }
    }, { passive: false });

    // Drag to scroll - süper yumuşak
    var isDown = false, startX = 0, scrollLeft = 0;
    
    el.addEventListener('mousedown', function(e){
        // Sadece logo üzerinde değilse drag başlat
        if(e.target.closest('.manufacturer-item')) {
            return; // Logo tıklamasını engelleme
        }
        isDown = true;
        el.style.cursor = 'grabbing';
        el.style.userSelect = 'none';
        startX = e.pageX - el.offsetLeft;
        scrollLeft = el.scrollLeft;
    });
    
    window.addEventListener('mouseup', function(){ 
        isDown = false; 
        el.style.cursor = 'grab';
        el.style.userSelect = '';
    });
    
    el.addEventListener('mouseleave', function(){ 
        isDown = false; 
        el.style.cursor = 'grab';
        el.style.userSelect = '';
    });
    
    el.addEventListener('mousemove', function(e){
        if(!isDown) return;
        e.preventDefault();
        var x = e.pageX - el.offsetLeft;
        var walk = (x - startX) * 2; 
        el.scrollLeft = scrollLeft - walk;
    });

    // Touch support for mobile/tablet
    var touchStartX = 0;
    var touchScrollLeft = 0;
    
    el.addEventListener('touchstart', function(e){
        touchStartX = e.touches[0].pageX - el.offsetLeft;
        touchScrollLeft = el.scrollLeft;
    });
    
    el.addEventListener('touchmove', function(e){
        var x = e.touches[0].pageX - el.offsetLeft;
        var walk = (x - touchStartX) * 2;
        el.scrollLeft = touchScrollLeft - walk;
    });

   
};

