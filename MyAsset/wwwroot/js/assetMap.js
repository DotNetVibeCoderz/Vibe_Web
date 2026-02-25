window.downloadFile = (filename, content) => {
    const element = document.createElement('a');
    element.setAttribute('href', 'data:text/csv;base64,' + content);
    element.setAttribute('download', filename);
    element.style.display = 'none';
    document.body.appendChild(element);
    element.click();
    document.body.removeChild(element);
}

window.assetMap = {
    map: null,
    markers: [],
    dotNetRef: null,

    initializeMap: function (elementId, assets, dotNetRef) {
        this.dotNetRef = dotNetRef;
        
        // Check if map already exists
        if (this.map) {
            this.map.remove();
        }

        // Initialize Map
        // Default center Jakarta for example or 0,0
        var center = [-6.2088, 106.8456];
        if (assets && assets.length > 0) {
            // Find first asset with coords
            var first = assets.find(a => a.latitude !== 0 && a.longitude !== 0);
            if (first) {
                center = [first.latitude, first.longitude];
            }
        }

        this.map = L.map(elementId).setView(center, 12);

        L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(this.map);

        this.addMarkers(assets);
    },

    addMarkers: function (assets) {
        // Clear existing markers
        this.markers.forEach(m => this.map.removeLayer(m));
        this.markers = [];

        assets.forEach(asset => {
            if (asset.latitude && asset.longitude && (asset.latitude !== 0 || asset.longitude !== 0)) {
                var marker = L.marker([asset.latitude, asset.longitude])
                    .addTo(this.map)
                    .bindPopup(`<b>${asset.name}</b><br>${asset.location}`);

                // Add click event to notify Blazor
                marker.on('click', () => {
                    this.dotNetRef.invokeMethodAsync('SelectAsset', asset.id);
                });

                this.markers.push(marker);
            }
        });
    },

    flyTo: function (lat, lng) {
        if (this.map) {
            this.map.flyTo([lat, lng], 16);
        }
    }
};

window.browserGeolocation = {
    getCurrentPosition: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Geolocation is not supported by this browser.");
            } else {
                navigator.geolocation.getCurrentPosition(
                    (position) => {
                        resolve({
                            latitude: position.coords.latitude,
                            longitude: position.coords.longitude
                        });
                    },
                    (error) => {
                        reject(error.message);
                    }
                );
            }
        });
    }
};