namespace FamilyTree.Models;

public class Relationship
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public Person? Person { get; set; }

    public int RelatedPersonId { get; set; }
    public Person? RelatedPerson { get; set; }

    public RelationshipType Type { get; set; }
}
