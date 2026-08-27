using System.Text;
using Lapak.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Lapak.Services.Rag;

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DocumentName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}

public class VectorSearchResult
{
    public string Content { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public interface IVectorRagService
{
    Task IndexDocumentsAsync(CancellationToken ct = default);
    Task<List<VectorSearchResult>> SearchAsync(string query, int topK = 5, CancellationToken ct = default);
    Task<string> GetRagContextAsync(string query, int topK = 3);
    int GetChunkCount();
    DateTime LastIndexedAt { get; }
}

/// <summary>
/// In-memory TF-IDF index over the files in the configured document folder.
/// </summary>
/// <remarks>
/// The index is built off to the side and swapped in under a short lock. Reads
/// never block on file I/O, and — importantly — no lock is ever held across an
/// <c>await</c>: doing that with a thread-affine lock leaves it permanently
/// stuck when the continuation resumes on a different thread.
/// </remarks>
public class VectorRagService : IVectorRagService
{
    /// <summary>An immutable index generation. Swapping the reference is atomic.</summary>
    private sealed record IndexSnapshot(
        IReadOnlyList<DocumentChunk> Chunks,
        IReadOnlyDictionary<string, Dictionary<string, double>> InvertedIndex,
        DateTime IndexedAt)
    {
        public static readonly IndexSnapshot Empty = new(
            Array.Empty<DocumentChunk>(),
            new Dictionary<string, Dictionary<string, double>>(),
            DateTime.MinValue);
    }

    private readonly VectorDatabaseConfig _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<VectorRagService> _logger;

    private volatile IndexSnapshot _snapshot = IndexSnapshot.Empty;

    /// <summary>Serialises re-indexing so the background timer and a manual reindex don't duplicate work.</summary>
    private readonly SemaphoreSlim _indexGate = new(1, 1);

    public VectorRagService(IOptions<VectorDatabaseConfig> config, IWebHostEnvironment env, ILogger<VectorRagService> logger)
    {
        _config = config.Value;
        _env = env;
        _logger = logger;
    }

    public DateTime LastIndexedAt => _snapshot.IndexedAt;

    public int GetChunkCount() => _snapshot.Chunks.Count;

    public async Task IndexDocumentsAsync(CancellationToken ct = default)
    {
        var docsPath = Path.Combine(_env.ContentRootPath, _config.DocumentFolderPath);
        if (!Directory.Exists(docsPath))
        {
            Directory.CreateDirectory(docsPath);
            _logger.LogInformation("Created empty document folder at {Path}", docsPath);
            return;
        }

        var files = Directory.GetFiles(docsPath, "*.*").Where(IsSupportedFile).ToList();
        if (files.Count == 0)
        {
            _logger.LogInformation("No indexable documents found in {Path}", docsPath);
            return;
        }

        await _indexGate.WaitAsync(ct);
        try
        {
            var chunks = new List<DocumentChunk>();
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    chunks.AddRange(ChunkDocument(content, Path.GetFileName(file)));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipped unreadable document: {File}", file);
                }
            }

            _snapshot = new IndexSnapshot(chunks, BuildInvertedIndex(chunks), DateTime.UtcNow);

            _logger.LogInformation(
                "Indexed {ChunkCount} chunks from {FileCount} documents", chunks.Count, files.Count);
        }
        finally
        {
            _indexGate.Release();
        }
    }

    public Task<List<VectorSearchResult>> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var snapshot = _snapshot;

        if (snapshot.Chunks.Count == 0)
        {
            return Task.FromResult(new List<VectorSearchResult>
            {
                new() { Content = "Belum ada dokumen kebijakan yang terindeks.", DocumentName = "System", RelevanceScore = 0 }
            });
        }

        var queryTerms = Tokenize(query);
        var scores = new Dictionary<string, double>();

        foreach (var chunk in snapshot.Chunks)
        {
            double score = 0;
            foreach (var term in queryTerms)
            {
                if (snapshot.InvertedIndex.TryGetValue(term, out var postings) &&
                    postings.TryGetValue(chunk.Id, out var tfidf))
                    score += tfidf;
            }

            // An exact phrase hit is worth more than the sum of its terms.
            if (score > 0 && chunk.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                score *= 1.5;

            if (score > 0) scores[chunk.Id] = score;
        }

        var byId = snapshot.Chunks.ToDictionary(c => c.Id);

        var results = scores
            .OrderByDescending(kv => kv.Value)
            .Take(topK)
            .Select(kv => new VectorSearchResult
            {
                Content = byId[kv.Key].Content,
                DocumentName = byId[kv.Key].DocumentName,
                RelevanceScore = Math.Round(kv.Value, 4)
            })
            .ToList();

        return Task.FromResult(results);
    }

