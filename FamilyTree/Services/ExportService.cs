using System.Text.Json;
using FamilyTree.Data;
using FamilyTree.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyTree.Services;

public class ExportService
{
    private readonly ApplicationDbContext _context;

    public ExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> ExportTreeAsync(int treeId)
    {
        var tree = await _context.FamilyTrees
            .Include(t => t.Members)
                .ThenInclude(m => m.MediaItems)
            .Include(t => t.Members)
                .ThenInclude(m => m.Stories)
            .Include(t => t.TimelineEvents)
            .FirstAsync(t => t.Id == treeId);

        var payload = new
        {
            tree.Id,
            tree.Name,
            tree.Description,
            tree.CreatedAt,
            Members = tree.Members.Select(m => new
            {
                m.Id,
                m.FirstName,
                m.LastName,
                m.Nickname,
                m.BirthDate,
                m.MarriageDate,
                m.DeathDate,
                m.Gender,
                m.Location,
                m.PhotoUrl,
                m.BranchTag,
                m.OrderNumber,
                Stories = m.Stories.Select(s => new { s.Title, s.Content, s.CreatedAt }),
                MediaItems = m.MediaItems.Select(mi => new { mi.Type, mi.Url, mi.Caption, mi.UploadedAt })
            }),
            TimelineEvents = tree.TimelineEvents.Select(e => new
            {
                e.Title,
                e.Description,
                e.EventDate
            })
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task ImportTreeAsync(string json, string ownerId)
    {
        var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var tree = new FamilyTreeEntity
        {
            Name = root.GetProperty("Name").GetString() ?? "Imported Tree",
            Description = root.TryGetProperty("Description", out var desc) ? desc.GetString() : null,
            CreatedAt = DateTime.UtcNow,
            OwnerId = ownerId
        };

        if (root.TryGetProperty("Members", out var membersElement))
        {
            foreach (var member in membersElement.EnumerateArray())
            {
                tree.Members.Add(new Person
                {
                    FirstName = member.GetProperty("FirstName").GetString() ?? "",
                    LastName = member.GetProperty("LastName").GetString() ?? "",
                    Nickname = member.TryGetProperty("Nickname", out var nick) ? nick.GetString() : null,
                    BirthDate = member.GetProperty("BirthDate").GetDateTime(),
                    MarriageDate = member.TryGetProperty("MarriageDate", out var marriage) && marriage.ValueKind != JsonValueKind.Null ? marriage.GetDateTime() : null,
                    DeathDate = member.TryGetProperty("DeathDate", out var death) && death.ValueKind != JsonValueKind.Null ? death.GetDateTime() : null,
                    Gender = member.GetProperty("Gender").GetString() ?? "Unknown",
                    Location = member.TryGetProperty("Location", out var loc) ? loc.GetString() : null,
                    PhotoUrl = member.TryGetProperty("PhotoUrl", out var photo) ? photo.GetString() : null,
                    BranchTag = member.TryGetProperty("BranchTag", out var branch) ? branch.GetString() : null,
                    OrderNumber = member.TryGetProperty("OrderNumber", out var order) ? order.GetInt32() : 0
                });
            }
        }

        if (root.TryGetProperty("TimelineEvents", out var timelineElement))
        {
            foreach (var ev in timelineElement.EnumerateArray())
            {
                tree.TimelineEvents.Add(new TimelineEvent
                {
                    Title = ev.GetProperty("Title").GetString() ?? "",
                    Description = ev.GetProperty("Description").GetString() ?? "",
                    EventDate = ev.GetProperty("EventDate").GetDateTime()
                });
            }
        }

        _context.FamilyTrees.Add(tree);
        await _context.SaveChangesAsync();
    }
}
