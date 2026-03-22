namespace FamilyTree.Models;

public class Story
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
