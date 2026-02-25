using System.Collections.Concurrent;
using System.Timers;
using FMSNet.Data;
using Microsoft.EntityFrameworkCore;

namespace FMSNet.Services
{
    public interface ISimulationService
    {
        void Start();
        void Stop();
        bool IsRunning { get; }
    }

    public class SimulationService : ISimulationService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private bool _isRunning;
        private Random _random = new Random();

        // Menyimpan state pergerakan kendaraan (Heading dalam derajat, Speed dalam koordinat unit per detik)
        private static readonly ConcurrentDictionary<int, (double Heading, double Speed)> _vehicleStates = new();

        public bool IsRunning => _isRunning;

        public SimulationService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            // Kita turunkan interval ke 1 detik agar pergerakan terlihat lebih update dan smooth di UI
            _timer = new System.Timers.Timer(1000); 
            _timer.Elapsed += async (sender, e) => await UpdateVehiclePositions();
        }

        public void Start()
        {
            if (!_isRunning)
            {
                _timer.Start();
                _isRunning = true;
            }
        }

        public void Stop()
        {
            if (_isRunning)
            {
                _timer.Stop();
                _isRunning = false;
            }
        }

        private async Task UpdateVehiclePositions()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var vehicles = await dbContext.Vehicles.ToListAsync();

                foreach (var v in vehicles)
                {
                    if (v.Status == "Active")
                    {
                        // Inisialisasi state jika belum ada
                        if (!_vehicleStates.TryGetValue(v.Id, out var state))
                        {
                            state = (_random.NextDouble() * 360, 0.0001 + (_random.NextDouble() * 0.0002));
                            _vehicleStates[v.Id] = state;
                        }

                        // Simulasi perubahan heading yang smooth (efek memutar/belok)
                        // Perubahan maksimal 5 derajat per update
                        double headingChange = (_random.NextDouble() - 0.5) * 10;
                        double newHeading = (state.Heading + headingChange) % 360;

                        // Konversi heading ke Radian untuk perhitungan trigonometri
                        double radian = newHeading * (Math.PI / 180.0);

                        // Hitung pergeseran posisi berdasarkan kecepatan
                        // Kita gunakan kecepatan konstan yang sedikit bervariasi
                        double latChange = Math.Cos(radian) * state.Speed;
                        double longChange = Math.Sin(radian) * state.Speed;

                        v.Latitude += latChange;
                        v.Longitude += longChange;

                        // Update state terbaru
                        _vehicleStates[v.Id] = (newHeading, state.Speed);
                        
                        // Simulasi konsumsi bahan bakar yang lebih realistis berdasarkan pergerakan
                        // Konsumsi sedikit demi sedikit
                        v.FuelLevel -= 0.005; 
                        if (v.FuelLevel < 0) v.FuelLevel = 0;

                        // Simulasi odometer (asumsi kecepatan dikonversi ke KM)
                        // 0.0001 unit koordinat kurang lebih ~11 meter
                        v.Odometer += (state.Speed * 111); // Konversi kasar ke KM

                        v.LastUpdate = DateTime.UtcNow;
                    }
                }

                try 
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Abaikan jika terjadi konflik karena ini hanya simulasi
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}