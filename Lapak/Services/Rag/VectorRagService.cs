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
}

public class VectorRagService : IVectorRagService, IDisposable
{
    private readonly VectorDatabaseConfig _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<VectorRagService> _logger;
    private readonly List<DocumentChunk> _chunks = new();
    private readonly Dictionary<string, Dictionary<string, double>> _invertedIndex = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private DateTime _lastIndexed = DateTime.MinValue;

    public VectorRagService(IOptions<VectorDatabaseConfig> config, IWebHostEnvironment env, ILogger<VectorRagService> logger)
    {
        _config = config.Value; _env = env; _logger = logger;
    }

    public async Task IndexDocumentsAsync(CancellationToken ct = default)
    {
        var docsPath = Path.Combine(_env.ContentRootPath, _config.DocumentFolderPath);
        if (!Directory.Exists(docsPath)) { Directory.CreateDirectory(docsPath); return; }
        var files = Directory.GetFiles(docsPath, "*.*").Where(f => IsSupportedFile(f)).ToList();
        if (files.Count == 0) return;

        _lock.EnterWriteLock();
        try
        {
            _chunks.Clear(); _invertedIndex.Clear();
            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    var chunks = ChunkDocument(content, Path.GetFileName(file));
                    foreach (var chunk in chunks) { _chunks.Add(chunk); IndexChunk(chunk); }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to index: {File}", file); }
            }
            _lastIndexed = DateTime.UtcNow;
        }
        finally { _lock.ExitWriteLock(); }
    }

    public async Task<List<VectorSearchResult>> SearchAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            if (_chunks.Count == 0) return new() { new() { Content = "Belum ada dokumen kebijakan.", DocumentName = "System", RelevanceScore = 1.0 } };
            var queryTerms = Tokenize(query); var scores = new Dictionary<string, double>();
            foreach (var chunk in _chunks)
            {
                double score = 0;
                foreach (var term in queryTerms)
                    if (_invertedIndex.TryGetValue(term, out var postings) && postings.TryGetValue(chunk.Id, out var tfidf)) score += tfidf;
                if (chunk.Content.Contains(query, StringComparison.OrdinalIgnoreCase)) score *= 1.5;
                if (score > 0) scores[chunk.Id] = score;
            }
            return scores.OrderByDescending(kv => kv.Value).Take(topK).Select(kv =>
            {
                var chunk = _chunks.First(c => c.Id == kv.Key);
                return new VectorSearchResult { Content = chunk.Content, DocumentName = chunk.DocumentName, RelevanceScore = Math.Round(kv.Value, 4) };
            }).ToList();
        }
        finally { _lock.ExitReadLock(); }
    }

    public async Task<string> GetRagContextAsync(string query, int topK = 3)
    {
        var results = await SearchAsync(query, topK);
        if (results.Count == 0) return "Tidak ada informasi relevan.";
        var sb = new StringBuilder();
        foreach (var r in results) { sb.AppendLine($"--- Dari: {r.DocumentName} ---"); sb.AppendLine(r.Content); sb.AppendLine(); }
        return sb.ToString();
    }

    private List<DocumentChunk> ChunkDocument(string content, string docName)
    {
        var chunks = new List<DocumentChunk>(); var paras = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var cur = new StringBuilder(); int idx = 0;
        foreach (var p in paras)
        {
            if (cur.Length + p.Length > _config.ChunkSize && cur.Length > 0)
            {
                chunks.Add(new DocumentChunk { DocumentName = docName, Content = cur.ToString().Trim(), ChunkIndex = idx++ });
                cur.Clear();
            }
            cur.AppendLine(p);
        }
        if (cur.Length > 0) chunks.Add(new DocumentChunk { DocumentName = docName, Content = cur.ToString().Trim(), ChunkIndex = idx });
        return chunks;
    }

    private void IndexChunk(DocumentChunk chunk)
    {
        var terms = Tokenize(chunk.Content); var tf = new Dictionary<string, int>();
        foreach (var t in terms) { tf.TryGetValue(t, out var c); tf[t] = c + 1; }
        foreach (var (term, freq) in tf)
        {
            if (!_invertedIndex.ContainsKey(term)) _invertedIndex[term] = new();
            double idf = Math.Log((double)_chunks.Count / (_invertedIndex[term].Count + 1)) + 1;
            _invertedIndex[term][chunk.Id] = freq * idf;
        }
    }

    private static List<string> Tokenize(string text) => text.ToLowerInvariant()
        .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(t => t.Length >= 2).Distinct().ToList();

    private static bool IsSupportedFile(string path) => Path.GetExtension(path).ToLowerInvariant() is ".txt" or ".md" or ".csv" or ".html" or ".htm" or ".json";

    public int GetChunkCount() { _lock.EnterReadLock(); try { return _chunks.Count; } finally { _lock.ExitReadLock(); } }
    public void Dispose() => _lock?.Dispose();
}

public class VectorIndexingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _sf; private readonly ILogger<VectorIndexingBackgroundService> _log; private readonly VectorDatabaseConfig _cfg;
    public VectorIndexingBackgroundService(IServiceScopeFactory sf, IOptions<VectorDatabaseConfig> cfg, ILogger<VectorIndexingBackgroundService> log)
    { _sf = sf; _cfg = cfg.Value; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        _log.LogInformation("Vector indexing started. Interval: {M} min", _cfg.ReindexIntervalMinutes);
        while (!st.IsCancellationRequested)
        {
            try { using var s = _sf.CreateScope(); await s.ServiceProvider.GetRequiredService<IVectorRagService>().IndexDocumentsAsync(st); }
            catch (Exception ex) { _log.LogError(ex, "Indexing error"); }
            await Task.Delay(TimeSpan.FromMinutes(_cfg.ReindexIntervalMinutes), st);
        }
    }
}
