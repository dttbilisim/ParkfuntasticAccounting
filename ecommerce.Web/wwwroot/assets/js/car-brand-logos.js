window.carBrandLogos = {
    CACHE_KEY: 'car_brand_logos_v2', // Changed key to invalidate old cache
    CACHE_EXPIRY_DAYS: 7,
    logos: {},

    init: async function () {
        await this.loadLogos();
    },

    loadLogos: async function () {
        // Try to load from cache first
        const cachedData = localStorage.getItem(this.CACHE_KEY);
        if (cachedData) {
            try {
                const parsed = JSON.parse(cachedData);
                const now = new Date().getTime();
                // Check if cache is valid (not expired)
                if (parsed.timestamp && (now - parsed.timestamp < this.CACHE_EXPIRY_DAYS * 24 * 60 * 60 * 1000)) {
                    this.logos = parsed.data;
                    console.log('Loaded car brand logos from cache');
                    return;
                }
            } catch (e) {
                console.error('Error parsing cached logos', e);
            }
        }

        // Fetch from API if cache is missing or expired
        try {
            const response = await fetch('/api/ManufacturerLogos');
            if (response.ok) {
                const data = await response.json();
                this.logos = data;

                // Save to cache
                const cacheObject = {
                    timestamp: new Date().getTime(),
                    data: data
                };
                localStorage.setItem(this.CACHE_KEY, JSON.stringify(cacheObject));
                console.log('Fetched and cached car brand logos from API');
            } else {
                console.error('Failed to fetch car brand logos from API');
            }
        } catch (e) {
            console.error('Error fetching car brand logos', e);
        }
    },

    getLogoUrl: function (brandName) {
        if (!brandName) return null;
        // Case-insensitive lookup
        const key = Object.keys(this.logos).find(k => k.toLowerCase() === brandName.toLowerCase());
        return key ? this.logos[key] : null;
    },

    // Legacy support for existing calls (if any)
    loadLogo: function (brandName, type, elementId) {
        const url = this.getLogoUrl(brandName);
        const element = document.getElementById(elementId);
        if (element) {
            if (url) {
                element.innerHTML = `<img src="${url}" alt="${brandName}" style="max-width: 100%; max-height: 100%; object-fit: contain;" />`;
            } else {
                // Fallback icon
                element.innerHTML = '<i class="fa-solid fa-car-side"></i>';
            }
        }
    },

    // Helper for clearing search (kept from original file as it seems used by Blazor)
    clearSearchViaInterop: function (searchType) {
        if (window.componentReference) {
            window.componentReference.invokeMethodAsync('ClearSearch', searchType);
        }
    },

    setComponentReference: function (dotNetRef) {
        window.componentReference = dotNetRef;
    },

    clearComponentReference: function () {
        window.componentReference = null;
    },

    getVehicleTypeIcon: function (vehicleTypeName) {
        const icons = {
            'Otomobil': 'fa-solid fa-car',
            'Arazi Aracı': 'fa-solid fa-truck-pickup',
            'Motosiklet': 'fa-solid fa-motorcycle',
            'Hafif Ticari': 'fa-solid fa-shuttle-van',
            'Kamyon': 'fa-solid fa-truck',
            'Otobüs': 'fa-solid fa-bus',
            'Traktör': 'fa-solid fa-tractor',
            'İş Makinesi': 'fa-solid fa-snowplow',
            'Tekne': 'fa-solid fa-ship',
            'Karavan': 'fa-solid fa-caravan',
            'ATV': 'fa-solid fa-motorcycle',
            'UTV': 'fa-solid fa-truck-monster'
        };
        return icons[vehicleTypeName] || 'fa-solid fa-car';
    }
};

// Initialize on load
document.addEventListener('DOMContentLoaded', function () {
    window.carBrandLogos.init();
});
