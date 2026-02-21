// leaflet-map.js - Geospatial visualization dengan Leaflet.js

window.initializeLeafletMap = function(mapData) {
    console.log('Initializing Leaflet map with data:', mapData);
    
    // Check if Leaflet is loaded
    if (typeof L === 'undefined') {
        console.error('Leaflet.js is not loaded!');
        document.getElementById('map').innerHTML = 
            '<div style="display:flex;justify-content:center;align-items:center;height:100%;background:#ffe6e6;">' +
            '<p style="color:red;font-weight:bold;">Leaflet.js not loaded. Please check internet connection.</p>' +
            '</div>';
        return;
    }

    // Clear existing map if any
    var container = document.getElementById('map');
    container.innerHTML = '';

    // Initialize map centered on Indonesia
    var map = L.map('map').setView([-2.5489, 117.0], 5); // Center of Indonesia

    // Add OpenStreetMap tiles (gratis, no API key needed)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 18,
        minZoom: 3
    }).addTo(map);

    // Custom icon colors based on risk level
    function getCircleColor(riskScore) {
        if (riskScore > 70) return '#ff3333'; // Red - High Risk
        if (riskScore > 40) return '#ffcc00'; // Yellow - Medium Risk
        return '#00cc66'; // Green - Low Risk
    }

    function getCircleRadius(count) {
        return Math.min(30, Math.max(10, count * 2)); // Scale radius based on post count
    }

    // Add markers for each location
    mapData.forEach(function(data) {
        var color = getCircleColor(data.riskScore);
        var radius = getCircleRadius(data.count);
        
        // Create circle marker
        var circle = L.circleMarker([data.lat, data.lng], {
            radius: radius,
            fillColor: color,
            color: '#000',
            weight: 2,
            opacity: 1,
            fillOpacity: 0.7
        }).addTo(map);

        // Create popup content with Brutalist styling
        var popupContent = `
            <div style="font-family:'Courier New',monospace;border:2px solid #000;padding:10px;background:#fff;min-width:200px;">
                <h3 style="margin:0 0 10px 0;font-size:16px;text-transform:uppercase;border-bottom:2px solid #000;padding-bottom:5px;">
                    ${data.location}
                </h3>
                <div style="margin-bottom:8px;">
                    <strong>Posts:</strong> ${data.count}
                </div>
                <div style="margin-bottom:8px;">
                    <strong>Sentiment:</strong> 
                    <span style="background:${data.sentiment === 'Positive' ? '#00cc66' : data.sentiment === 'Negative' ? '#ff3333' : '#cccccc'}; 
                                  color:${data.sentiment === 'Negative' ? '#fff' : '#000'}; 
                                  padding:2px 6px;font-weight:bold;">
                        ${data.sentiment}
                    </span>
                </div>
                <div style="margin-bottom:8px;">
                    <strong>Risk Level:</strong> 
                    <span style="background:${color};color:#fff;padding:2px 6px;font-weight:bold;">
                        ${data.riskScore > 70 ? 'HIGH' : data.riskScore > 40 ? 'MEDIUM' : 'LOW'} (${data.riskScore}%)
                    </span>
                </div>
                <div style="font-size:11px;color:#666;border-top:1px solid #000;padding-top:5px;margin-top:5px;">
                    Media Monitoring OSINT<br/>
                    Last updated: ${new Date().toLocaleString()}
                </div>
            </div>
        `;

        circle.bindPopup(popupContent);

        // Add tooltip on hover
        circle.bindTooltip(`${data.location}: ${data.count} posts`, {
            permanent: false,
            direction: 'top',
            className: 'brutalist-tooltip'
        });
    });

    // Add custom CSS for tooltips
    var style = document.createElement('style');
    style.textContent = `
        .brutalist-tooltip {
            font-family: 'Courier New', monospace !important;
            border: 2px solid #000 !important;
            box-shadow: 4px 4px 0px rgba(0,0,0,0.3) !important;
            background: #fff !important;
            font-weight: bold !important;
            text-transform: uppercase !important;
        }
    `;
    document.head.appendChild(style);

    console.log('Leaflet map initialized successfully with', mapData.length, 'locations');
    
    // Fit bounds to show all markers
    if (mapData.length > 0) {
        var group = new L.featureGroup(mapData.map(function(data) {
            return L.circleMarker([data.lat, data.lng]);
        }));
        map.fitBounds(group.getBounds(), { padding: [50, 50] });
    }
};