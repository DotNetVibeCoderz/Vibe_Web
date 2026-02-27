using AISTracker.Data;
using AISTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace AISTracker.Services
{
    public class AISTrackingService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AISTrackingService> _logger;
        private readonly ConcurrentDictionary<int, PositionReport> _lastPositions = new();
        private readonly object _stateLock = new();
        private CancellationTokenSource? _simulationCts;
        private Task? _simulationTask;
        private bool _isRunning;

        public AISTrackingService(IServiceScopeFactory scopeFactory, ILogger<AISTrackingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool IsRunning
        {
            get
            {
                lock (_stateLock)
                {
                    return _isRunning;
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartSimulation();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopSimulation();
            return Task.CompletedTask;
        }

        public void StartSimulation()
        {
            lock (_stateLock)
            {
                if (_isRunning)
                {
                    return;
                }

                _simulationCts = new CancellationTokenSource();
                _simulationTask = SimulateMovement(_simulationCts.Token);
                _isRunning = true;
                _logger.LogInformation("AIS Tracking Service started.");
            }
        }

        public void StopSimulation()
        {
            lock (_stateLock)
            {
                if (!_isRunning)
                {
                    return;
                }

                _simulationCts?.Cancel();
                _simulationCts?.Dispose();
                _simulationCts = null;
                _isRunning = false;
                _logger.LogInformation("AIS Tracking Service stopped.");
            }
        }

        private async Task SimulateMovement(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var vessels = await dbContext.Vessels.ToListAsync(cancellationToken);

                        foreach (var vessel in vessels)
                        {
                            // Simulate slight movement
                            var lastPos = _lastPositions.ContainsKey(vessel.Id)
                                ? _lastPositions[vessel.Id]
                                : await dbContext.PositionReports
                                    .Where(p => p.VesselId == vessel.Id)
                                    .OrderByDescending(p => p.Timestamp)
                                    .FirstOrDefaultAsync(cancellationToken);

                            double lat = lastPos?.Latitude ?? (new Random().NextDouble() * 180 - 90);
                            double lon = lastPos?.Longitude ?? (new Random().NextDouble() * 360 - 180);

                            // Small random shift
                            lat += (new Random().NextDouble() - 0.5) * 0.01;
                            lon += (new Random().NextDouble() - 0.5) * 0.01;

                            var newPos = new PositionReport
                            {
                                VesselId = vessel.Id,
                                Latitude = lat,
                                Longitude = lon,
                                SpeedOverGround = new Random().Next(0, 20),
                                CourseOverGround = new Random().Next(0, 360),
                                Timestamp = DateTime.UtcNow,
                                NavigationalStatus = "Under way using engine"
                            };

                            dbContext.PositionReports.Add(newPos);
                            _lastPositions[vessel.Id] = newPos;

                            // Alert Logic: Collision near port (dummy logic)
                            if (newPos.SpeedOverGround > 15) // Speed limit check
                            {
                                dbContext.Alerts.Add(new Alert
                                {
                                    Title = "Speed Alert",
                                    Message = $"{vessel.Name} is speeding at {newPos.SpeedOverGround} knots.",
                                    Severity = "Warning",
                                    IsRead = false,
                                    Timestamp = DateTime.UtcNow
                                });
                            }
                        }
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AIS simulation loop");
                }

                await Task.Delay(5000, cancellationToken); // Update every 5 seconds
            }
        }

        public PositionReport GetLastPosition(int vesselId)
        {
            _lastPositions.TryGetValue(vesselId, out var pos);
            return pos;
        }
    }
}
