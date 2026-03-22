namespace FamilyTree.Models;

public class TimelineEvent
{
    public int Id { get; set; }
    public int FamilyTreeEntityId { get; set; }
    public FamilyTreeEntity? FamilyTree { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
}
