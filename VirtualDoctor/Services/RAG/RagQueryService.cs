using Microsoft.SemanticKernel.ChatCompletion;
using VirtualDoctor.Models;
using VirtualDoctor.Services.AI;

namespace VirtualDoctor.Services.RAG;

public class RagQueryService : IRagQueryService
{
    private readonly IVectorStoreService _vs;
    private readonly ILlmProviderFactory _llm;
    private readonly IArticleService _as;
    private readonly AppConfig _cfg;

    public RagQueryService(IVectorStoreService vs, ILlmProviderFactory llm, IArticleService a, AppConfig cfg)
    { _vs = vs; _llm = llm; _as = a; _cfg = cfg; }

    public async Task<string> QueryAsync(string question, string? provider = null)
    {
        var docs = await _vs.SearchAsync(question, 5);
        if (!docs.Any()) return "Belum ada artikel yang relevan. Silakan coba pertanyaan lain.";

        var ctx = new System.Text.StringBuilder();
        foreach (var (id, content, score) in docs)
        {
            var t = content.Length > 2000 ? content[..2000] + "..." : content;
            ctx.AppendLine($"--- Artikel {id} (relevansi {score:P1}) ---").AppendLine(t).AppendLine();
        }

        var prompt = $"Konteks artikel kesehatan:\n{ctx}\n\nPertanyaan: {question}\n\nJawab berdasarkan konteks. Jangan mengarang.";

        var kernel = _llm.GetKernel(provider, 0.3);
        var cs = kernel.GetRequiredService<IChatCompletionService>();
        var settings = _llm.GetExecutionSettings(provider, 0.3, false);

        var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        history.AddSystemMessage("Asisten riset kesehatan. Jawab berdasarkan data yang diberikan.");
        history.AddUserMessage(prompt);

        var result = await cs.GetChatMessageContentAsync(history, settings);
        return result.Content ?? "Tidak dapat menghasilkan jawaban.";
    }

    public async Task<List<(HealthArticle Article, float Score)>> FindRelevantArticlesAsync(string query, int topK = 5)
    {
        var results = await _vs.SearchAsync(query, topK);
        var articles = new List<(HealthArticle, float)>();
        foreach (var (id, _, score) in results)
        {
            var a = await _as.GetByIdAsync(id);
            if (a != null) articles.Add((a, score));
            else { var all = await _as.SearchAsync(id); if (all.Any()) articles.Add((all.First(), score)); }
        }
        return articles;
    }
}
