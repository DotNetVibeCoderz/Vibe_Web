var kopdarMap = null;

window.initMap = (elementId, lat, lon, users, groups) => {
    // Destroy map if already exists
    if (kopdarMap !== null) {
        kopdarMap.remove();
    }

    // Initialize map
    kopdarMap = L.map(elementId).setView([lat, lon], 13);

    // Set map tiles (OpenStreetMap)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap Kopdar'
    }).addTo(kopdarMap);

    // Add Current User Marker
    var currentIcon = L.icon({
        iconUrl: '/images/logo.svg',
        iconSize: [40, 40],
        iconAnchor: [20, 40],
        popupAnchor: [0, -40]
    });
    L.marker([lat, lon], {icon: currentIcon}).addTo(kopdarMap)
        .bindPopup('<b>You are here!</b>').openPopup();

    // Add Other Users
    users.forEach(u => {
        L.marker([u.latitude, u.longitude]).addTo(kopdarMap)
            .bindPopup(`<b>${u.username}</b><br>${u.bio || ''}<br><a href="/profile/${u.id}">View Profile</a>`);
    });

    // Add Groups
    groups.forEach(g => {
        var groupIcon = L.icon({
            iconUrl: 'https://cdn-icons-png.flaticon.com/512/32/32441.png',
            iconSize: [30, 30]
        });
        L.marker([g.latitude, g.longitude], {icon: groupIcon}).addTo(kopdarMap)
            .bindPopup(`<b>Group: ${g.name}</b><br>${g.description || ''}`);
    });
};