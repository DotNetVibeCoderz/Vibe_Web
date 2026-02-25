using System;

namespace QueueKiosk.Models;

public class AppQueueTicket
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public AppService? Service { get; set; }
    
    public string TicketNumber { get; set; } = string.Empty;
    public bool IsPriority { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CalledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Waiting, Called, Completed, Skipped
    public string Status { get; set; } = "Waiting";
    
    public int? CounterId { get; set; }
    public AppCounter? Counter { get; set; }
}
