// Service Worker — Web Push Bildirimleri
// Push event dinleme, bildirim gösterme ve tıklama yönlendirme

// Service Worker kurulumu
self.addEventListener('install', function (event) {
    // Yeni SW hemen aktif olsun
    self.skipWaiting();
});

// Service Worker aktivasyonu
self.addEventListener('activate', function (event) {
    // Eski cache'leri temizle ve kontrolü hemen al
    event.waitUntil(self.clients.claim());
});

// Push bildirim alındığında
self.addEventListener('push', function (event) {
    if (!event.data) {
        return;
    }

    var data = {};
    try {
        data = event.data.json();
    } catch (e) {
        // JSON parse edilemezse düz metin olarak kullan
        data = {
            title: 'Yeni Bildirim',
            body: event.data.text()
        };
    }

    var title = data.title || 'Yedeksen';
    var options = {
        body: data.body || '',
        icon: data.icon || '/assets/images/logo.png',
        badge: '/assets/images/logo.png',
        data: {
            url: data.url || '/'
        },
        // Bildirim davranış ayarları
        requireInteraction: false,
        silent: false
    };

    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

// Bildirime tıklandığında
self.addEventListener('notificationclick', function (event) {
    event.notification.close();

    // Bildirimde belirtilen URL'ye yönlendir
    var targetUrl = event.notification.data && event.notification.data.url
        ? event.notification.data.url
        : '/';

    event.waitUntil(
        // Açık pencere varsa ona odaklan, yoksa yeni pencere aç
        self.clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(function (clientList) {
                // Aynı origin'de açık pencere var mı kontrol et
                for (var i = 0; i < clientList.length; i++) {
                    var client = clientList[i];
                    if (client.url.indexOf(self.location.origin) !== -1 && 'focus' in client) {
                        client.navigate(targetUrl);
                        return client.focus();
                    }
                }
                // Açık pencere yoksa yeni pencere aç
                if (self.clients.openWindow) {
                    return self.clients.openWindow(targetUrl);
                }
            })
    );
});
