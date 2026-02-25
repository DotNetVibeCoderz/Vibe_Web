using FMSNet.Data;
using Microsoft.EntityFrameworkCore;

namespace FMSNet.Services
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetVehiclesAsync();
        Task<Vehicle?> GetVehicleByIdAsync(int id);
        Task<Vehicle> CreateVehicleAsync(Vehicle vehicle);
        Task<Vehicle> UpdateVehicleAsync(Vehicle vehicle);
        Task DeleteVehicleAsync(int id);
    }

    public class VehicleService : IVehicleService
    {
        private readonly ApplicationDbContext _context;

        public VehicleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetVehiclesAsync()
        {
            return await _context.Vehicles.AsNoTracking().Include(v => v.Driver).ToListAsync();
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(int id)
        {
            return await _context.Vehicles.FindAsync(id);
        }

        public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<Vehicle> UpdateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task DeleteVehicleAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
        }
    }
}