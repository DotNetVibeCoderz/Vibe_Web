// Chart.js wrapper. Charts read their colours from the live design tokens, so a
// theme switch restyles every chart on the page instead of leaving dark text on
// a dark card.
(function () {
    const registry = new Map();

    function token(name, fallback) {
        const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
        return v || fallback;
    }

    function palette() {
        return [
            token('--dongker', '#1b3a6b'),
            token('--kunyit', '#e8a317'),
            token('--daun', '#2f7d5c'),
            token('--bata', '#b3452f'),
            '#6b7fa8',
            '#9a6fb0'
        ];
    }

    function alpha(hex, a) {
        const h = hex.replace('#', '');
        const n = h.length === 3 ? h.split('').map(c => c + c).join('') : h;
        const r = parseInt(n.slice(0, 2), 16), g = parseInt(n.slice(2, 4), 16), b = parseInt(n.slice(4, 6), 16);
        return `rgba(${r}, ${g}, ${b}, ${a})`;
    }

    function buildConfig(type, labels, data, label) {
        const colors = palette();
        const grid = token('--rule-soft', '#e6e9ee');
        const text = token('--fg-muted', '#5b6678');
        const categorical = type === 'pie' || type === 'doughnut' || type === 'bar';

        return {
            type: type,
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: categorical
                        ? labels.map((_, i) => alpha(colors[i % colors.length], type === 'bar' ? 0.85 : 0.9))
                        : alpha(colors[0], 0.14),
                    borderColor: categorical
                        ? labels.map((_, i) => colors[i % colors.length])
                        : colors[0],
                    borderWidth: type === 'line' ? 2.5 : 1,
                    borderRadius: type === 'bar' ? 5 : 0,
                    fill: type === 'line',
                    tension: 0.35,
                    pointRadius: type === 'line' ? 3 : 0,
                    pointBackgroundColor: colors[1],
                    pointBorderColor: token('--bg-surface', '#fff'),
                    pointBorderWidth: 2,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        display: categorical && type !== 'bar',
                        position: 'bottom',
                        labels: {
                            color: text,
                            usePointStyle: true,
                            pointStyle: 'circle',
                            boxWidth: 8,
                            padding: 14,
                            font: { family: 'Public Sans', size: 12 }
                        }
                    },
                    tooltip: {
                        backgroundColor: token('--tinta', '#101a2e'),
                        titleFont: { family: 'Public Sans', weight: '600' },
                        bodyFont: { family: 'IBM Plex Mono', size: 12 },
                        padding: 10,
                        cornerRadius: 6,
                        displayColors: false
                    }
                },
                scales: (type === 'pie' || type === 'doughnut') ? {} : {
                    x: {
                        grid: { display: false, drawBorder: false },
                        ticks: { color: text, font: { family: 'Public Sans', size: 11 } }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: grid, drawBorder: false },
                        border: { display: false },
                        ticks: { color: text, font: { family: 'IBM Plex Mono', size: 11 }, precision: 0 }
                    }
                }
            }
        };
    }

    window.smsnetChart = {
        render: function (canvasId, type, labels, data, label) {
            const el = document.getElementById(canvasId);
            if (!el || typeof Chart === 'undefined') return;

            const existing = registry.get(canvasId);
            if (existing) existing.chart.destroy();

            const chart = new Chart(el, buildConfig(type, labels, data, label));
            registry.set(canvasId, { chart, type, labels, data, label });
        },

        destroy: function (canvasId) {
            const entry = registry.get(canvasId);
            if (entry) { entry.chart.destroy(); registry.delete(canvasId); }
        }
    };

    // Restyle every live chart when the theme flips.
    window.addEventListener('smsnet:themechange', function () {
        registry.forEach(function (entry, id) {
            entry.chart.destroy();
            const el = document.getElementById(id);
            if (!el) { registry.delete(id); return; }
            entry.chart = new Chart(el, buildConfig(entry.type, entry.labels, entry.data, entry.label));
        });
    });
})();
