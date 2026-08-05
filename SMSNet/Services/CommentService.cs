using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services;

/// <summary>
/// Reads and writes the comment threads that hang off forum topics and KPI
/// indicators.
/// <para>
/// Deletion authority lives here rather than in the pages: "the author or an
/// admin" is a rule about the data, and a page that forgot to check would be a
/// silent hole. The UI hides the button, but this method is what actually
/// refuses.
/// </para>
/// </summary>
public sealed class CommentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IWebHostEnvironment environment,
        ILogger<CommentService> logger)
    {
        _dbFactory = dbFactory;
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<Comment>> ListAsync(string threadType, int threadId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Comments
            .AsNoTracking()
            .Include(c => c.Attachments)
            .Where(c => c.ThreadType == threadType && c.ThreadId == threadId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>Comment counts for a set of hosts, so a list page can show them
    /// without one query per row.</summary>
    public async Task<Dictionary<int, int>> CountsAsync(
        string threadType, IEnumerable<int> threadIds, CancellationToken ct = default)
    {
        var ids = threadIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.Comments
            .AsNoTracking()
            .Where(c => c.ThreadType == threadType && ids.Contains(c.ThreadId))
            .GroupBy(c => c.ThreadId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<Comment> AddAsync(
        string threadType,
        int threadId,
        ClaimsPrincipal? user,
        string body,
        IEnumerable<StoredFile>? attachments = null,
        CancellationToken ct = default)
    {
        var comment = new Comment
        {
            ThreadType = threadType,
            ThreadId = threadId,
            AuthorUserId = UserIdOf(user) ?? string.Empty,
            AuthorName = DisplayNameOf(user),
            Body = (body ?? string.Empty).Trim(),
            CreatedAt = SchoolClock.LocalNow,
            Attachments = (attachments ?? Enumerable.Empty<StoredFile>())
                .Select(f => new CommentAttachment
                {
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    Url = f.Url,
                    SizeBytes = f.SizeBytes,
                    IsImage = f.IsImage
                })
                .ToList()
        };

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return comment;
    }

    /// <summary>Whether <paramref name="user"/> may delete <paramref name="comment"/>:
    /// its author, or an administrator.</summary>
    public static bool CanDelete(Comment comment, ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(AppRoles.Admin))
        {
            return true;
        }

        var userId = UserIdOf(user);

        // An empty stored id would otherwise match every anonymous-authored row.
        return !string.IsNullOrEmpty(userId)
               && string.Equals(comment.AuthorUserId, userId, StringComparison.Ordinal);
    }

    /// <returns>True when the comment was deleted; false when it was missing or
    /// the user was not permitted.</returns>
    public async Task<bool> DeleteAsync(int commentId, ClaimsPrincipal? user, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var comment = await db.Comments
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == commentId, ct);

        if (comment is null || !CanDelete(comment, user))
        {
            return false;
        }

        DeleteFiles(comment.Attachments);

        db.Comments.Remove(comment);
        await db.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Removes every comment on a host record. Called when the host itself is
    /// deleted: the thread is addressed by (type, id), so the database cannot
    /// cascade, and without this the next record to reuse that id would inherit a
    /// stranger's discussion.
    /// </summary>
    public async Task DeleteThreadAsync(string threadType, int threadId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var comments = await db.Comments
            .Include(c => c.Attachments)
            .Where(c => c.ThreadType == threadType && c.ThreadId == threadId)
            .ToListAsync(ct);

        if (comments.Count == 0)
        {
            return;
        }

        DeleteFiles(comments.SelectMany(c => c.Attachments));

        db.Comments.RemoveRange(comments);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Best-effort removal of the stored files behind deleted comments.
    /// A failure here must not block the delete — an orphaned file is a tidiness
    /// problem, a half-deleted comment is a correctness one.</summary>
    private void DeleteFiles(IEnumerable<CommentAttachment> attachments)
    {
        foreach (var attachment in attachments)
        {
            try
            {
                var relative = attachment.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var path = Path.Combine(_environment.WebRootPath, relative);

                // Never follow a path that climbed out of wwwroot.
                var root = Path.GetFullPath(_environment.WebRootPath);
                var full = Path.GetFullPath(path);

                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
                {
                    File.Delete(full);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete attachment file {Url}", attachment.Url);
            }
        }
    }

    private static string? UserIdOf(ClaimsPrincipal? user) =>
        user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static string DisplayNameOf(ClaimsPrincipal? user) =>
        user?.FindFirst(AppUserClaimsPrincipalFactory.FullNameClaim)?.Value
        ?? user?.Identity?.Name
        ?? "Pengguna";
}
