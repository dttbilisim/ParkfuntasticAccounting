/**
 * Plasiyer Konum Takibi — Leaflet.js Harita Modülü
 * OpenStreetMap (ücretsiz) tile layer kullanır
 */
window.salesmanMap = {
    map: null,
    markers: [],
    markerGroup: null,

    /**
     * Haritayı başlat ve marker'ları ekle
     * @param {string} elementId - Harita container element ID
     * @param {Array} salesmen - Plasiyer verileri [{fullName, username, latitude, longitude, lastActive, application}]
     */
    init: function (elementId, salesmen) {
        // Önceki haritayı temizle
        this.dispose();

        var container = document.getElementById(elementId);
        if (!container) return;

        // Türkiye merkezi (varsayılan)
        var defaultCenter = [39.0, 35.0];
        var defaultZoom = 6;

        // Haritayı oluştur
        this.map = L.map(elementId, {
            zoomControl: true,
            scrollWheelZoom: true
        }).setView(defaultCenter, defaultZoom);

        // OpenStreetMap tile layer (ücretsiz)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
            maxZoom: 19
        }).addTo(this.map);

        // Marker grubu oluştur (fit bounds için)
        this.markerGroup = L.featureGroup();

        // Plasiyerleri haritaya ekle
        if (salesmen && salesmen.length > 0) {
            for (var i = 0; i < salesmen.length; i++) {
                var s = salesmen[i];
                this._addMarker(s);
            }

            // Marker grubunu haritaya ekle ve tüm marker'ları gösterecek şekilde zoom yap
            this.markerGroup.addTo(this.map);
            this.map.fitBounds(this.markerGroup.getBounds().pad(0.1));
        }

        // Container boyutu değiştiğinde tile'ların düzgün yüklenmesi için
        var self = this;
        setTimeout(function () {
            if (self.map) self.map.invalidateSize();
        }, 200);
    },

    /**
     * Tek bir plasiyer marker'ı ekle
     */
    _addMarker: function (salesman) {
        // Özel yeşil marker ikonu
        var icon = L.divIcon({
            className: 'salesman-marker',
            html: '<div style="background: #0e947a; color: white; border-radius: 50%; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; font-size: 16px; box-shadow: 0 2px 8px rgba(0,0,0,0.3); border: 2px solid white;">' +
                  '<i class="fa-solid fa-user-tie"></i></div>',
            iconSize: [36, 36],
            iconAnchor: [18, 18],
            popupAnchor: [0, -20]
        });

        var marker = L.marker([salesman.latitude, salesman.longitude], { icon: icon });

        // Popup içeriği
        var popupContent =
            '<div style="min-width: 180px; font-family: inherit;">' +
            '<div style="font-weight: 600; font-size: 14px; margin-bottom: 4px; color: #0e947a;">' +
            '<i class="fa-solid fa-user-tie me-1"></i>' + this._escapeHtml(salesman.fullName) + '</div>' +
            '<div style="font-size: 12px; color: #666; margin-bottom: 2px;">' +
            '<i class="fa-solid fa-at me-1"></i>' + this._escapeHtml(salesman.username) + '</div>' +
            '<div style="font-size: 12px; color: #666; margin-bottom: 2px;">' +
            '<i class="fa-solid fa-clock me-1"></i>' + this._escapeHtml(salesman.lastActive) + '</div>' +
            '<div style="font-size: 12px; color: #666; margin-bottom: 2px;">' +
            '<i class="fa-solid fa-mobile-screen me-1"></i>' + this._escapeHtml(salesman.application) + '</div>' +
            '<div style="font-size: 11px; color: #999; margin-top: 4px;">' +
            '<i class="fa-solid fa-location-dot me-1"></i>' +
            salesman.latitude.toFixed(4) + ', ' + salesman.longitude.toFixed(4) + '</div>' +
            '</div>';

        marker.bindPopup(popupContent);

        // İsim badge'i — marker üstünde her zaman görünür
        marker.bindTooltip(this._escapeHtml(salesman.fullName), {
            permanent: true,
            direction: 'top',
            offset: [0, -22],
            className: 'salesman-name-badge'
        });

        marker.addTo(this.map);
        this.markerGroup.addLayer(marker);
        this.markers.push({ marker: marker, data: salesman });
    },

    /**
     * Belirli bir konuma odaklan ve popup aç
     */
    focusOn: function (lat, lng, username) {
        if (!this.map) return;

        // Önceki rotayı temizle (başka plasiyere geçerken eski rota kalmasın)
        this._clearRoute();

        this.map.setView([lat, lng], 15, { animate: true });

        // Önce tüm popup'ları kapat
        for (var i = 0; i < this.markers.length; i++) {
            this.markers[i].marker.closePopup();
        }

        // Username ile eşleşen marker'ın popup'ını aç
        for (var i = 0; i < this.markers.length; i++) {
            var m = this.markers[i];
            if (m.data.username === username) {
                m.marker.openPopup();
                break;
            }
        }
    },

    /**
     * Önceki rota çizgisi ve marker'larını temizle
     */
    _clearRoute: function () {
        if (this._routeLayer && this.map) {
            this.map.removeLayer(this._routeLayer);
            this._routeLayer = null;
        }
        if (this._routeMarkers) {
            for (var i = 0; i < this._routeMarkers.length; i++) {
                if (this.map) this.map.removeLayer(this._routeMarkers[i]);
            }
        }
        this._routeMarkers = [];
    },

    /**
     * Plasiyerin günlük konum geçmişini haritada polyline rota olarak göster
     * @param {string} userId - Plasiyer userId
     * @param {string} fullName - Plasiyer adı
     * @param {Array} points - Konum noktaları [{lat, lng, ts}]
     */
    showRoute: function (userId, fullName, points) {
        if (!this.map || !points || points.length === 0) return;

        // Önceki rotayı temizle
        this._clearRoute();

        // Polyline koordinatları
        var latlngs = points.map(function (p) { return [p.lat, p.lng]; });

        // Rota çizgisi — yeşil kesikli çizgi
        this._routeLayer = L.polyline(latlngs, {
            color: '#0e947a',
            weight: 4,
            opacity: 0.8,
            dashArray: '10, 6',
            lineJoin: 'round'
        }).addTo(this.map);

        // Başlangıç noktası — yeşil daire
        var startIcon = L.divIcon({
            className: 'route-start-marker',
            html: '<div style="background: #4CAF50; color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; font-size: 11px; font-weight: bold; box-shadow: 0 2px 6px rgba(0,0,0,0.3); border: 2px solid white;">S</div>',
            iconSize: [24, 24],
            iconAnchor: [12, 12]
        });
        var startMarker = L.marker(latlngs[0], { icon: startIcon })
            .bindPopup('<b>' + this._escapeHtml(fullName) + '</b><br>Başlangıç: ' + this._formatTime(points[0].ts))
            .addTo(this.map);
        this._routeMarkers.push(startMarker);

        // Bitiş noktası — kırmızı daire
        var endIcon = L.divIcon({
            className: 'route-end-marker',
            html: '<div style="background: #F44336; color: white; border-radius: 50%; width: 24px; height: 24px; display: flex; align-items: center; justify-content: center; font-size: 11px; font-weight: bold; box-shadow: 0 2px 6px rgba(0,0,0,0.3); border: 2px solid white;">B</div>',
            iconSize: [24, 24],
            iconAnchor: [12, 12]
        });
        var lastPoint = points[points.length - 1];
        var endMarker = L.marker(latlngs[latlngs.length - 1], { icon: endIcon })
            .bindPopup('<b>' + this._escapeHtml(fullName) + '</b><br>Son konum: ' + this._formatTime(lastPoint.ts))
            .addTo(this.map);
        this._routeMarkers.push(endMarker);

        // Haritayı rotaya sığdır
        this.map.fitBounds(this._routeLayer.getBounds().pad(0.1));
    },

    /**
     * Unix timestamp'i saat:dakika formatına dönüştür
     */
    _formatTime: function (ts) {
        var d = new Date(ts * 1000);
        var hours = ('0' + d.getHours()).slice(-2);
        var mins = ('0' + d.getMinutes()).slice(-2);
        return hours + ':' + mins;
    },

    /**
     * Haritayı temizle ve kaynakları serbest bırak
     */
    dispose: function () {
        if (this._routeLayer) {
            this.map.removeLayer(this._routeLayer);
            this._routeLayer = null;
        }
        if (this._routeMarkers) {
            for (var i = 0; i < this._routeMarkers.length; i++) {
                if (this.map) this.map.removeLayer(this._routeMarkers[i]);
            }
            this._routeMarkers = [];
        }
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.markers = [];
        this.markerGroup = null;
    },

    /**
     * XSS koruması için HTML escape
     */
    _escapeHtml: function (text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(text));
        return div.innerHTML;
    }
};
