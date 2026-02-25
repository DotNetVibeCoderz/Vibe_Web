using QueueKiosk.Models;
using QueueKiosk.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace QueueKiosk.Services;

public class QueueManagerService
{
    private readonly IServiceProvider _serviceProvider;
    public event Action? OnQueueUpdated;

    public QueueManagerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NotifyUpdate() => OnQueueUpdated?.Invoke();

    public async Task<AppQueueTicket?> GenerateTicketAsync(int serviceId, bool isPriority)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = await db.Services.FindAsync(serviceId);
        if (service == null) return null;

        var today = DateTime.Today;
        var todayCount = await db.QueueTickets
            .CountAsync(q => q.ServiceId == serviceId && q.CreatedAt >= today);

        var ticket = new AppQueueTicket
        {
            ServiceId = serviceId,
            TicketNumber = $"{service.LetterCode}{(isPriority ? "-P-" : "-")}{(todayCount + 1).ToString("D3")}",
            IsPriority = isPriority,
            CreatedAt = DateTime.Now,
            Status = "Waiting"
        };

        db.QueueTickets.Add(ticket);
        await db.SaveChangesAsync();

        NotifyUpdate();
        return await db.QueueTickets.Include(q => q.Service).FirstOrDefaultAsync(q => q.Id == ticket.Id);
    }

    public async Task<List<AppQueueTicket>> GetWaitingQueueAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.QueueTickets
            .Include(q => q.Service)
            .Where(q => q.Status == "Waiting")
            .OrderByDescending(q => q.IsPriority)
            .ThenBy(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AppQueueTicket>> GetCalledQueueAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Only get recently called (e.g., today)
        return await db.QueueTickets
            .Include(q => q.Service)
            .Include(q => q.Counter)
            .Where(q => q.Status == "Called" && q.CalledAt >= DateTime.Today)
            .OrderByDescending(q => q.CalledAt)
            .ToListAsync();
    }

    public async Task<AppQueueTicket?> CallNextForCounterAsync(int counterId, string supportedServiceIds)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var query = db.QueueTickets
            .Include(q => q.Service)
            .Where(q => q.Status == "Waiting");

        if (!string.IsNullOrWhiteSpace(supportedServiceIds))
        {
            var allowedServiceIds = supportedServiceIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(int.Parse)
                                                       .ToList();
            if (allowedServiceIds.Any())
            {
                query = query.Where(q => allowedServiceIds.Contains(q.ServiceId));
            }
        }

        var nextWaiting = await query
            .OrderByDescending(q => q.IsPriority)
            .ThenBy(q => q.CreatedAt)
            .FirstOrDefaultAsync();

        if (nextWaiting != null)
        {
            nextWaiting.Status = "Called";
            nextWaiting.CalledAt = DateTime.Now;
            nextWaiting.CounterId = counterId;
            await db.SaveChangesAsync();
            NotifyUpdate();
            
            // Mock Notification
            var notif = scope.ServiceProvider.GetRequiredService<NotificationMockService>();
            notif.SendNotification(nextWaiting.TicketNumber, "Your turn is coming up soon! Please proceed to " + db.Counters.Find(counterId)?.Name);
        }

        return nextWaiting;
    }

    public async Task<AppQueueTicket?> CallNextAsync(int counterId)
    {
        return await CallNextForCounterAsync(counterId, string.Empty);
    }

    public async Task CompleteTicketAsync(int ticketId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticket = await db.QueueTickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.Status = "Completed";
            ticket.CompletedAt = DateTime.Now;
            await db.SaveChangesAsync();
            NotifyUpdate();
        }
    }
    
    public async Task SkipTicketAsync(int ticketId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticket = await db.QueueTickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.Status = "Skipped";
            await db.SaveChangesAsync();
            NotifyUpdate();
        }
    }
}
