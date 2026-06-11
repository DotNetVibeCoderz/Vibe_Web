// Model jadwal dokter
namespace VirtualDoctor.Models;

public class DoctorSchedule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DoctorId { get; set; } = string.Empty;
    public DayOfWeek Day { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxPatients { get; set; } = 10;
    
    public Doctor Doctor { get; set; } = null!;
}
