window.setTheme = (theme) => {
    var link = document.getElementById("theme-link");
    if (link) {
        link.href = "_content/Radzen.Blazor/css/" + theme + ".css";
    }
};

window.googleMapsState = {
    isReady: false,
    pendingInits: []
};

window.googleMapsLoaded = () => {
    window.googleMapsState.isReady = true;
    window.googleMapsState.pendingInits.forEach(action => action());
    window.googleMapsState.pendingInits = [];
};

// Google Maps utilities for Live Tracking & History Playback
window.googleMaps = {
    maps: {},

    init: function (mapId, center, zoom) {
        var initialize = () => {
            if (this.maps[mapId]) {
                return;
            }

            var mapElement = document.getElementById(mapId);
            if (!mapElement || !window.google || !window.google.maps) {
                return;
            }

            var map = new google.maps.Map(mapElement, {
                center: { lat: center.lat, lng: center.lng },
                zoom: zoom || 2,
                mapTypeId: "roadmap"
            });

            this.maps[mapId] = {
                map: map,
                markers: [],
                route: null,
                playbackMarker: null,
                playbackTimer: null,
                dotNetRef: null
            };
        };

        if (!window.googleMapsState.isReady) {
            window.googleMapsState.pendingInits.push(initialize);
            return;
        }

        initialize();
    },

    registerMarkerClickHandler: function (mapId, dotNetRef) {
        var instance = this.maps[mapId];
        if (!instance) {
            return;
        }

        instance.dotNetRef = dotNetRef;
    },

    setMarkers: function (mapId, markers) {
        var instance = this.maps[mapId];
        if (!instance) {
            return;
        }

        instance.markers.forEach(m => m.setMap(null));
        instance.markers = [];

        if (!markers || markers.length === 0) {
            return;
        }

        markers.forEach(item => {
            var marker = new google.maps.Marker({
                position: { lat: item.lat, lng: item.lng },
                map: instance.map,
                title: item.title || ""
            });

            if (instance.dotNetRef) {
                marker.addListener("click", () => {
                    instance.dotNetRef.invokeMethodAsync("HandleMarkerClick", item);
                });
            }

            instance.markers.push(marker);
        });
    },

    setRoute: function (mapId, points) {
        var instance = this.maps[mapId];
        if (!instance) {
            return;
        }

        if (instance.route) {
            instance.route.setMap(null);
            instance.route = null;
        }

        if (!points || points.length === 0) {
            return;
        }

        var path = points.map(p => ({ lat: p.lat, lng: p.lng }));
        instance.route = new google.maps.Polyline({
            path: path,
            strokeColor: "#2563eb",
            strokeOpacity: 1,
            strokeWeight: 3,
            map: instance.map
        });

        var bounds = new google.maps.LatLngBounds();
        path.forEach(point => bounds.extend(point));
        instance.map.fitBounds(bounds);
    },

    playRoute: function (mapId, points, intervalMs) {
        var instance = this.maps[mapId];
        if (!instance) {
            return;
        }

        this.stopPlayback(mapId);

        if (!points || points.length === 0) {
            return;
        }

        var path = points.map(p => ({ lat: p.lat, lng: p.lng }));
        var index = 0;

        instance.playbackMarker = new google.maps.Marker({
            position: path[0],
            map: instance.map
        });
        instance.map.setCenter(path[0]);
        instance.map.setZoom(8);

        instance.playbackTimer = setInterval(() => {
            index++;
            if (index >= path.length) {
                this.stopPlayback(mapId);
                return;
            }
            instance.playbackMarker.setPosition(path[index]);
        }, intervalMs || 800);
    },

    stopPlayback: function (mapId) {
        var instance = this.maps[mapId];
        if (!instance) {
            return;
        }

        if (instance.playbackTimer) {
            clearInterval(instance.playbackTimer);
            instance.playbackTimer = null;
        }

        if (instance.playbackMarker) {
            instance.playbackMarker.setMap(null);
            instance.playbackMarker = null;
        }
    }
};
