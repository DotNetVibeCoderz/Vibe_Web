namespace FamilyTree.Models;

public class Person
{
    public int Id { get; set; }
    public int FamilyTreeEntityId { get; set; }
    public FamilyTreeEntity? FamilyTree { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateTime BirthDate { get; set; }
    public DateTime? MarriageDate { get; set; }
    public DateTime? DeathDate { get; set; }
    public string Gender { get; set; } = "Unknown";
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    public string? BranchTag { get; set; }

    // Notes stored encrypted
    public string? NotesEncrypted { get; set; }

    public int OrderNumber { get; set; }

    public List<Relationship> Relationships { get; set; } = new();
    public List<MediaItem> MediaItems { get; set; } = new();
    public List<Story> Stories { get; set; } = new();
}
