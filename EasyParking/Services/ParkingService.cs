using EasyParking.Models;
using EasyParking.Data;
using Microsoft.EntityFrameworkCore;

namespace EasyParking.Services;

public class ParkingService
{
    private readonly AppDbContext _context;

    public ParkingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ParkingSlot>> GetSlotsAsync()
    {
        return await _context.ParkingSlots.ToListAsync();
    }

    public async Task<ParkingSlot?> GetAvailableSlotAsync(string level, string type, bool isEv)
    {
        return await _context.ParkingSlots
            .FirstOrDefaultAsync(s => !s.IsOccupied && !s.IsReserved && 
                                    s.Level == level && s.Type == type && s.IsEV == isEv);
    }

    public async Task CheckInAsync(string plate, int slotId)
    {
        var slot = await _context.ParkingSlots.FindAsync(slotId);
        if (slot != null && !slot.IsOccupied)
        {
            slot.IsOccupied = true;
            _context.Transactions.Add(new ParkingTransaction
            {
                VehiclePlate = plate,
                ParkingSlotId = slotId,
                CheckInTime = DateTime.Now,
                Status = "Active"
            });
            await _context.SaveChangesAsync();
        }
    }

    public async Task<decimal> CheckOutAsync(int transactionId)
    {
        var trx = await _context.Transactions.Include(t => t.ParkingSlot)
                                 .FirstOrDefaultAsync(t => t.Id == transactionId);
        if (trx != null && trx.Status == "Active")
        {
            trx.CheckOutTime = DateTime.Now;
            var duration = trx.CheckOutTime.Value - trx.CheckInTime;
            trx.Amount = (decimal)Math.Ceiling(duration.TotalHours) * 5000; // Base rate 5000/hour
            trx.Status = "Completed";

            if (trx.ParkingSlot != null)
                trx.ParkingSlot.IsOccupied = false;

            await _context.SaveChangesAsync();
            return trx.Amount.Value;
        }
        return 0;
    }
}
