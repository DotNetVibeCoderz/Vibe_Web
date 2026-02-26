window.smsnetChart = {
    render: function (canvasId, type, labels, data, label) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            return;
        }

        if (ctx._chartInstance) {
            ctx._chartInstance.destroy();
        }

        ctx._chartInstance = new Chart(ctx, {
            type: type,
            data: {
                labels: labels,
                datasets: [
                    {
                        label: label,
                        data: data,
                        backgroundColor: [
                            'rgba(79, 70, 229, 0.7)',
                            'rgba(14, 165, 233, 0.7)',
                            'rgba(34, 197, 94, 0.7)',
                            'rgba(245, 158, 11, 0.7)',
                            'rgba(239, 68, 68, 0.7)'
                        ],
                        borderColor: 'rgba(79, 70, 229, 1)',
                        borderWidth: 2,
                        fill: type === 'line'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true
                    }
                }
            }
        });
    }
};
