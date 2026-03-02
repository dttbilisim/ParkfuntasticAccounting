window.initPassiveListeners = () => {
    const passiveEvents = ['touchstart', 'touchmove', 'wheel'];

    passiveEvents.forEach(eventName => {
        document.addEventListener(eventName, () => {}, { passive: true });
    });

    console.log("✅ Passive scroll event listeners initialized.");
};