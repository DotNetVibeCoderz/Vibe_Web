using FamilyTree.Data;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class TreeService
{
    private readonly ApplicationDbContext _context;

    public TreeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Person>> GetOrderedMembersAsync(int treeId)
    {
        var members = await _context.People
            .Where(p => p.FamilyTreeEntityId == treeId)
            .ToListAsync();

        return members
            .OrderBy(p => p.BirthDate)
            .Select((person, index) =>
            {
                person.OrderNumber = index + 1;
                return person;
            })
            .ToList();
    }

    public IEnumerable<Person> GetSiblingsOrdered(IEnumerable<Person> siblings)
    {
        return siblings.OrderBy(p => p.BirthDate)
            .Select((person, index) =>
            {
                person.OrderNumber = index + 1;
                return person;
            });
    }
}
