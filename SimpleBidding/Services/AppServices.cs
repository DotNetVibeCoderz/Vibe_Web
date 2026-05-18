using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleBidding.Data;
using SimpleBidding.Models;

namespace SimpleBidding.Services
{
    public class AuctionService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public AuctionService(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<AuctionItem>> GetAllItemsAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuctionItems
                .Include(a => a.Seller)
                .Include(a => a.Winner)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<AuctionItem?> GetItemByIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuctionItems
                .Include(a => a.Seller)
                .Include(a => a.Winner)
                .Include(a => a.Bids)
                .ThenInclude(b => b.Bidder)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task CreateItemAsync(AuctionItem item)
        {
            using var context = _dbFactory.CreateDbContext();
            context.AuctionItems.Add(item);
            await context.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(AuctionItem item)
        {
            using var context = _dbFactory.CreateDbContext();
            context.AuctionItems.Update(item);
            await context.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            var item = await context.AuctionItems.FindAsync(id);
            if (item != null)
            {
                context.AuctionItems.Remove(item);
                await context.SaveChangesAsync();
            }
        }
        
        public async Task<List<AuctionItem>> GetItemsBySellerAsync(string sellerId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuctionItems
                .Where(a => a.SellerId == sellerId)
                .ToListAsync();
        }

        public async Task<List<AuctionItem>> GetClosedAuctionsBySellerAsync(string sellerId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuctionItems
                .Include(a => a.Winner)
                .Include(a => a.Bids)
                .Where(a => a.SellerId == sellerId && (a.EndTime <= DateTime.UtcNow || a.Status == AuctionStatus.Closed))
                .ToListAsync();
        }

        public async Task<List<AuctionItem>> GetWonAuctionsByBidderAsync(string bidderId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuctionItems
                .Include(a => a.Seller)
                .Where(a => a.WinnerId == bidderId && a.Status == AuctionStatus.Closed)
                .ToListAsync();
        }

        public async Task<(bool success, string message)> SetWinnerAsync(int itemId, string winnerId)
        {
            using var context = _dbFactory.CreateDbContext();
            var item = await context.AuctionItems.FindAsync(itemId);
            if (item == null) return (false, "Item tidak ditemukan.");

            item.WinnerId = winnerId;
            item.Status = AuctionStatus.Closed;
            
            await context.SaveChangesAsync();
            return (true, "Pemenang telah ditentukan.");
        }
    }

    public class BidService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly AuditService _auditService;

        public BidService(IDbContextFactory<ApplicationDbContext> dbFactory, AuditService auditService)
        {
            _dbFactory = dbFactory;
            _auditService = auditService;
        }

        public async Task<(bool success, string message)> PlaceBidAsync(int itemId, string userId, decimal amount)
        {
            using var context = _dbFactory.CreateDbContext();
            var item = await context.AuctionItems.FindAsync(itemId);
            
            if (item == null) return (false, "Item tidak ditemukan.");
            if (item.EndTime < DateTime.UtcNow || item.Status != AuctionStatus.Active) return (false, "Lelang sudah berakhir atau tutup.");
            if (amount <= item.CurrentPrice) return (false, "Tawaran harus lebih tinggi dari harga saat ini.");

            var bid = new Bid
            {
                AuctionItemId = itemId,
                BidderId = userId,
                Amount = amount,
                BidTime = DateTime.UtcNow
            };

            item.CurrentPrice = amount;
            context.Bids.Add(bid);
            await context.SaveChangesAsync();

            await _auditService.LogAsync("PlaceBid", userId, $"Bid {amount} pada item {itemId}");
            
            return (true, "Berhasil menawar!");
        }

        public async Task<List<Bid>> GetBidsByUserAsync(string userId)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Bids
                .Include(b => b.AuctionItem)
                .Where(b => b.BidderId == userId)
                .OrderByDescending(b => b.BidTime)
                .ToListAsync();
        }
    }

    public class AuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public AuditService(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task LogAsync(string action, string userId, string details)
        {
            using var context = _dbFactory.CreateDbContext();
            var log = new AuditLog
            {
                Action = action,
                UserId = userId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetAllLogsAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.AuditLogs.OrderByDescending(l => l.Timestamp).ToListAsync();
        }
    }

    public class ReportService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ReportService(IDbContextFactory<ApplicationDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            using var context = _dbFactory.CreateDbContext();
            return new DashboardStats
            {
                TotalItems = await context.AuctionItems.CountAsync(),
                ActiveAuctions = await context.AuctionItems.CountAsync(a => a.EndTime > DateTime.UtcNow),
                TotalBids = await context.Bids.CountAsync(),
                TotalUsers = await context.Users.CountAsync(),
                RecentTransactions = await context.Transactions.OrderByDescending(t => t.CreatedAt).Take(5).ToListAsync()
            };
        }
    }

    public class DashboardStats
    {
        public int TotalItems { get; set; }
        public int ActiveAuctions { get; set; }
        public int TotalBids { get; set; }
        public int TotalUsers { get; set; }
        public List<Transaction> RecentTransactions { get; set; } = new();
    }
}
