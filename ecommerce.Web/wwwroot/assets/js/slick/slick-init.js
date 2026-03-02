window.observeAndInitSlick = (element, config, options = {}) => {
   

    const observerOptions = {
        root: null,
        rootMargin: '0px',
        threshold: 0,
        ...options
    };

    const immediate = !!options.immediate;

    const getElement = () => {
        if (typeof element === 'string') return document.querySelector(element);
        if (element instanceof Element) return element;
        return null;
    };

    const tryInitialize = () => {
        const targetElement = getElement();
        if (!targetElement) {
            console.warn("❌ tryInitialize: Hedef element DOM'da bulunamadı:", element);
            return;
        }

        const $el = $(targetElement);
        // Sadece banner slider için min-height kullan, diğerleri için kullanma (boşluk sorunu yaratıyor)
        const isBannerSlider = targetElement.id === 'slider-animate';
        if (window.innerWidth > 767 && isBannerSlider) {
            $el.css("min-height", "300px");
        }
        if ($el.length) {
            if ($el.hasClass('slick-initialized')) {
               
                return;
            }
            // Wait for images but don't block forever; handle errors and add fallback
            const images = $el.find("img");
            if (images.length) {
                let loadedCount = 0;
                const total = images.length;

                const finalizeInit = () => {
                    if ($el.hasClass('slick-initialized')) return;
                    $el.slick(config);
                    $el.removeClass("slick-container-initializing").addClass("slick-container-ready");
                    $el.css("min-height", "");
                    setTimeout(() => {
                        $el.css({'height': 'auto', 'min-height': '0', 'max-height': 'none'});
                        $el.find('.slick-list, .slick-track').css({'height': 'auto', 'min-height': '0'});
                    }, 100);
                };

                // Fallback: initialize even if some images fail to load
                const MAX_WAIT_MS = 1500;
                const fallbackTimer = setTimeout(finalizeInit, MAX_WAIT_MS);

                const markOneDone = () => {
                    loadedCount++;
                    if (loadedCount >= total) {
                        clearTimeout(fallbackTimer);
                        finalizeInit();
                    }
                };

                images.each(function () {
                    const img = this;
                    if (img.complete) {
                        markOneDone();
                    } else {
                        $(img)
                            .one("load", markOneDone)
                            .one("error", markOneDone); // count failures too
                    }
                });
            } else {
                $el.slick(config);
                $el.removeClass("slick-container-initializing").addClass("slick-container-ready");
                $el.css("min-height", "");
                
                // Height fix: force reset heights (tüm ekran boyutları)
                setTimeout(() => {
                    $el.css({'height': 'auto', 'min-height': '0', 'max-height': 'none'});
                    $el.find('.slick-list, .slick-track').css({'height': 'auto', 'min-height': '0'});
                }, 100);
            }
            return;
        } else {
            console.warn("⚠️ Slick zaten başlatılmış ya da hedef element yok.");
        }
    };

    // Immediate init: useful for Blazor SSR to avoid initial blank space
    if (immediate) {
        requestAnimationFrame(() => tryInitialize());
        return;
    }

    if (!('IntersectionObserver' in window)) {
        requestAnimationFrame(() => setTimeout(tryInitialize, 50));
        return;
    }

    const observer = new IntersectionObserver((entries, obs) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                requestAnimationFrame(() => setTimeout(tryInitialize, 100));
                obs.disconnect();
                break;
            }
        }
    }, observerOptions);

    const target = getElement();
    if (target) {
        observer.observe(target);
    } else {
        console.warn("❌ IntersectionObserver: Hedef DOM'da mevcut değil:", element);
    }
};

// GLOBAL FIX - Force all slick sliders to auto height (tüm ekran boyutları)
$(document).ready(function() {
    setTimeout(function() {
        // Category ve Product section'larındaki slider'ları hedefle
        $('.category-section-3 .slick-slider, .category-section-3 .slick-initialized, .product-section-3 .slick-slider, .product-section-3 .slick-initialized').each(function() {
            $(this).css({'height': 'auto', 'min-height': '0', 'max-height': 'none', 'margin-top': '0', 'margin-bottom': '0'});
            $(this).find('.slick-list, .slick-track').css({'height': 'auto', 'min-height': '0', 'margin-top': '0', 'margin-bottom': '0'});
            
            // CRITICAL: Force all individual slides to auto height with setProperty
            $(this).find('.slick-slide').each(function() {
                this.style.setProperty('height', 'auto', 'important');
                this.style.setProperty('min-height', '0', 'important');
                this.style.setProperty('max-height', 'none', 'important');
                // Also fix any child divs inside slides
                $(this).find('> div').each(function() {
                    this.style.setProperty('height', 'auto', 'important');
                    this.style.setProperty('min-height', '0', 'important');
                });
            });
        });
       
    }, 1000);
    
    // Double check with another timeout to catch late-loaded sliders
    setTimeout(function() {
        $('.category-section-3 .slick-slide, .product-section-3 .slick-slide').each(function() {
            if ($(this).height() > 1000) {
                console.warn('⚠️ Found oversized slide:', $(this).height() + 'px', 'Fixing...');
                // Use setProperty to force !important
                this.style.setProperty('height', 'auto', 'important');
                this.style.setProperty('min-height', '0', 'important');
                this.style.setProperty('max-height', 'none', 'important');
                // Fix child divs
                $(this).find('> div').each(function() {
                    this.style.setProperty('height', 'auto', 'important');
                    this.style.setProperty('min-height', '0', 'important');
                });
            }
        });
    }, 2000);
});