/**
 * SIMPLE & RELIABLE Infinite Scroll for Blazor SSR
 * Uses pure scroll event - no IntersectionObserver complexity
 */

let isLoading = false;
let scrollHandler = null;

window.setupInfiniteScroll = function (sentinelElement, dotNetRef) {
    // Cleanup previous handler
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler);
    }
    
    // Track scroll for direction detection
    let lastScrollTop = 0;
    let scrollMovementSum = 0; // Track total scroll movement
    
    // Simple scroll event with throttle
    let scrollTimeout = null;
    scrollHandler = function() {
        // Throttle: only check every 300ms
        if (scrollTimeout) return;
        
        scrollTimeout = setTimeout(() => {
            scrollTimeout = null;
            
            // Detect scroll direction and movement
            const currentScrollTop = window.scrollY || document.documentElement.scrollTop;
            const scrollDelta = currentScrollTop - lastScrollTop;
            
            // Track accumulated downward movement
            if (scrollDelta > 0) {
                scrollMovementSum += scrollDelta;
            } else {
                scrollMovementSum = 0; // Reset if scrolling up
            }
            
            lastScrollTop = currentScrollTop;
            
            // Simple check: are we near the bottom?
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.scrollHeight;
            const distanceToBottom = documentHeight - (currentScrollTop + windowHeight);
            
            // Mobile vs Desktop thresholds
            const isMobile = window.innerWidth <= 767;
            const threshold = isMobile ? 150 : 400;
            const minScrollMovement = isMobile ? 100 : 50; // Mobile: must scroll 100px down
            
            // Only trigger when:
            // 1. Near bottom (within threshold)
            // 2. Not already loading
            // 3. Scrolled DOWN enough (not just jitter)
            if (distanceToBottom < threshold && !isLoading && scrollMovementSum > minScrollMovement) {
                isLoading = true;
                scrollMovementSum = 0; // Reset movement counter
                
                dotNetRef.invokeMethodAsync('LoadMoreProducts')
                    .then(() => {
                        // Mobile needs more cooldown time
                        const cooldown = isMobile ? 2000 : 1000;
                        setTimeout(() => { isLoading = false; }, cooldown);
                    })
                    .catch(err => {
                        console.error('⚠️ Infinite scroll error:', err);
                        isLoading = false;
                    });
            }
        }, 300);
    };
    
    // Add scroll listener
    window.addEventListener('scroll', scrollHandler, { passive: true });
};

// Cleanup
window.cleanupInfiniteScroll = function () {
    if (scrollHandler) {
        window.removeEventListener('scroll', scrollHandler);
        scrollHandler = null;
        isLoading = false;
    }
};