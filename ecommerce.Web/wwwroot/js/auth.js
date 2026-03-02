window.authLogin = async function (email, password) {
    try {
        const res = await fetch('/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ email, password })
        });
        if (!res.ok) {
            const text = await res.text();
            return { ok: false, message: text || 'Giriş başarısız' };
        }
        return { ok: true };
    } catch (e) {
        return { ok: false, message: e?.message || 'İstek hatası' };
    }
}

window.redirectTo = function (url) {
    try { window.location.replace(url); } catch (_) { window.location.href = url; }
}

// Simple confetti wrapper using canvas-confetti if available
window.confettiBlast = function () {
    try {
        const getOverlayCanvas = () => {
            var id = 'confetti-overlay-canvas';
            var canvas = document.getElementById(id);
            if (!canvas) {
                var container = document.createElement('div');
                container.id = 'confetti-overlay-container';
                container.style.position = 'fixed';
                container.style.left = '0';
                container.style.top = '0';
                container.style.width = '100vw';
                container.style.height = '100vh';
                container.style.pointerEvents = 'none';
                container.style.zIndex = '2147483647';
                canvas = document.createElement('canvas');
                canvas.id = id;
                canvas.style.width = '100%';
                canvas.style.height = '100%';
                // set actual pixel size so confetti is visible
                canvas.width = window.innerWidth;
                canvas.height = window.innerHeight;
                container.appendChild(canvas);
                document.body.appendChild(container);
                // keep canvas sized on resize
                window.addEventListener('resize', function () {
                    try {
                        var c = document.getElementById(id);
                        if (!c) return;
                        c.width = window.innerWidth;
                        c.height = window.innerHeight;
                    } catch (_) { }
                }, { passive: true });
            }
            return canvas;
        };

        const cssFallback = () => {
            try {
                var styleId = 'confetti-fallback-style';
                if (!document.getElementById(styleId)) {
                    var style = document.createElement('style');
                    style.id = styleId;
                    style.innerHTML = '@keyframes confettiFall{0%{transform:translateY(-100vh) rotate(0)}100%{transform:translateY(100vh) rotate(720deg)}} .confetti-piece{position:fixed;top:-10px;left:0;width:8px;height:14px;opacity:.9;will-change:transform;animation:confettiFall 2.2s linear forwards;z-index:2147483647}';
                    document.head.appendChild(style);
                }
                for (var i = 0; i < 80; i++) {
                    var d = document.createElement('div');
                    d.className = 'confetti-piece';
                    d.style.backgroundColor = ['#fce18a', '#ff726d', '#b48def', '#f4306d'][i % 4];
                    d.style.left = (Math.random() * 100) + 'vw';
                    d.style.animationDelay = (Math.random() * 0.3) + 's';
                    d.style.transform = 'translateY(-100vh)';
                    d.style.width = (6 + Math.random() * 6) + 'px';
                    d.style.height = (10 + Math.random() * 10) + 'px';
                    document.body.appendChild(d);
                    setTimeout((el) => { el && el.remove && el.remove(); }, 2300, d);
                }
            } catch (e) { if (window.console && console.warn) console.warn(e); }
        };

        const fire = () => {
            try {
                if (typeof window.confetti === 'function') {
                    var canvas = getOverlayCanvas();
                    var myConfetti = window.confetti.create(canvas, { resize: true, useWorker: false });
                    // More explicit, larger, and numerous confetti
                    const defaults = { startVelocity: 55, ticks: 300, gravity: 0.9, scalar: 1.3, origin: { y: 0.2 } };
                    myConfetti(Object.assign({}, defaults, { particleCount: 250, spread: 120 }));
                    setTimeout(() => myConfetti(Object.assign({}, defaults, { particleCount: 200, spread: 100 })), 300);
                    setTimeout(() => myConfetti(Object.assign({}, defaults, { particleCount: 150, spread: 140, scalar: 1.5, startVelocity: 65, origin: { y: 0.4 } })), 600);
                } else { cssFallback(); }
            } catch (e) { cssFallback(); }
        };

        // Ensure overlay exists up-front so kontrollerde null çıkmasın
        getOverlayCanvas();

        if (window.confetti) {
            fire();
            return true;
        }

        // dynamically load canvas-confetti if missing, but do NOT fire automatically
        var existing = document.querySelector('script[data-confetti="1"]');
        if (existing) {
            // Script is loading or already loaded; wait and fire when ready
            var waitTimer = setInterval(function () {
                if (typeof window.confetti === 'function') {
                    clearInterval(waitTimer);
                    fire();
                }
            }, 50);
            setTimeout(function () { clearInterval(waitTimer); }, 2000); // cleanup after 2s
            return true;
        }
        var s = document.createElement('script');
        s.src = 'https://cdn.jsdelivr.net/npm/canvas-confetti@1.9.3/dist/confetti.browser.min.js';
        s.async = true;
        s.defer = true;
        s.setAttribute('data-confetti', '1');
        s.onload = fire;
        document.head.appendChild(s);
        return true;
    } catch (e) { if (window.console && console.warn) console.warn(e); return false; }
}

// Retry-based wrapper to ensure confetti fires when libs and DOM are ready
window.confettiEnsureBlast = function () {
    try {
        var attempts = 0;
        var maxAttempts = 20; // ~2s total
        var timer = setInterval(function () {
            attempts++;
            if (typeof window.confetti === 'function') {
                clearInterval(timer);
                window.confettiBlast();
            }
            if (attempts >= maxAttempts) { clearInterval(timer); }
        }, 100);
        // also kick an immediate attempt
        return window.confettiBlast();
    } catch (e) { if (window.console && console.warn) console.warn(e); return false; }
}

// ===== MEGA MENU - CSS ONLY =====
// Completely removed JavaScript for maximum stability