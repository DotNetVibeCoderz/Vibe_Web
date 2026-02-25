namespace QueueKiosk.Models;

public class AppService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LetterCode { get; set; } = "A"; 
    public bool IsActive { get; set; } = true;
    public string Description { get; set; } = string.Empty;
}
