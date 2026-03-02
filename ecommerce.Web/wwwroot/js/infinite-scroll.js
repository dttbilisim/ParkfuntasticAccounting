// Infinite Scroll for Order List
window.initInfiniteScroll = (dotNetHelper) => {
    console.log('🔄 Infinite scroll initializing...');

    // Generate a unique ID for this initialization attempt
    const searchId = Date.now();
    window.currentSearchId = searchId;

    // Cleanup previous observer if exists
    if (window.orderScrollObserver) {
        console.log('🧹 Cleaning up previous observer');
        window.orderScrollObserver.disconnect();
        window.orderScrollObserver = null;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            console.log('👀 Observer triggered, isIntersecting:', entry.isIntersecting);
            if (entry.isIntersecting) {
                console.log('✅ Loading more orders...');
                // Trigger load more
                dotNetHelper.invokeMethodAsync('OnScrollTrigger')
                    .then(() => console.log('✅ OnScrollTrigger completed'))
                    .catch(err => console.error('❌ OnScrollTrigger failed:', err));
            }
        });
    }, {
        root: null,
        rootMargin: '100px', // Trigger 100px before reaching element
        threshold: 0.1
    });

    // Observe the trigger element
    let retryCount = 0;
    const maxRetries = 10; // Stop after 5 seconds

    const observeTarget = () => {
        // If a new initialization has started, stop this loop
        if (window.currentSearchId !== searchId) {
            console.log('🛑 Search loop aborted, newer initialization detected');
            return;
        }

        const target = document.getElementById('loadMoreObserver');
        if (target) {
            console.log('✅ Observer target found, observing...');
            observer.observe(target);
            window.orderScrollObserver = observer;
        } else {
            retryCount++;
            if (retryCount > maxRetries) {
                console.warn('⚠️ Observer target not found after multiple attempts. Giving up.');
                return;
            }
            console.log(`⚠️ Observer target not found, retrying (${retryCount}/${maxRetries})...`);
            // Retry after a short delay if element not found
            setTimeout(observeTarget, 500);
        }
    };

    observeTarget();
};

window.resetInfiniteScroll = () => {
    console.log('🔄 Resetting infinite scroll observer...');
    if (window.orderScrollObserver) {
        window.orderScrollObserver.disconnect();
        window.orderScrollObserver = null;
    }

};
