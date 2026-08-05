using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SMSNet.Data;
using SMSNet.Models;

namespace SMSNet.Services.Assistant;

/// <summary>A file the user attached to a message, already saved to storage.</summary>
public sealed record PendingAttachment(
    string FileName,
    string ContentType,
    string Url,
    long SizeBytes,
    AttachmentKind Kind,
    string AbsolutePath);

/// <summary>
/// Conversation management and turn execution for the assistant.
/// <para>
/// Everything here opens its own DI scope. The chat page holds a long-lived
/// Blazor circuit, and reusing that circuit's <c>ApplicationDbContext</c> across
/// an await-heavy turn is the classic source of "A second operation was started
/// on this context" in Blazor Server.
/// </para>
/// </summary>
public sealed class AssistantService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AssistantKernelFactory _kernelFactory;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        IServiceScopeFactory scopeFactory,
        AssistantKernelFactory kernelFactory,
        ILogger<AssistantService> logger)
    {
        _scopeFactory = scopeFactory;
        _kernelFactory = kernelFactory;
        _logger = logger;
    }

    public AssistantOptions Options => _kernelFactory.Options;

    // --- Sessions ----------------------------------------------------------

    public async Task<List<ChatSession>> ListSessionsAsync(string userId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.ChatSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatSession> CreateSessionAsync(string userId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var options = _kernelFactory.Options;
        var session = new ChatSession
        {
            UserId = userId,
            Title = "Percakapan baru",
            Provider = options.Provider,
            Model = options.ResolveModel(),
            CreatedAt = SchoolClock.LocalNow,
            UpdatedAt = SchoolClock.LocalNow
        };

        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(ct);

        return session;
    }

    public async Task<bool> DeleteSessionAsync(string userId, int sessionId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Scoping by UserId as well as id is what stops one user deleting another's thread.
        var session = await db.ChatSessions
            .Include(s => s.Messages).ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session is null)
        {
            return false;
        }

        db.ChatSessions.Remove(session);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Clears the thread's messages but keeps the session itself.</summary>
    public async Task<bool> ResetSessionAsync(string userId, int sessionId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.ChatSessions
            .Include(s => s.Messages).ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);

        if (session is null)
        {
            return false;
        }

        db.ChatMessages.RemoveRange(session.Messages);
        session.Title = "Percakapan baru";
        session.UpdatedAt = SchoolClock.LocalNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task RenameSessionAsync(string userId, int sessionId, string title, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (session is null)
        {
            return;
        }

        session.Title = Truncate(title, 160);
        session.UpdatedAt = SchoolClock.LocalNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(string userId, int sessionId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var owns = await db.ChatSessions.AnyAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        if (!owns)
        {
            return new List<ChatMessage>();
        }

        return await db.ChatMessages.AsNoTracking()
            .Include(m => m.Attachments)
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.Id)
            .ToListAsync(ct);
    }

    // --- Turn execution ----------------------------------------------------

    /// <summary>
    /// Persists the user's turn, asks the model, and persists the answer.
    /// The reply is stored even when generation fails, so the thread shows what
    /// happened instead of silently dropping the question.
    /// </summary>
    public async Task<ChatMessage> SendAsync(
        AssistantUserContext user,
        int sessionId,
        string prompt,
        IReadOnlyList<PendingAttachment> attachments,
        CancellationToken ct = default)
    {
        var options = _kernelFactory.Options;

        if (!options.IsConfigured(out var reason))
        {
            return await PersistAsync(user.UserId, sessionId, prompt, attachments,
                $"Asisten belum dikonfigurasi. {reason}", null, isError: true, ct);
        }

        try
        {
            var history = await BuildHistoryAsync(user, sessionId, prompt, attachments, ct);
            var (kernel, settings) = _kernelFactory.Build(user);
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var result = await chat.GetChatMessageContentsAsync(history, settings, kernel, ct);
            var reply = string.Join("\n\n", result
                .Select(r => r.Content)
                .Where(c => !string.IsNullOrWhiteSpace(c)));

            if (string.IsNullOrWhiteSpace(reply))
            {
                reply = "Maaf, saya tidak memperoleh jawaban untuk pertanyaan itu. Coba ulangi dengan kalimat lain.";
            }

            var tools = result
                .SelectMany(r => ExtractTools(r.Metadata))
                .Distinct()
                .ToArray();

            return await PersistAsync(user.UserId, sessionId, prompt, attachments, reply,
                tools.Length > 0 ? string.Join(", ", tools) : null, isError: false, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assistant turn failed for session {SessionId}", sessionId);

            return await PersistAsync(user.UserId, sessionId, prompt, attachments,
                $"Maaf, terjadi kendala saat menghubungi model ({options.Provider}). Detail: {ex.Message}",
                null, isError: true, ct);
        }
    }

    private async Task<ChatHistory> BuildHistoryAsync(
        AssistantUserContext user,
        int sessionId,
        string prompt,
        IReadOnlyList<PendingAttachment> attachments,
        CancellationToken ct)
    {
        var options = _kernelFactory.Options;
        var history = new ChatHistory();

        history.AddSystemMessage($"""
            {options.ResolveSystemPrompt()}

            Konteks pengguna saat ini:
            - Nama    : {user.DisplayName}
            - Peran   : {user.RoleLabel}
            - Tanggal : {SchoolClock.Now:dddd, dd MMMM yyyy HH:mm} {SchoolClock.TimeZoneLabel}

            Sesuaikan kedalaman jawaban dengan peran tersebut. Bila sebuah fungsi menolak
            permintaan karena peran tidak berwenang, sampaikan penolakan itu dengan sopan
            dan jangan mencoba memperolehnya lewat cara lain.
            """);

        var previous = await GetMessagesAsync(user.UserId, sessionId, ct);

        foreach (var message in previous.TakeLast(options.HistoryWindow))
        {
            if (message.IsError)
            {
                continue; // a failure notice is UI state, not conversation
            }

            if (message.Role == "assistant")
            {
                history.AddAssistantMessage(message.Content);
            }
            else
            {
                history.AddUserMessage(message.Content);
            }
        }

        history.Add(BuildUserTurn(prompt, attachments));
        return history;
    }

    private static ChatMessageContent BuildUserTurn(string prompt, IReadOnlyList<PendingAttachment> attachments)
    {
        var items = new ChatMessageContentItemCollection();
        var text = prompt?.Trim() ?? string.Empty;

        // Documents can't be sent as vision input, so their links are named in the
        // text and the model can pull them back through baca_file_dari_url.
        var documents = attachments.Where(a => a.Kind == AttachmentKind.Document).ToList();
        if (documents.Count > 0)
        {
            var list = string.Join("\n", documents.Select(d => $"- {d.FileName}: {d.Url}"));
            text = $"{text}\n\nBerkas terlampir:\n{list}".Trim();
        }

        if (text.Length > 0)
        {
            items.Add(new TextContent(text));
        }

        foreach (var image in attachments.Where(a => a.Kind == AttachmentKind.Image))
        {
            try
            {
                var bytes = File.ReadAllBytes(image.AbsolutePath);
                items.Add(new ImageContent(bytes, image.ContentType));
            }
            catch (IOException)
            {
                items.Add(new TextContent($"[Gambar {image.FileName} gagal dibaca dari penyimpanan.]"));
            }
        }

        return new ChatMessageContent(AuthorRole.User, items);
    }

    private async Task<ChatMessage> PersistAsync(
        string userId,
        int sessionId,
        string prompt,
        IReadOnlyList<PendingAttachment> attachments,
        string reply,
        string? toolsUsed,
        bool isError,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct)
                      ?? throw new InvalidOperationException("Sesi percakapan tidak ditemukan.");

        var userMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "user",
            Content = prompt?.Trim() ?? string.Empty,
            CreatedAt = SchoolClock.LocalNow,
            Attachments = attachments.Select(a => new ChatAttachment
            {
                FileName = a.FileName,
                ContentType = a.ContentType,
                Url = a.Url,
                SizeBytes = a.SizeBytes,
                Kind = a.Kind
            }).ToList()
        };

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "assistant",
            Content = reply,
            ToolsUsed = toolsUsed,
            IsError = isError,
            CreatedAt = SchoolClock.LocalNow
        };

        db.ChatMessages.Add(userMessage);
        db.ChatMessages.Add(assistantMessage);

        // Name the thread from its opening question, the way a subject line works.
        var isFirstTurn = !await db.ChatMessages.AnyAsync(m => m.ChatSessionId == sessionId, ct);
        if (isFirstTurn && !string.IsNullOrWhiteSpace(prompt))
        {
            session.Title = Truncate(prompt.Trim(), 60);
        }

        session.UpdatedAt = SchoolClock.LocalNow;
        await db.SaveChangesAsync(ct);

        return assistantMessage;
    }

    private static IEnumerable<string> ExtractTools(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata is null || !metadata.TryGetValue("ToolsUsed", out var value))
        {
            return Array.Empty<string>();
        }

        return value switch
        {
            string[] array => array,
            IEnumerable<string> list => list,
            string single => new[] { single },
            _ => Array.Empty<string>()
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max].TrimEnd() + "…";
}
