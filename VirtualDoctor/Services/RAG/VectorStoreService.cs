using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.AI;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.RAG;

public class VectorStoreService : IVectorStoreService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _eg;
    private readonly ILogger<VectorStoreService> _log;
    private readonly IVectorProvider _provider;

    public VectorStoreService(AppConfig config,
        IEmbeddingGenerator<string, Embedding<float>> eg,
        ILogger<VectorStoreService> log,
        IHttpClientFactory httpFactory)
    {
        _eg = eg;
        _log = log;

        var provider = (config.VectorDb.Provider ?? "InMemory").Trim().ToLowerInvariant();
        _provider = provider switch
        {
            "sqlite" => new SqliteVectorProvider(config.VectorDb, log),
            "qdrant" => new QdrantVectorProvider(config.VectorDb, httpFactory, log),
            "chroma" => new ChromaVectorProvider(config.VectorDb, httpFactory, log),
            "azureaisearch" => new AzureAiSearchVectorProvider(config.VectorDb, httpFactory, log),
            _ => new InMemoryVectorProvider(log)
        };
    }

    public Task InitializeAsync() => _provider.EnsureInitializedAsync(0);

    public async Task IndexDocumentAsync(string id, string content, Dictionary<string, string>? meta = null)
        => await IndexChunksAsync(id, ChunkText(content), meta);

    public async Task IndexChunksAsync(string id, List<string> chunks, Dictionary<string, string>? meta = null)
    {
        var validChunks = chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (validChunks.Count == 0) return;

        var embeddings = await _eg.GenerateAsync(validChunks);
        var records = new List<VectorRecord>();

        for (int i = 0; i < validChunks.Count; i++)
        {
            var vector = embeddings[i].Vector.ToArray();
            records.Add(new VectorRecord
            {
                Id = $"{id}_{i}",
                DocumentId = id,
                Content = validChunks[i],
                Embedding = vector,
                Metadata = meta ?? new()
            });
        }

        await _provider.EnsureInitializedAsync(records[0].Embedding.Length);
        await _provider.UpsertAsync(records);
        _log.LogInformation("[VectorStore] Indexed {C} chunks for '{Id}'", records.Count, id);
    }

    public async Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(string query, int topK = 5)
    {
        var emb = await _eg.GenerateAsync(new[] { query });
        var qvec = emb.First().Vector.ToArray();
        await _provider.EnsureInitializedAsync(qvec.Length);
        return await _provider.SearchAsync(qvec, topK);
    }

    public Task DeleteDocumentAsync(string id) => _provider.DeleteDocumentAsync(id);
    public Task<bool> IsDocumentIndexedAsync(string id) => _provider.IsDocumentIndexedAsync(id);
    public Task<int> GetDocumentCountAsync() => _provider.GetDocumentCountAsync();

    static List<string> ChunkText(string text, int cs = 1000, int ov = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;
        if (text.Length <= cs) { chunks.Add(text); return chunks; }
        var words = text.Split(' '); var cur = new List<string>(); var len = 0;
        foreach (var w in words)
        {
            if (len + w.Length > cs && cur.Count > 0)
            {
                chunks.Add(string.Join(" ", cur));
                var o = cur.Skip(Math.Max(0, cur.Count - ov / 10)).ToList();
                cur = o; len = o.Sum(x => x.Length) + o.Count;
            }
            cur.Add(w); len += w.Length + 1;
        }
        if (cur.Count > 0) chunks.Add(string.Join(" ", cur));
        return chunks;
    }

    static float CosineSim(float[] a, float[] b)
    {
        int n = Math.Min(a.Length, b.Length);
        float d = 0, na = 0, nb = 0;
        for (int i = 0; i < n; i++) { d += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return (na == 0 || nb == 0) ? 0 : d / (MathF.Sqrt(na) * MathF.Sqrt(nb));
    }

    record VectorRecord
    {
        public string Id { get; init; } = string.Empty;
        public string DocumentId { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public float[] Embedding { get; init; } = Array.Empty<float>();
        public Dictionary<string, string> Metadata { get; init; } = new();
    }

    interface IVectorProvider
    {
        Task EnsureInitializedAsync(int vectorSize);
        Task UpsertAsync(IEnumerable<VectorRecord> records);
        Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK);
        Task DeleteDocumentAsync(string documentId);
        Task<bool> IsDocumentIndexedAsync(string documentId);
        Task<int> GetDocumentCountAsync();
    }

    class InMemoryVectorProvider : IVectorProvider
    {
        private readonly ConcurrentDictionary<string, VectorEntry> _store = new();
        private readonly ILogger _log;
        public InMemoryVectorProvider(ILogger log) => _log = log;

        public Task EnsureInitializedAsync(int vectorSize)
        {
            _log.LogInformation("[VectorStore] InMemory ready");
            return Task.CompletedTask;
        }

        public Task UpsertAsync(IEnumerable<VectorRecord> records)
        {
            foreach (var docId in records.Select(r => r.DocumentId).Distinct())
            {
                var keys = _store.Keys.Where(k => k.StartsWith(docId + "_", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var k in keys) _store.TryRemove(k, out _);
            }

            foreach (var r in records)
            {
                _store[r.Id] = new VectorEntry
                {
                    DocumentId = r.DocumentId,
                    Content = r.Content,
                    Embedding = r.Embedding,
                    Metadata = r.Metadata,
                    IndexedAt = DateTime.UtcNow
                };
            }

            return Task.CompletedTask;
        }

        public Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK)
        {
            var results = new List<(string, string, float)>();
            foreach (var (_, e) in _store)
                results.Add((e.DocumentId, e.Content, CosineSim(queryVector, e.Embedding)));
            return Task.FromResult(results.OrderByDescending(r => r.Item3).Take(topK).ToList());
        }

        public Task DeleteDocumentAsync(string documentId)
        {
            var keys = _store.Keys.Where(k => k.StartsWith(documentId + "_", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var k in keys) _store.TryRemove(k, out _);
            return Task.CompletedTask;
        }

        public Task<bool> IsDocumentIndexedAsync(string documentId)
            => Task.FromResult(_store.Keys.Any(k => k.StartsWith(documentId + "_", StringComparison.OrdinalIgnoreCase)));

        public Task<int> GetDocumentCountAsync()
            => Task.FromResult(_store.Values.Select(v => v.DocumentId).Distinct().Count());

        class VectorEntry
        {
            public string DocumentId = "";
            public string Content = "";
            public float[] Embedding = Array.Empty<float>();
            public Dictionary<string, string> Metadata = new();
            public DateTime IndexedAt;
        }
    }

    class SqliteVectorProvider : IVectorProvider
    {
        private readonly VectorDbConfig _cfg;
        private readonly ILogger _log;
        private bool _initialized;

        public SqliteVectorProvider(VectorDbConfig cfg, ILogger log)
        {
            _cfg = cfg;
            _log = log;
        }

        public async Task EnsureInitializedAsync(int vectorSize)
        {
            if (_initialized) return;
            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS vectors (
    id TEXT PRIMARY KEY,
    documentId TEXT NOT NULL,
    content TEXT NOT NULL,
    metadata TEXT NULL,
    embedding TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_vectors_documentId ON vectors(documentId);
";
            await cmd.ExecuteNonQueryAsync();
            _initialized = true;
            _log.LogInformation("[VectorStore] SQLite ready");
        }

        public async Task UpsertAsync(IEnumerable<VectorRecord> records)
        {
            var list = records.ToList();
            await EnsureInitializedAsync(list.First().Embedding.Length);

            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            var docIds = list.Select(r => r.DocumentId).Distinct().ToList();
            foreach (var docId in docIds)
            {
                await using var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM vectors WHERE documentId = $doc";
                del.Parameters.AddWithValue("$doc", docId);
                await del.ExecuteNonQueryAsync();
            }

            foreach (var r in list)
            {
                await using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO vectors (id, documentId, content, metadata, embedding) VALUES ($id, $doc, $content, $meta, $emb)";
                ins.Parameters.AddWithValue("$id", r.Id);
                ins.Parameters.AddWithValue("$doc", r.DocumentId);
                ins.Parameters.AddWithValue("$content", r.Content);
                ins.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(r.Metadata));
                ins.Parameters.AddWithValue("$emb", JsonSerializer.Serialize(r.Embedding));
                await ins.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK)
        {
            var results = new List<(string, string, float)>();
            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT documentId, content, embedding FROM vectors";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var docId = reader.GetString(0);
                var content = reader.GetString(1);
                var embJson = reader.GetString(2);
                var emb = JsonSerializer.Deserialize<float[]>(embJson) ?? Array.Empty<float>();
                results.Add((docId, content, CosineSim(queryVector, emb)));
            }
            return results.OrderByDescending(r => r.Item3).Take(topK).ToList();
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM vectors WHERE documentId = $doc";
            cmd.Parameters.AddWithValue("$doc", documentId);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsDocumentIndexedAsync(string documentId)
        {
            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM vectors WHERE documentId = $doc LIMIT 1";
            cmd.Parameters.AddWithValue("$doc", documentId);
            var res = await cmd.ExecuteScalarAsync();
            return res != null;
        }

        public async Task<int> GetDocumentCountAsync()
        {
            await using var conn = new SqliteConnection(_cfg.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(DISTINCT documentId) FROM vectors";
            var res = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(res ?? 0);
        }
    }

    class QdrantVectorProvider : IVectorProvider
    {
        private readonly VectorDbConfig _cfg;
        private readonly HttpClient _http;
        private readonly ILogger _log;
        private bool _initialized;

        public QdrantVectorProvider(VectorDbConfig cfg, IHttpClientFactory hf, ILogger log)
        {
            _cfg = cfg;
            _log = log;
            _http = hf.CreateClient();
        }

        public async Task EnsureInitializedAsync(int vectorSize)
        {
            if (_initialized || vectorSize == 0) return;
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return;

            var collection = _cfg.CollectionName ?? "health-docs";
            var get = await _http.GetAsync($"{baseUrl}/collections/{collection}");
            if (!get.IsSuccessStatusCode)
            {
                var body = new
                {
                    vectors = new { size = vectorSize, distance = "Cosine" }
                };
                var res = await _http.PutAsJsonAsync($"{baseUrl}/collections/{collection}", body);
                res.EnsureSuccessStatusCode();
            }
            _initialized = true;
            _log.LogInformation("[VectorStore] Qdrant ready");
        }

        public async Task UpsertAsync(IEnumerable<VectorRecord> records)
        {
            var list = records.ToList();
            if (list.Count == 0) return;
            await EnsureInitializedAsync(list[0].Embedding.Length);
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var collection = _cfg.CollectionName ?? "health-docs";

            var payload = new
            {
                points = list.Select(r => new
                {
                    id = r.Id,
                    vector = r.Embedding,
                    payload = new { documentId = r.DocumentId, content = r.Content, metadata = r.Metadata }
                })
            };

            var res = await _http.PutAsJsonAsync($"{baseUrl}/collections/{collection}/points?wait=true", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var collection = _cfg.CollectionName ?? "health-docs";

            var payload = new
            {
                vector = queryVector,
                limit = topK,
                with_payload = true
            };

            var res = await _http.PostAsJsonAsync($"{baseUrl}/collections/{collection}/points/search", payload);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var results = new List<(string, string, float)>();
            foreach (var item in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                var score = item.GetProperty("score").GetSingle();
                var payloadEl = item.GetProperty("payload");
                var docId = payloadEl.GetProperty("documentId").GetString() ?? string.Empty;
                var content = payloadEl.GetProperty("content").GetString() ?? string.Empty;
                results.Add((docId, content, score));
            }
            return results;
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var collection = _cfg.CollectionName ?? "health-docs";
            var payload = new
            {
                filter = new
                {
                    must = new[] { new { key = "documentId", match = new { value = documentId } } }
                }
            };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/collections/{collection}/points/delete?wait=true", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<bool> IsDocumentIndexedAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var collection = _cfg.CollectionName ?? "health-docs";
            var payload = new
            {
                filter = new
                {
                    must = new[] { new { key = "documentId", match = new { value = documentId } } }
                },
                limit = 1
            };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/collections/{collection}/points/scroll", payload);
            if (!res.IsSuccessStatusCode) return false;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("result").GetProperty("points").EnumerateArray().Any();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var collection = _cfg.CollectionName ?? "health-docs";
            var res = await _http.PostAsJsonAsync($"{baseUrl}/collections/{collection}/points/scroll", new { limit = 10000 });
            if (!res.IsSuccessStatusCode) return 0;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var points = doc.RootElement.GetProperty("result").GetProperty("points").EnumerateArray();
            return points.Select(p => p.GetProperty("payload").GetProperty("documentId").GetString() ?? string.Empty).Distinct().Count();
        }
    }

    class ChromaVectorProvider : IVectorProvider
    {
        private readonly VectorDbConfig _cfg;
        private readonly HttpClient _http;
        private readonly ILogger _log;
        private bool _initialized;

        public ChromaVectorProvider(VectorDbConfig cfg, IHttpClientFactory hf, ILogger log)
        {
            _cfg = cfg;
            _log = log;
            _http = hf.CreateClient();
        }

        public async Task EnsureInitializedAsync(int vectorSize)
        {
            if (_initialized || vectorSize == 0) return;
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return;
            var name = _cfg.CollectionName ?? "health-docs";

            var res = await _http.GetAsync($"{baseUrl}/api/v1/collections");
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var exists = doc.RootElement.EnumerateArray().Any(c => c.GetProperty("name").GetString() == name);

            if (!exists)
            {
                var create = await _http.PostAsJsonAsync($"{baseUrl}/api/v1/collections", new { name });
                create.EnsureSuccessStatusCode();
            }
            _initialized = true;
            _log.LogInformation("[VectorStore] Chroma ready");
        }

        public async Task UpsertAsync(IEnumerable<VectorRecord> records)
        {
            var list = records.ToList();
            if (list.Count == 0) return;
            await EnsureInitializedAsync(list[0].Embedding.Length);
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var name = _cfg.CollectionName ?? "health-docs";

            var payload = new
            {
                ids = list.Select(r => r.Id).ToArray(),
                embeddings = list.Select(r => r.Embedding).ToArray(),
                documents = list.Select(r => r.Content).ToArray(),
                metadatas = list.Select(r => new Dictionary<string, object>
                {
                    { "documentId", r.DocumentId },
                    { "metadata", r.Metadata }
                }).ToArray()
            };

            var res = await _http.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{name}/add", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var name = _cfg.CollectionName ?? "health-docs";

            var payload = new
            {
                query_embeddings = new[] { queryVector },
                n_results = topK,
                include = new[] { "documents", "metadatas", "distances" }
            };

            var res = await _http.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{name}/query", payload);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var results = new List<(string, string, float)>();
            var docs = doc.RootElement.GetProperty("documents")[0];
            var metas = doc.RootElement.GetProperty("metadatas")[0];
            var dists = doc.RootElement.GetProperty("distances")[0];

            for (int i = 0; i < docs.GetArrayLength(); i++)
            {
                var content = docs[i].GetString() ?? string.Empty;
                var meta = metas[i];
                var docId = meta.TryGetProperty("documentId", out var docIdEl) ? docIdEl.GetString() ?? string.Empty : string.Empty;
                var dist = dists[i].GetSingle();
                var score = 1 - dist;
                results.Add((docId, content, score));
            }

            return results;
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var name = _cfg.CollectionName ?? "health-docs";
            var payload = new { where = new { documentId } };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{name}/delete", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<bool> IsDocumentIndexedAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var name = _cfg.CollectionName ?? "health-docs";
            var payload = new { where = new { documentId }, limit = 1 };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/api/v1/collections/{name}/get", payload);
            if (!res.IsSuccessStatusCode) return false;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("ids").EnumerateArray().Any();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var name = _cfg.CollectionName ?? "health-docs";
            var res = await _http.GetAsync($"{baseUrl}/api/v1/collections/{name}");
            if (!res.IsSuccessStatusCode) return 0;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("count", out var countEl) ? countEl.GetInt32() : 0;
        }
    }

    class AzureAiSearchVectorProvider : IVectorProvider
    {
        private readonly VectorDbConfig _cfg;
        private readonly HttpClient _http;
        private readonly ILogger _log;
        private bool _initialized;
        private const string ApiVersion = "2023-11-01";

        public AzureAiSearchVectorProvider(VectorDbConfig cfg, IHttpClientFactory hf, ILogger log)
        {
            _cfg = cfg;
            _log = log;
            _http = hf.CreateClient();
        }

        public async Task EnsureInitializedAsync(int vectorSize)
        {
            if (_initialized || vectorSize == 0) return;
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return;
            var index = _cfg.CollectionName ?? "health-docs";

            _http.DefaultRequestHeaders.Remove("api-key");
            _http.DefaultRequestHeaders.Add("api-key", _cfg.ApiKey);

            var get = await _http.GetAsync($"{baseUrl}/indexes/{index}?api-version={ApiVersion}");
            if (!get.IsSuccessStatusCode)
            {
                var payload = new
                {
                    name = index,
                    fields = new object[]
                    {
                        new { name = "id", type = "Edm.String", key = true, filterable = true },
                        new { name = "documentId", type = "Edm.String", filterable = true, sortable = true },
                        new { name = "content", type = "Edm.String", searchable = true },
                        new { name = "metadata", type = "Edm.String", searchable = false },
                        new { name = "embedding", type = "Collection(Edm.Single)", searchable = true, vectorSearchDimensions = vectorSize, vectorSearchProfile = "default" }
                    },
                    vectorSearch = new
                    {
                        algorithms = new[]
                        {
                            new { name = "default", kind = "hnsw", hnswParameters = new { m = 4, efConstruction = 400, efSearch = 500, metric = "cosine" } }
                        },
                        profiles = new[] { new { name = "default", algorithm = "default" } }
                    }
                };

                var create = await _http.PutAsJsonAsync($"{baseUrl}/indexes/{index}?api-version={ApiVersion}", payload);
                create.EnsureSuccessStatusCode();
            }

            _initialized = true;
            _log.LogInformation("[VectorStore] Azure AI Search ready");
        }

        public async Task UpsertAsync(IEnumerable<VectorRecord> records)
        {
            var list = records.ToList();
            if (list.Count == 0) return;
            await EnsureInitializedAsync(list[0].Embedding.Length);
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var index = _cfg.CollectionName ?? "health-docs";

            var docs = list.Select(r => new Dictionary<string, object>
            {
                ["@search.action"] = "mergeOrUpload",
                ["id"] = r.Id,
                ["documentId"] = r.DocumentId,
                ["content"] = r.Content,
                ["metadata"] = JsonSerializer.Serialize(r.Metadata),
                ["embedding"] = r.Embedding
            }).ToArray();

            var payload = new { value = docs };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/index?api-version={ApiVersion}", payload);
            res.EnsureSuccessStatusCode();
        }

        public async Task<List<(string DocumentId, string Content, float Score)>> SearchAsync(float[] queryVector, int topK)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var index = _cfg.CollectionName ?? "health-docs";

            var payload = new
            {
                search = "",
                vectorQueries = new[] { new { kind = "vector", vector = queryVector, fields = "embedding", k = topK } },
                select = "documentId,content"
            };

            var res = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/search?api-version={ApiVersion}", payload);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var results = new List<(string, string, float)>();
            foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
            {
                var docId = item.GetProperty("documentId").GetString() ?? string.Empty;
                var content = item.GetProperty("content").GetString() ?? string.Empty;
                var score = item.GetProperty("@search.score").GetSingle();
                results.Add((docId, content, score));
            }
            return results;
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var index = _cfg.CollectionName ?? "health-docs";

            var searchPayload = new
            {
                search = "*",
                filter = $"documentId eq '{documentId}'",
                select = "id",
                top = 1000
            };

            var searchRes = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/search?api-version={ApiVersion}", searchPayload);
            if (!searchRes.IsSuccessStatusCode) return;
            var json = await searchRes.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var ids = doc.RootElement.GetProperty("value").EnumerateArray().Select(v => v.GetProperty("id").GetString()).Where(id => id != null).ToList();
            if (ids.Count == 0) return;

            var deleteDocs = ids.Select(id => new Dictionary<string, object>
            {
                ["@search.action"] = "delete",
                ["id"] = id!
            }).ToArray();

            var del = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/index?api-version={ApiVersion}", new { value = deleteDocs });
            del.EnsureSuccessStatusCode();
        }

        public async Task<bool> IsDocumentIndexedAsync(string documentId)
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var index = _cfg.CollectionName ?? "health-docs";
            var payload = new { search = "*", filter = $"documentId eq '{documentId}'", top = 1 };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/search?api-version={ApiVersion}", payload);
            if (!res.IsSuccessStatusCode) return false;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("value").EnumerateArray().Any();
        }

        public async Task<int> GetDocumentCountAsync()
        {
            var baseUrl = _cfg.Endpoint?.TrimEnd('/');
            var index = _cfg.CollectionName ?? "health-docs";
            var payload = new { search = "*", top = 0, count = true };
            var res = await _http.PostAsJsonAsync($"{baseUrl}/indexes/{index}/docs/search?api-version={ApiVersion}", payload);
            if (!res.IsSuccessStatusCode) return 0;
            var json = await res.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("@odata.count", out var c) ? c.GetInt32() : 0;
        }
    }
}
