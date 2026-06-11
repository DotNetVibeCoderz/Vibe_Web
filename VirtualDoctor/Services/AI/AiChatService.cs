using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using VirtualDoctor.Data;
using SK = Microsoft.SemanticKernel.ChatCompletion;

namespace VirtualDoctor.Services.AI;

public class AiChatService : IAiChatService
{
    private readonly AppDbContext _db;
    private readonly ILlmProviderFactory _llmFactory;
    private readonly IKernelFunctionService _kfs;
    private readonly Models.AppConfig _cfg;
    private readonly ILogger<AiChatService> _log;
    private string _provider;

    public AiChatService(AppDbContext db, ILlmProviderFactory f, IKernelFunctionService kfs, Models.AppConfig cfg, ILogger<AiChatService> log)
    { _db = db; _llmFactory = f; _kfs = kfs; _cfg = cfg; _log = log; _provider = cfg.Llm.DefaultProvider; }

    public async Task<string> SendMessageAsync(string uid, string cid, string msg, string? p = null, string? img = null, string? doc = null)
    {
        p ??= _provider;
        var chat = await GetOrCreateChat(uid, cid);
        _db.ChatMessages.Add(new Models.ChatMessage { ChatHistoryId = chat.Id, Role = "user", Content = msg, ImageUrl = img, DocumentUrl = doc, SentAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        string resp = await GetAiResponse(p, chat, msg, img, doc);

        _db.ChatMessages.Add(new Models.ChatMessage { ChatHistoryId = chat.Id, Role = "assistant", Content = resp, SentAt = DateTime.UtcNow });
        chat.UpdatedAt = DateTime.UtcNow;
        chat.Title = GenerateTitle(chat, msg);
        await _db.SaveChangesAsync();
        return resp;
    }

    public async IAsyncEnumerable<string> SendStreamingMessageAsync(string uid, string cid, string msg, string? p = null, string? img = null, string? doc = null)
    {
        p ??= _provider;
        var chat = await GetOrCreateChat(uid, cid);
        _db.ChatMessages.Add(new Models.ChatMessage { ChatHistoryId = chat.Id, Role = "user", Content = msg, ImageUrl = img, DocumentUrl = doc, SentAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var resp = await GetAiResponse(p, chat, msg, img, doc);

        // Simulasikan streaming dengan delay antar kata
        var words = resp.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            yield return words[i] + (i < words.Length - 1 ? " " : "");
            await Task.Delay(25);
        }

        _db.ChatMessages.Add(new Models.ChatMessage { ChatHistoryId = chat.Id, Role = "assistant", Content = resp, SentAt = DateTime.UtcNow });
        chat.UpdatedAt = DateTime.UtcNow;
        chat.Title = GenerateTitle(chat, msg);
        await _db.SaveChangesAsync();
    }

    // ============ Core AI Logic ============

    private async Task<string> GetAiResponse(string provider, Models.ChatHistory chat, string msg, string? img, string? doc)
    {
        // 1. Coba Semantic Kernel dengan function calling
        try
        {
            var k = _llmFactory.GetKernel(provider);
            _kfs.RegisterAllPlugins(k);
            var cs = k.GetRequiredService<IChatCompletionService>();
            var s = _llmFactory.GetExecutionSettings(provider, enableFunctions: true);
            var h = BuildHistory(chat, msg, img, doc);

            var result = await cs.GetChatMessageContentAsync(h, s, k);
            var content = result.Content;
            if (!string.IsNullOrWhiteSpace(content))
            {
                _log.LogInformation("[AI] SK response OK, provider={P}", provider);
                return content;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[AI] SK failed, using local fallback. Provider={P}", provider);
        }

        // 2. FALLBACK: Local response pakai KernelFunctionService
        return await GenerateLocalResponse(msg, img, doc);
    }

    /// <summary>
    /// Local response generator - tidak butuh API key.
    /// Menggunakan pattern matching + kernel functions.
    /// </summary>
    private async Task<string> GenerateLocalResponse(string msg, string? img, string? doc)
    {
        var lower = msg.ToLower().Trim();
        var botName = _cfg.Llm.BotName;

        // Greetings
        if (lower is "halo" or "hai" or "hi" or "hello" or "selamat pagi" or "selamat siang" or "selamat malam" or "assalamualaikum")
            return $"Halo! 👋 Saya {botName}, siap membantu pertanyaan kesehatan Anda. Silakan tanyakan keluhan atau informasi kesehatan yang Anda butuhkan. 😊";

        // Emergency check
        if (lower.Contains("darurat") || lower.Contains("gawat") || lower.Contains("ambulan") || lower.Contains("119"))
            return "⚠️ **PERHATIAN!** Jika ini kondisi darurat medis, segera hubungi **119** atau kunjungi **IGD terdekat**. Jangan tunda! Untuk konsultasi non-darurat, silakan lanjutkan pertanyaan Anda.";

        // Image detection
        if (!string.IsNullOrEmpty(img))
            return $"📸 Saya menerima gambar Anda. Untuk analisis gambar yang akurat, diperlukan AI Vision (multi-modal). Saran saya: pastikan gambar yang dikirim jelas dan relevan. Anda bisa konsultasi langsung ke dokter melalui menu **Konsultasi**.";

        // Document detection
        if (!string.IsNullOrEmpty(doc))
            return $"📄 Saya menerima dokumen Anda. Untuk membaca dan menganalisis isi dokumen, gunakan fitur **RAG Artikel** di halaman Artikel. Upload PDF ke Admin Artikel lalu indexing, setelah itu bisa ditanyakan via AI Chat.";

        // Intent: Search Internet
        if (lower.Contains("cari") && (lower.Contains("internet") || lower.Contains("google") || lower.Contains("informasi terbaru")))
        { var q = CleanQuery(msg); return await _kfs.SearchInternetAsync(q); }

        // Intent: Date/Time
        if (lower.Contains("tanggal") || lower.Contains("hari ini") || lower.Contains("jam sekarang") || lower.Contains("waktu"))
            return await _kfs.CheckDateAsync();

        // Intent: Math
        if ((lower.Contains("hitung") || lower.Contains("berapa")) && HasMathExpression(lower))
            return await _kfs.MathCalcAsync(ExtractMath(lower));

        // Intent: Medicine/Pharmacy
        if (lower.Contains("obat") || lower.Contains("vitamin") || lower.Contains("suplemen") || lower.Contains("farmasi"))
            return await _kfs.OrderMedicineAsync(ExtractMedicineName(msg), 1);

        // Intent: Hospital/Clinic
        if (lower.Contains("rumah sakit") || lower.Contains("rs ") || lower.Contains("klinik") || lower.Contains("puskesmas") || lower.Contains("dokter terdekat"))
            return await _kfs.FindHospitalAsync(ExtractLocation(msg));

        // Intent: Doctor/Schedule
        if ((lower.Contains("jadwal") || lower.Contains("booking") || lower.Contains("janji")) && lower.Contains("dokter"))
            return await _kfs.ScheduleDoctorAsync(msg);

        // Intent: Articles/RAG
        if (lower.Contains("artikel") || lower.Contains("jurnal") || lower.Contains("penelitian") || lower.Contains("referensi medis"))
            return await _kfs.QueryHealthDocsAsync(msg);

        // Intent: Health consultation
        if (lower.Contains("sakit") || lower.Contains("nyeri") || lower.Contains("demam") || lower.Contains("batuk") ||
            lower.Contains("pilek") || lower.Contains("gejala") || lower.Contains("diagnosa") || lower.Contains("penyakit") ||
            lower.Contains("kolesterol") || lower.Contains("diabetes") || lower.Contains("hipertensi") || lower.Contains("alergi"))
        {
            return GenerateHealthResponse(msg, botName);
        }

        // Intent: Tips
        if (lower.Contains("tips") || lower.Contains("saran") || lower.Contains("rekomendasi") || lower.Contains("sehat"))
        {
            var tips = new[] {
                "💧 Minum 8 gelas air putih setiap hari untuk hidrasi optimal.",
                "🏃 Luangkan 30 menit olahraga ringan seperti jalan kaki setiap hari.",
                "😴 Tidur 7-8 jam per malam untuk pemulihan tubuh.",
                "🥗 Konsumsi 5 porsi sayur & buah setiap hari.",
                "🧘 Lakukan stretching pagi 5 menit untuk fleksibilitas otot.",
                "☀️ Berjemur pagi 10-15 menit untuk vitamin D alami.",
                "🫁 Latihan napas dalam 5 menit per hari kurangi stres.",
                "🩺 Cek kesehatan rutin minimal 1 tahun sekali."
            };
            var tip = tips[Random.Shared.Next(tips.Length)];
            return $"💡 **Tips Kesehatan:** {tip}\n\nUntuk rekomendasi yang lebih personal, konsultasikan dengan dokter melalui menu **Konsultasi Dokter**.";
        }

        // Default: general health response
        return $"Terima kasih atas pertanyaannya! 🙏\n\nSaya {botName}, dan berikut saran saya:\n\n" +
               $"1. Untuk konsultasi lebih mendalam, silakan gunakan menu **Konsultasi Dokter**.\n" +
               $"2. Cek artikel kesehatan kami di menu **Artikel** (dilengkapi AI RAG).\n" +
               $"3. Untuk membeli obat, kunjungi menu **Farmasi**.\n\n" +
               $"Jika ada keluhan spesifik, ceritakan lebih detail agar saya bisa membantu lebih lanjut. Untuk kondisi darurat, hubungi **119** segera! 🚑";
    }

    private string GenerateHealthResponse(string msg, string botName)
    {
        if (msg.ToLower().Contains("sakit kepala") || msg.ToLower().Contains("pusing"))
            return "Untuk sakit kepala, beberapa penyebab umum: dehidrasi, kurang tidur, stres, atau tegang otot.\n\n" +
                   "💡 **Saran:**\n1. Minum air putih yang cukup\n2. Istirahat di ruangan tenang\n3. Kompres dingin di dahi\n4. Paracetamol bisa membantu (tersedia di Farmasi)\n\n" +
                   "⚠️ Segera ke dokter jika: sakit kepala hebat mendadak, disertai demam tinggi, kaku leher, atau gangguan penglihatan.";

        if (msg.ToLower().Contains("demam") || msg.ToLower().Contains("panas"))
            return "Untuk demam, ini adalah respons alami tubuh melawan infeksi.\n\n" +
                   "💡 **Saran:**\n1. Kompres air hangat di dahi & ketiak\n2. Banyak minum air putih\n3. Istirahat cukup\n4. Paracetamol bisa membantu (tersedia di Farmasi)\n\n" +
                   "⚠️ Segera ke dokter jika: demam >39°C, lebih dari 3 hari, atau disertai kejang.";

        if (msg.ToLower().Contains("batuk") || msg.ToLower().Contains("pilek") || msg.ToLower().Contains("flu"))
            return "Untuk batuk & pilek, biasanya disebabkan infeksi virus yang sembuh sendiri dalam 5-7 hari.\n\n" +
                   "💡 **Saran:**\n1. Minum air hangat + madu + lemon\n2. Istirahat cukup\n3. Gunakan humidifier\n4. Hindari merokok & polusi\n\n" +
                   "⚠️ Segera ke dokter jika: batuk >2 minggu, batuk darah, sesak napas, atau demam tinggi.";

        if (msg.ToLower().Contains("nyeri"))
            return "Nyeri bisa disebabkan berbagai faktor: cedera, peradangan, atau kondisi kronis.\n\n" +
                   "💡 **Saran:**\n1. Kompres sesuai jenis nyeri (dingin untuk bengkak, hangat untuk otot tegang)\n2. Istirahatkan area yang nyeri\n3. Ibuprofen bisa membantu (tersedia di Farmasi)\n\n" +
                   "⚠️ Segera ke dokter jika: nyeri hebat, tidak membaik >3 hari, atau disertai gejala lain.";

        return $"Saya memahami keluhan Anda. Sebagai {botName}, saya sarankan untuk:\n\n" +
               "1. Catat gejala yang dirasakan (kapan mulai, seberapa parah)\n" +
               "2. Konsultasi dengan dokter untuk diagnosis yang akurat\n" +
               "3. Jangan mendiagnosis sendiri\n\n" +
               "Silakan gunakan menu **Konsultasi Dokter** untuk konsultasi langsung. Untuk kondisi darurat, hubungi **119**. 🚑";
    }

    // ============ Helpers ============

    private static string CleanQuery(string msg) => msg.Replace("cari", "").Replace("internet", "").Replace("google", "").Replace("informasi", "").Replace("di", "").Replace("tentang", "").Trim();

    private static bool HasMathExpression(string text) => text.Contains('+') || text.Contains('-') || text.Contains('*') || text.Contains('/') || text.Contains("kali") || text.Contains("bagi") || text.Contains("tambah") || text.Contains("kurang");

    private static string ExtractMath(string text) => text.Replace("hitung", "").Replace("kalkulasi", "").Replace("berapa", "").Trim();

    private static string ExtractMedicineName(string msg) => msg.Replace("obat", "").Replace("vitamin", "").Replace("suplemen", "").Replace("cari", "").Replace("beli", "").Replace("pesan", "").Trim();

    private static string ExtractLocation(string msg) => msg.Replace("cari", "").Replace("rumah sakit", "").Replace("rs", "").Replace("klinik", "").Replace("puskesmas", "").Replace("terdekat", "").Replace("di", "").Trim();

    private static string GenerateTitle(Models.ChatHistory chat, string msg)
    {
        if (chat.Title != "Konsultasi Baru") return chat.Title;
        return msg.Length > 40 ? msg[..40] + "..." : msg;
    }

    SK.ChatHistory BuildHistory(Models.ChatHistory chat, string msg, string? img, string? doc)
    {
        var h = new SK.ChatHistory();
        h.AddSystemMessage(_cfg.Llm.SystemPrompt);
        var recent = chat.Messages?.OrderByDescending(m => m.SentAt).Take(10).Reverse().ToList();
        if (recent != null)
        {
            foreach (var m in recent)
            {
                if (m.Role == "user") h.AddUserMessage(m.Content);
                else if (m.Role == "assistant") h.AddAssistantMessage(m.Content);
            }
        }

        var contentItems = new ChatMessageContentItemCollection();
        contentItems.Add(new TextContent(msg));

        if (!string.IsNullOrEmpty(doc))
        {
            contentItems.Add(new TextContent($"\n[Dokumen: {doc}]") );
        }

        if (!string.IsNullOrEmpty(img))
        {
            contentItems.Add(new ImageContent(new Uri(img)));
        }

        h.AddMessage(AuthorRole.User, contentItems);
        return h;
    }

    async Task<Models.ChatHistory> GetOrCreateChat(string uid, string cid)
    {
        var c = await _db.ChatHistories.Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == cid);
        if (c == null) { c = new Models.ChatHistory { Id = cid, UserId = uid, Title = "Konsultasi Baru" }; _db.ChatHistories.Add(c); await _db.SaveChangesAsync(); }
        return c;
    }

    // Standard CRUD
    public async Task<List<Models.ChatHistory>> GetUserChatsAsync(string uid) => await _db.ChatHistories.Where(c => c.UserId == uid).OrderByDescending(c => c.UpdatedAt).ToListAsync();
    public async Task<Models.ChatHistory?> GetChatAsync(string cid) => await _db.ChatHistories.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == cid);
    public async Task<Models.ChatHistory> CreateChatAsync(string uid, string t = "Konsultasi Baru") { var c = new Models.ChatHistory { UserId = uid, Title = t }; _db.ChatHistories.Add(c); await _db.SaveChangesAsync(); return c; }
    public async Task<bool> DeleteChatAsync(string cid) { var c = await _db.ChatHistories.Include(x => x.Messages).FirstOrDefaultAsync(x => x.Id == cid); if (c == null) return false; _db.ChatMessages.RemoveRange(c.Messages); _db.ChatHistories.Remove(c); return await _db.SaveChangesAsync() > 0; }
    public async Task ClearChatAsync(string cid) { _db.ChatMessages.RemoveRange(await _db.ChatMessages.Where(m => m.ChatHistoryId == cid).ToListAsync()); await _db.SaveChangesAsync(); }
    public Task<string> GetCurrentProviderAsync() => Task.FromResult(_provider);
    public Task SetProviderAsync(string p) { _provider = p; return Task.CompletedTask; }
    public Task<string> GetBotNameAsync() => Task.FromResult(_cfg.Llm.BotName);
    public Task SetBotNameAsync(string n) { _cfg.Llm.BotName = n; return Task.CompletedTask; }
    public Task UpdateSystemPromptAsync(string p) { _cfg.Llm.SystemPrompt = p; return Task.CompletedTask; }
    public Task UpdateTemperatureAsync(double t) { _cfg.Llm.Temperature = t; return Task.CompletedTask; }
}
