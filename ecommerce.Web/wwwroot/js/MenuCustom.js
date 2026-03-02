

// Mobile detection
function isMobileDevice() {
    return window.innerWidth <= 767;
}

// Show mega menu
function showMegaMenu() {
    if (isMobileDevice()) return;
    
    console.log('showMegaMenu called');
    var megaMenu = document.querySelector('.mega-menu-container');
    
    if (megaMenu) {
        megaMenu.style.setProperty('display', 'block', 'important');
        megaMenu.style.setProperty('opacity', '1', 'important');
        megaMenu.style.setProperty('visibility', 'visible', 'important');
        megaMenu.style.setProperty('pointer-events', 'auto', 'important');
       
    }
}

// Desktop toggle fallback (click)
function toggleMegaMenuDesktop() {
    if (isMobileDevice()) return;
    var megaMenu = document.querySelector('.mega-menu-container');
    if (!megaMenu) return;
    var isHidden = window.getComputedStyle(megaMenu).display === 'none' || megaMenu.style.display === 'none';
    if (isHidden) {
        showMegaMenu();
    } else {
        hideMegaMenu();
    }
}

// Hide mega menu
function hideMegaMenu(force) {
    var megaMenu = document.querySelector('.mega-menu-container');
    if (megaMenu) {
        // Use !important to override hover CSS while pointer is still over elements
        megaMenu.style.setProperty('display', 'none', 'important');
        megaMenu.style.setProperty('opacity', '0', 'important');
        megaMenu.style.setProperty('visibility', 'hidden', 'important');
        megaMenu.style.setProperty('pointer-events', 'none', 'important');

        if (force) {
            document.documentElement.classList.add('mega-force-hide');
            setTimeout(function () {
                document.documentElement.classList.remove('mega-force-hide');
            }, 500);
        }

    
    }
}

// Reset desktop mega menu styles (useful after switching from mobile to desktop)
function resetMegaMenuStyles() {
    var megaMenu = document.querySelector('.mega-menu-container');
    if (megaMenu) {
        megaMenu.style.removeProperty('display');
        megaMenu.style.removeProperty('opacity');
        megaMenu.style.removeProperty('visibility');
        megaMenu.style.removeProperty('pointer-events');
        megaMenu.style.removeProperty('position');
        megaMenu.style.removeProperty('top');
        megaMenu.style.removeProperty('left');
        megaMenu.style.removeProperty('z-index');
    }
    document.documentElement.classList.remove('mega-force-hide');
}

// Prepare mobile modal interactions (idempotent)
function ensureMobileBindings() {
    if (window.innerWidth > 767) return;
    var mobileBtn = document.getElementById('category-button');
    if (!mobileBtn) return;
    if (mobileBtn.getAttribute('data-mobile-bound') === 'true') return;
    mobileBtn.setAttribute('data-mobile-bound', 'true');
    mobileBtn.addEventListener('touchstart', function (e) {
        try { e.preventDefault(); e.stopPropagation(); } catch (_) {}
        setTimeout(function(){ mobileBtn.click(); }, 0);
    }, { passive: false });

    // Also bind click to ensure state toggles on emulated devices
    mobileBtn.addEventListener('click', function(){
        // nothing else; Blazor @onclick handles ToggleCategoryMenu
    });
}

// Show subcategories
function showSubcategories(categoryId) {
    if (isMobileDevice()) return;
    
    console.log('showSubcategories called with categoryId:', categoryId);

    // Hide all subcategories first
    document.querySelectorAll('.mega-subcategories-content').forEach(function(el) {
        el.style.display = 'none';
    });

    // Show specific subcategory
    var target = document.getElementById('mega-subcat-' + categoryId);
    
    if (target) {
        target.style.display = 'block';
        target.style.opacity = '1';
        target.style.visibility = 'visible';
        target.style.pointerEvents = 'auto';

    }
}

// Hide all subcategories
function hideAllSubcategories() {
    document.querySelectorAll('.mega-subcategories-content').forEach(function(el) {
        el.style.display = 'none';
    });

}

// Check hide subcategories
function checkHideSubcategories(categoryId) {
    // Don't hide immediately
}

// Check hide mega menu
function checkHideMegaMenu() {
    // Don't hide immediately
}

// Initialize
document.addEventListener('DOMContentLoaded', function() {
   

    // Close mega menu when any link inside it is clicked
    document.addEventListener('click', function (e) {
        var link = e.target && e.target.closest && e.target.closest('.mega-menu-container a');
        if (link) {
            // Close menu immediately; let navigation proceed naturally
            hideMegaMenu(true);
        }
    }, true);

    // Also close when mouse leaves the whole menu container
    var megaMenu = document.querySelector('.mega-menu-container');
    if (megaMenu) {
        megaMenu.addEventListener('mouseleave', function () {
            hideMegaMenu();
        });
    }

    // Mobile-only: clicking the category button toggles the Blazor modal
    document.addEventListener('click', function (e) {
        var btn = e.target && e.target.closest && e.target.closest('#category-button');
        if (!btn) return;
        if (window.innerWidth <= 767) {
            // Let Blazor @onclick handle ToggleCategoryMenu; no extra JS needed
            // Ensure desktop mega menu stays hidden on mobile
            hideMegaMenu(true);
        } else {
            // Desktop fallback: toggle on click if hover didn't trigger
            e.preventDefault();
            toggleMegaMenuDesktop();
        }
    }, true);

    // Mobile reliability bindings (idempotent)
    ensureMobileBindings();

    // Re-bind when switching between breakpoints without reload
    window.addEventListener('resize', function () {
        if (window.innerWidth >= 768) {
            resetMegaMenuStyles();
            // allow CSS hover to take over immediately
        } else {
            hideMegaMenu(true);
            // remove desktop-only inline styles fully
            var megaMenu = document.querySelector('.mega-menu-container');
            if (megaMenu) {
                megaMenu.removeAttribute('style');
            }
            ensureMobileBindings();
        }
    });

    // Desktop: click outside to close
    document.addEventListener('click', function (e) {
        if (window.innerWidth < 768) return;
        var megaMenu = document.querySelector('.mega-menu-container');
        if (!megaMenu) return;
        var withinMenu = e.target && (megaMenu.contains(e.target) || (document.getElementById('category-button') && document.getElementById('category-button').contains(e.target)));
        if (!withinMenu) {
            hideMegaMenu();
        }
    });
});

// Wishlist mobile: ensure delete/clear buttons always above footer
window.ensureWishlistZ = function() {
    try {
        if (window.innerWidth > 767) return;
        var targets = document.querySelectorAll(
            '.wishlist-section [class*="delete"], .wishlist-section [class*="remove"], .wishlist-section .remove-favorite, .wishlist-section .remove-wishlist, .wishlist-section .wishlist-remove'
        );
        targets.forEach(function(el){
            el.style.position = 'relative';
            el.style.zIndex = '1200';
            el.style.width = el.style.width || '28px';
            el.style.height = el.style.height || '28px';
        });
        var footers = document.querySelectorAll('.mobile-bottom-nav, .bottom-tabbar, .mobile-footer, footer.sticky-bottom');
        footers.forEach(function(f){ f.style.zIndex = '1000'; });
    } catch(e) { console.log('ensureWishlistZ error', e); }
}