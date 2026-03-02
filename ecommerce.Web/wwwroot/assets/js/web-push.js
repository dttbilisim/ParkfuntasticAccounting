// Web Push Bildirimleri — Service Worker kayıt, izin isteme, subscription oluşturma

(function () {
    'use strict';

    // VAPID public key — appsettings.json'daki değerle eşleşmeli
    // Bu değer sunucu tarafından sayfaya enjekte edilecek veya burada sabit olarak tanımlanacak
    var VAPID_PUBLIC_KEY = window.__VAPID_PUBLIC_KEY__ || '';

    // Backend API endpoint'i
    var REGISTER_TOKEN_URL = '/api/Notification/register-token';

    // Tarayıcı desteği kontrolü
    function isPushSupported() {
        return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
    }

    // Base64 URL string'i Uint8Array'e çevir (VAPID key için gerekli)
    function urlBase64ToUint8Array(base64String) {
        var padding = '='.repeat((4 - base64String.length % 4) % 4);
        var base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');
        var rawData = window.atob(base64);
        var outputArray = new Uint8Array(rawData.length);
        for (var i = 0; i < rawData.length; i++) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }

    // Benzersiz tarayıcı ID oluştur (localStorage'da sakla)
    function getBrowserDeviceId() {
        var key = 'web_push_device_id';
        var deviceId = localStorage.getItem(key);
        if (!deviceId) {
            // Rastgele benzersiz ID oluştur
            deviceId = 'web_' + Date.now().toString(36) + '_' + Math.random().toString(36).substring(2, 10);
            localStorage.setItem(key, deviceId);
        }
        return deviceId;
    }

    // Service Worker'ı kaydet
    async function registerServiceWorker() {
        try {
            var registration = await navigator.serviceWorker.register('/sw.js');
            console.log('Service Worker kaydedildi:', registration.scope);
            return registration;
        } catch (error) {
            console.error('Service Worker kayıt hatası:', error);
            return null;
        }
    }

    // Bildirim izni iste
    async function requestNotificationPermission() {
        var permission = await Notification.requestPermission();
        return permission === 'granted';
    }

    // Push subscription oluştur
    async function subscribeToPush(registration) {
        try {
            // Mevcut subscription var mı kontrol et
            var existingSubscription = await registration.pushManager.getSubscription();
            if (existingSubscription) {
                return existingSubscription;
            }

            // Yeni subscription oluştur
            var subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY)
            });

            console.log('Push subscription oluşturuldu.');
            return subscription;
        } catch (error) {
            console.error('Push subscription hatası:', error);
            return null;
        }
    }

    // Subscription'ı backend'e kaydet
    async function sendSubscriptionToServer(subscription) {
        try {
            var subscriptionJson = JSON.stringify(subscription);
            var deviceId = getBrowserDeviceId();

            var response = await fetch(REGISTER_TOKEN_URL, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    token: subscriptionJson,
                    platform: 'web',
                    deviceId: deviceId
                })
            });

            if (response.ok) {
                console.log('Web push subscription backend\'e kaydedildi.');
            } else {
                console.warn('Subscription kayıt yanıtı:', response.status);
            }
        } catch (error) {
            console.error('Subscription backend\'e kaydedilemedi:', error);
        }
    }

    // Ana başlatma fonksiyonu
    async function initWebPush() {
        // Tarayıcı desteği kontrolü
        if (!isPushSupported()) {
            console.log('Bu tarayıcı web push bildirimlerini desteklemiyor.');
            return;
        }

        // VAPID key kontrolü
        if (!VAPID_PUBLIC_KEY) {
            console.warn('VAPID public key tanımlanmamış. Web push devre dışı.');
            return;
        }

        // Daha önce reddedilmişse tekrar sorma
        if (Notification.permission === 'denied') {
            console.log('Bildirim izni daha önce reddedilmiş.');
            return;
        }

        // Service Worker kaydet
        var registration = await registerServiceWorker();
        if (!registration) {
            return;
        }

        // Service Worker hazır olana kadar bekle
        await navigator.serviceWorker.ready;

        // İzin iste (henüz verilmemişse)
        if (Notification.permission === 'default') {
            var granted = await requestNotificationPermission();
            if (!granted) {
                console.log('Kullanıcı bildirim iznini reddetti.');
                return;
            }
        }

        // Push subscription oluştur
        var subscription = await subscribeToPush(registration);
        if (!subscription) {
            return;
        }

        // Backend'e kaydet
        await sendSubscriptionToServer(subscription);
    }

    // Sayfa yüklendiğinde başlat (küçük gecikme ile kullanıcı deneyimini bozmamak için)
    if (document.readyState === 'complete') {
        setTimeout(initWebPush, 3000);
    } else {
        window.addEventListener('load', function () {
            setTimeout(initWebPush, 3000);
        });
    }
})();
