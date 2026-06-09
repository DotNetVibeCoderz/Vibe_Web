using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SimpleBidding.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal Balance { get; set; } = 0;
    }

    public enum AuctionStatus
    {
        Active,
        Closed,
        Cancelled
    }

    public class AuctionItem
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "https://via.placeholder.com/300";
        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public AuctionStatus Status { get; set; } = AuctionStatus.Active;
        
        public string SellerId { get; set; } = string.Empty;
        public ApplicationUser? Seller { get; set; }

        public string? WinnerId { get; set; }
        public ApplicationUser? Winner { get; set; }
        
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public string? Category { get; set; }
    }

    public class Bid
    {
        public int Id { get; set; }
        public int AuctionItemId { get; set; }
        public AuctionItem? AuctionItem { get; set; }
        
        public string BidderId { get; set; } = string.Empty;
        public ApplicationUser? Bidder { get; set; }
        
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; } = DateTime.UtcNow;
    }

    public class Transaction
    {
        public int Id { get; set; }
        public int AuctionItemId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Paid, Failed
        public string PaymentMethod { get; set; } = "Simulated";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // Login, Bid, CreateItem, etc.
        public string UserId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
