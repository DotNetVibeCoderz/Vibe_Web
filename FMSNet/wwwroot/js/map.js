
let map;
let markers = {};

// Custom Icons using SVGs
const createIcon = (svgContent, color) => {
    return L.divIcon({
        className: 'custom-vehicle-icon',
        html: `<div style="background-color: ${color}; width: 30px; height: 30px; border: 2px solid black; border-radius: 50%; display: flex; justify-content: center; align-items: center; box-shadow: 2px 2px 0px black;">
                ${svgContent}
               </div>`,
        iconSize: [30, 30],
        iconAnchor: [15, 15],
        popupAnchor: [0, -15]
    });
};

const truckSvg = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="1" y="3" width="15" height="13"></rect><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"></polygon><circle cx="5.5" cy="18.5" r="2.5"></circle><circle cx="18.5" cy="18.5" r="2.5"></circle></svg>';
const carSvg = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M19.9 14.9L17.5 7.6C17.2 6.7 16.3 6 15.3 6H8.7C7.7 6 6.8 6.7 6.5 7.6L4.1 14.9C3.6 16.3 4.6 17.9 6 18H18C19.4 17.9 20.4 16.3 19.9 14.9Z"></path><circle cx="7" cy="18" r="2"></circle><circle cx="17" cy="18" r="2"></circle></svg>';
const vanSvg = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="black" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="5" width="20" height="12" rx="2"></rect><circle cx="7" cy="18" r="2"></circle><circle cx="17" cy="18" r="2"></circle><line x1="2" y1="10" x2="22" y2="10"></line></svg>';

const icons = {
    'Truck': createIcon(truckSvg, '#ffcc00'), // Yellow
    'Car': createIcon(carSvg, '#00ffcc'),   // Cyan
    'Van': createIcon(vanSvg, '#ff0055')    // Red/Pink
};

export function initializeMap(elementId, centerLat, centerLong) {
    if (!map) {
        map = L.map(elementId).setView([centerLat, centerLong], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(map);
    }
}

export function updateMarkers(vehicles) {
    if (!map) return;

    vehicles.forEach(vehicle => {
        let lat = vehicle.latitude;
        let lng = vehicle.longitude;
        let id = vehicle.id;
        let type = vehicle.type || 'Car';
        let info = `<div style="font-family: 'Courier New';"><b>${vehicle.name}</b><br>Type: ${type}<br>Status: ${vehicle.status}</div>`;

        if (markers[id]) {
            // Move existing marker
            let marker = markers[id];
            let newLatLng = new L.LatLng(lat, lng);
            marker.setLatLng(newLatLng);
            marker.setPopupContent(info);
            
            // Check if we need to update icon (in case type changed, unlikely but safe)
            // marker.setIcon(icons[type] || icons['Car']); 
        } else {
            // Create new marker
            let icon = icons[type] || icons['Car'];
            let marker = L.marker([lat, lng], { icon: icon }).addTo(map).bindPopup(info);
            markers[id] = marker;
        }
    });
}

export function panTo(lat, lng) {
    if (map) {
        map.flyTo([lat, lng], 16);
    }
}
