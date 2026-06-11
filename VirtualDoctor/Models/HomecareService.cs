// Model homecare service
namespace VirtualDoctor.Models;

public class HomecareService
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public HomecareServiceType ServiceType { get; set; }
    public HomecareServiceStatus Status { get; set; } = HomecareServiceStatus.Requested;
    public DateTime ScheduledDate { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public decimal Fee { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser User { get; set; } = null!;
}

public enum HomecareServiceType { LabTest, Vaccination, VitaminBooster, DoctorVisit, NurseVisit }
public enum HomecareServiceStatus { Requested, Confirmed, InProgress, Completed, Cancelled }
