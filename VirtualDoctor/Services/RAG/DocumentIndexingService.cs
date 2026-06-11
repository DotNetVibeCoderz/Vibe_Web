using UglyToad.PdfPig;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services.RAG;

public class DocumentIndexingService : IDocumentIndexingService
{
    private readonly IVectorStoreService _vs;
    public DocumentIndexingService(IVectorStoreService vs) => _vs = vs;

    public async Task IndexPdfFileAsync(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var text = await ExtractTextFromPdfAsync(path);
            if (string.IsNullOrWhiteSpace(text)) return;
            var chunks = ChunkText(text);
            await _vs.IndexChunksAsync(Path.GetFileNameWithoutExtension(path), chunks, new() { ["source"] = path, ["filename"] = Path.GetFileName(path) });
        }
        catch (Exception ex) { Console.WriteLine($"[Index] Error: {ex.Message}"); }
    }

    public async Task IndexPdfFolderAsync(string folder)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var f in Directory.GetFiles(folder, "*.pdf", SearchOption.AllDirectories))
            await IndexPdfFileAsync(f);
    }

    public async Task<string> ExtractTextFromPdfAsync(string path)
    {
        try
        {
            using var pdf = PdfDocument.Open(path);
            var sb = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages()) { var t = page.Text; if (!string.IsNullOrWhiteSpace(t)) sb.AppendLine(t); }
            return sb.ToString();
        }
        catch { return ""; }
    }

    public List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 200)
    {
        var chunks = new List<string>();
        if (string.IsNullOrEmpty(text)) return chunks;
        if (text.Length <= chunkSize) { chunks.Add(text); return chunks; }
        var words = text.Split(' ');
        var cur = new List<string>(); var len = 0;
        foreach (var w in words)
        {
            if (len + w.Length > chunkSize && cur.Count > 0) { chunks.Add(string.Join(" ", cur)); var ov = cur.Skip(Math.Max(0, cur.Count - overlap / 10)).ToList(); cur = ov; len = ov.Sum(x => x.Length) + ov.Count; }
            cur.Add(w); len += w.Length + 1;
        }
        if (cur.Count > 0) chunks.Add(string.Join(" ", cur));
        return chunks;
    }

    public async Task ReindexAllAsync() { var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "HealthPdfs"); if (!Directory.Exists(folder)) Directory.CreateDirectory(folder); await IndexPdfFolderAsync(folder); }
}
