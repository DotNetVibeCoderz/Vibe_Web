using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using VirtualDoctor.Services.Storage;

namespace VirtualDoctor.Services.AI;

public sealed class GeneralPlugin
{
    private readonly IKernelFunctionService _svc;
    public GeneralPlugin(IKernelFunctionService svc) => _svc = svc;

    [KernelFunction, Description("Search the internet for current health information")]
    public async Task<string> SearchInternet([Description("Search query")] string query) => await _svc.SearchInternetAsync(query);

    [KernelFunction, Description("Get current date and time")]
    public string CheckDate()
    {
        var now = DateTime.Now;
        return $"{now:dddd, dd MMMM yyyy, HH:mm} WIB";
    }

    [KernelFunction, Description("Perform mathematical calculations")]
    public string MathCalc([Description("Math expression")] string expr)
    {
        try { return $"Hasil: {new System.Data.DataTable().Compute(expr, null)}"; }
        catch { return "Tidak bisa menghitung."; }
    }

    [KernelFunction, Description("Read file content from a URL")]
    public Task<string> ReadFileFromUrl([Description("URL")] string url) => _svc.ReadFileFromUrlAsync(url);

    [KernelFunction, Description("Describe an image from URL")]
    public Task<string> DescribeImage([Description("Image URL")] string url) => _svc.DescribeImageAsync(url);

    [KernelFunction, Description("Scrape text from a web page")]
    public Task<string> ScrapWebPage([Description("Web URL")] string url) => _svc.ScrapWebPageAsync(url);
}

public sealed class HealthPlugin
{
    private readonly IKernelFunctionService _svc;
    public HealthPlugin(IKernelFunctionService svc) => _svc = svc;

    [KernelFunction, Description("Route medical question to a human doctor")]
    public Task<string> AskDoctor([Description("Medical question")] string q) => _svc.AskDoctorAsync(q);

    [KernelFunction, Description("Search and order medicines")]
    public Task<string> OrderMedicine([Description("Medicine name")] string name, [Description("Quantity")] int qty = 1) => _svc.OrderMedicineAsync(name, qty);

    [KernelFunction, Description("Check doctor schedules and book appointments")]
    public Task<string> ScheduleDoctor([Description("Doctor preference")] string req) => _svc.ScheduleDoctorAsync(req);

    [KernelFunction, Description("Find nearby hospitals and clinics")]
    public Task<string> FindHospital([Description("Location name")] string loc) => _svc.FindHospitalAsync(loc);

    [KernelFunction, Description("Query medical articles via RAG")]
    public Task<string> QueryHealthDocs([Description("Medical question")] string q) => _svc.QueryHealthDocsAsync(q);
}

public class KernelFunctionService : IKernelFunctionService
{
    private readonly ISearchService _search;
    private readonly RAG.IRagQueryService _rag;
    private readonly IMedicineService _meds;
    private readonly IHospitalService _hosp;
    private readonly HttpClient _http;
    private readonly ILogger<KernelFunctionService> _log;

    public KernelFunctionService(ISearchService search, RAG.IRagQueryService rag, IMedicineService meds, IHospitalService hosp, IHttpClientFactory hf, ILogger<KernelFunctionService> log)
    {
        _search = search; _rag = rag; _meds = meds; _hosp = hosp;
        _http = hf.CreateClient("LlmClient"); _log = log;
    }

    public void RegisterAllPlugins(Kernel kernel)
    {
        kernel.Plugins.AddFromObject(new GeneralPlugin(this), "GeneralUtilities");
        kernel.Plugins.AddFromObject(new HealthPlugin(this), "HealthServices");
        _log.LogInformation("[SK] Plugins registered");
    }

    public async Task<string> SearchInternetAsync(string q) { try { return await _search.SearchHealthAsync(q); } catch { return "Pencarian gagal."; } }
    public Task<string> CheckDateAsync() => Task.FromResult($"{DateTime.Now:dddd, dd MMMM yyyy, HH:mm} WIB");
    public Task<string> MathCalcAsync(string e) { try { return Task.FromResult($"Hasil: {new System.Data.DataTable().Compute(e, null)}"); } catch { return Task.FromResult("Tidak bisa menghitung."); } }
    public async Task<string> ReadFileFromUrlAsync(string url) { try { var r = await _http.GetAsync(url); r.EnsureSuccessStatusCode(); var c = await r.Content.ReadAsStringAsync(); return c.Length > 5000 ? c[..5000] + "..." : c; } catch (Exception ex) { return $"Gagal: {ex.Message}"; } }
    public Task<string> DescribeImageAsync(string url) => Task.FromResult($"Gambar di {url}.");
    public async Task<string> ScrapWebPageAsync(string url) { try { var h = await _http.GetStringAsync(url); var t = System.Text.RegularExpressions.Regex.Replace(h, "<[^>]*>", " "); t = System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim(); return t.Length > 5000 ? t[..5000] + "..." : t; } catch (Exception ex) { return $"Gagal: {ex.Message}"; } }
    public Task<string> AskDoctorAsync(string q) => Task.FromResult("Konsultasi diteruskan. Darurat: 119 / IGD.");
    public async Task<string> OrderMedicineAsync(string name, int qty) { try { var m = await _meds.SearchAsync(name); if (m.Any()) { var x = m.First(); return $"✅ {x.Name} Rp{x.Price:N0}/item. Total: Rp{x.Price * qty:N0}. Kunjungi Farmasi."; } return $"❌ '{name}' tidak ditemukan."; } catch { return "Gagal."; } }
    public Task<string> ScheduleDoctorAsync(string r) => Task.FromResult("Kunjungi menu Booking untuk buat janji.");
    public async Task<string> FindHospitalAsync(string loc) { try { var h = await _hosp.SearchAsync(loc); if (h.Any()) { var l = h.Take(5).Select(x => $"- {x.Name} ({x.Type}), ⭐{x.Rating}"); return $"Ditemukan {h.Count}:\n{string.Join("\n", l)}"; } return "Tidak ditemukan."; } catch { return "Gagal."; } }
    public async Task<string> QueryHealthDocsAsync(string q) { try { return await _rag.QueryAsync(q); } catch { return "Gagal query."; } }
}