    public async Task<string> GetRagContextAsync(string query, int topK = 3)
    {
        var results = await SearchAsync(query, topK);
        if (results.Count == 0) return "Tidak ada informasi relevan.";

        var sb = new StringBuilder();
        foreach (var r in results)
        {
            sb.AppendLine($"--- Dari: {r.DocumentName} ---");
            sb.AppendLine(r.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private List<DocumentChunk> ChunkDocument(string content, string docName)
    {
        var chunks = new List<DocumentChunk>();
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        var index = 0;

        foreach (var paragraph in paragraphs)
        {
            if (current.Length + paragraph.Length > _config.ChunkSize && current.Length > 0)
            {
                var text = current.ToString().Trim();
                chunks.Add(new DocumentChunk { DocumentName = docName, Content = text, ChunkIndex = index++ });

                // Carry the tail of the previous chunk forward so an answer that
                // straddles a boundary is still retrievable from one chunk.
                current.Clear();
                if (_config.ChunkOverlap > 0 && text.Length > _config.ChunkOverlap)
                    current.AppendLine(text[^_config.ChunkOverlap..]);
            }
            current.AppendLine(paragraph);
        }

        if (current.Length > 0)
            chunks.Add(new DocumentChunk { DocumentName = docName, Content = current.ToString().Trim(), ChunkIndex = index });

        return chunks;
    }

    /// <summary>
    /// Builds TF-IDF postings in two passes. Document frequency has to be known
    /// for the whole corpus before any weight is computed, so a single streaming
    /// pass would score early chunks against an incomplete corpus.
    /// </summary>
    private static Dictionary<string, Dictionary<string, double>> BuildInvertedIndex(List<DocumentChunk> chunks)
    {
        var termFrequencies = new List<(string ChunkId, Dictionary<string, int> Terms)>(chunks.Count);
        var documentFrequency = new Dictionary<string, int>();

        foreach (var chunk in chunks)
        {
            var tf = new Dictionary<string, int>();
            foreach (var term in TokenizeAll(chunk.Content))
            {
                tf.TryGetValue(term, out var count);
                tf[term] = count + 1;
            }

            termFrequencies.Add((chunk.Id, tf));

            foreach (var term in tf.Keys)
            {
                documentFrequency.TryGetValue(term, out var df);
                documentFrequency[term] = df + 1;
            }
        }

        var index = new Dictionary<string, Dictionary<string, double>>();
        var totalChunks = Math.Max(chunks.Count, 1);

        foreach (var (chunkId, terms) in termFrequencies)
        {
            foreach (var (term, freq) in terms)
            {
                var idf = Math.Log((double)totalChunks / documentFrequency[term]) + 1;

                if (!index.TryGetValue(term, out var postings))
                    index[term] = postings = new Dictionary<string, double>();

                postings[chunkId] = freq * idf;
            }
        }

        return index;
    }

    /// <summary>Distinct query terms — repeats in a question add no signal.</summary>
    private static List<string> Tokenize(string text) => TokenizeAll(text).Distinct().ToList();

    /// <summary>Every term occurrence, needed for term frequency.</summary>
    private static IEnumerable<string> TokenizeAll(string text) => text.ToLowerInvariant()
        .Split(
            new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'', '-', '/' },
            StringSplitOptions.RemoveEmptyEntries)
        .Where(t => t.Length >= 2);

    private static bool IsSupportedFile(string path) =>
        Path.GetExtension(path).ToLowerInvariant() is ".txt" or ".md" or ".csv" or ".html" or ".htm" or ".json";
}

/// <summary>Re-indexes the document folder on a timer so edits are picked up without a restart.</summary>
public class VectorIndexingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VectorIndexingBackgroundService> _logger;
    private readonly VectorDatabaseConfig _config;

    public VectorIndexingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<VectorDatabaseConfig> config,
        ILogger<VectorIndexingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Vector indexing started. Interval: {Minutes} min", _config.ReindexIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IVectorRagService>().IndexDocumentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Document indexing failed; will retry on the next interval");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_config.ReindexIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
