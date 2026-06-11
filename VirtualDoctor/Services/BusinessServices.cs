using Microsoft.EntityFrameworkCore;
using VirtualDoctor.Data;
using VirtualDoctor.Models;

namespace VirtualDoctor.Services;

public class ConsultationService : IConsultationService
{
    private readonly AppDbContext _db;
    public ConsultationService(AppDbContext db) => _db = db;
    public async Task<Consultation?> StartAsync(string uid, string did, ConsultationType t) { var d = await _db.Doctors.FindAsync(did); if (d == null) return null; var c = new Consultation { UserId = uid, DoctorId = did, Type = t, Fee = d.ConsultationFee, Status = ConsultationStatus.InProgress }; _db.Consultations.Add(c); await _db.SaveChangesAsync(); return c; }
    public async Task<bool> SendMessageAsync(string cid, string sid, string sn, string msg) { _db.ConsultationMessages.Add(new ConsultationMessage { ConsultationId = cid, SenderId = sid, SenderName = sn, Message = msg }); return await _db.SaveChangesAsync() > 0; }
    public async Task<List<ConsultationMessage>> GetMessagesAsync(string cid) => await _db.ConsultationMessages.Where(m => m.ConsultationId == cid).OrderBy(m => m.SentAt).ToListAsync();
    public async Task<List<Consultation>> GetUserConsultationsAsync(string uid) => await _db.Consultations.Include(c => c.Doctor).Where(c => c.UserId == uid).OrderByDescending(c => c.StartedAt).ToListAsync();
    public async Task<List<Consultation>> GetDoctorConsultationsAsync(string did) => await _db.Consultations.Include(c => c.User).Where(c => c.DoctorId == did).OrderByDescending(c => c.StartedAt).ToListAsync();
    public async Task<Consultation?> GetByIdAsync(string id) => await _db.Consultations.Include(c => c.Doctor).Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
    public async Task<bool> EndAsync(string cid) { var c = await _db.Consultations.FindAsync(cid); if (c == null) return false; c.Status = ConsultationStatus.Completed; c.EndedAt = DateTime.UtcNow; return await _db.SaveChangesAsync() > 0; }
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;
    public async Task<Order?> CreateOrderAsync(Order o) { _db.Orders.Add(o); return await _db.SaveChangesAsync() > 0 ? o : null; }
    public async Task<List<Order>> GetUserOrdersAsync(string uid) => await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Medicine).Where(o => o.UserId == uid).OrderByDescending(o => o.CreatedAt).ToListAsync();
    public async Task<Order?> GetByIdAsync(string id) => await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Medicine).FirstOrDefaultAsync(o => o.Id == id);
    public async Task<bool> UpdateStatusAsync(string id, OrderStatus s) { var o = await _db.Orders.FindAsync(id); if (o == null) return false; o.Status = s; if (s == OrderStatus.Delivered) o.DeliveredAt = DateTime.UtcNow; return await _db.SaveChangesAsync() > 0; }
    public async Task<bool> CancelAsync(string id) { var o = await _db.Orders.FindAsync(id); if (o == null) return false; o.Status = OrderStatus.Cancelled; return await _db.SaveChangesAsync() > 0; }
}

public class HomecareAppService : IHomecareService
{
    private readonly AppDbContext _db;
    public HomecareAppService(AppDbContext db) => _db = db;
    public async Task<HomecareService?> BookAsync(HomecareService s) { _db.HomecareServices.Add(s); return await _db.SaveChangesAsync() > 0 ? s : null; }
    public async Task<List<HomecareService>> GetUserServicesAsync(string uid) => await _db.HomecareServices.Where(h => h.UserId == uid).OrderByDescending(h => h.CreatedAt).ToListAsync();
    public async Task<bool> UpdateStatusAsync(string id, HomecareServiceStatus s) { var x = await _db.HomecareServices.FindAsync(id); if (x == null) return false; x.Status = s; return await _db.SaveChangesAsync() > 0; }

    public async Task<List<HomecareService>> GetAllAsync() => await _db.HomecareServices.Include(h => h.User).OrderByDescending(h => h.CreatedAt).ToListAsync();

    public async Task<HomecareService> CreateAsync(HomecareService s)
    {
        _db.HomecareServices.Add(s);
        await _db.SaveChangesAsync();
        return s;
    }

    public async Task<HomecareService?> UpdateAsync(HomecareService s)
    {
        var existing = await _db.HomecareServices.FindAsync(s.Id);
        if (existing == null) return null;
        _db.Entry(existing).CurrentValues.SetValues(s);
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var hc = await _db.HomecareServices.FindAsync(id);
        if (hc == null) return false;
        hc.Status = HomecareServiceStatus.Cancelled;
        await _db.SaveChangesAsync();
        return true;
    }
}

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;
    public ArticleService(AppDbContext db) => _db = db;

    public async Task<List<HealthArticle>> GetAllAsync() => await _db.HealthArticles.OrderByDescending(a => a.PublishedAt).ToListAsync();

    public async Task<(List<HealthArticle> Items, int TotalCount)> GetPagedAsync(int page, int size, string? search = null, string? category = null, bool? isIndexed = null)
    {
        var q = _db.HealthArticles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) { var s = search.ToLower(); q = q.Where(a => a.Title.ToLower().Contains(s) || (a.Summary != null && a.Summary.ToLower().Contains(s))); }
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(a => a.Category == category);
        if (isIndexed.HasValue) q = q.Where(a => a.IsIndexed == isIndexed.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.PublishedAt).Skip((page - 1) * size).Take(size).ToListAsync();
        return (items, total);
    }

    public async Task<HealthArticle?> GetByIdAsync(string id) => await _db.HealthArticles.FindAsync(id);
    public async Task<List<HealthArticle>> SearchAsync(string q) => await _db.HealthArticles.Where(a => a.Title.Contains(q) || a.Content.Contains(q)).ToListAsync();
    public async Task<List<HealthArticle>> GetByCategoryAsync(string c) => await _db.HealthArticles.Where(a => a.Category == c).OrderByDescending(a => a.PublishedAt).ToListAsync();
    public async Task<HealthArticle> CreateAsync(HealthArticle a) { a.Id = Guid.NewGuid().ToString(); if (a.PublishedAt == default) a.PublishedAt = DateTime.UtcNow; _db.HealthArticles.Add(a); await _db.SaveChangesAsync(); return a; }
    public async Task<HealthArticle> UpdateAsync(HealthArticle a) { var e = await _db.HealthArticles.FindAsync(a.Id) ?? throw new KeyNotFoundException(); e.Title = a.Title; e.Summary = a.Summary; e.Content = a.Content; e.Author = a.Author; e.Category = a.Category; e.ImageUrl = a.ImageUrl; e.PdfUrl = a.PdfUrl; e.SourceUrl = a.SourceUrl; await _db.SaveChangesAsync(); return e; }
    public async Task<bool> DeleteAsync(string id) { var a = await _db.HealthArticles.FindAsync(id); if (a == null) return false; _db.HealthArticles.Remove(a); return await _db.SaveChangesAsync() > 0; }
    public async Task<int> GetTotalCountAsync() => await _db.HealthArticles.CountAsync();
    public async Task<List<string>> GetCategoriesAsync() => await _db.HealthArticles.Where(a => a.Category != null).Select(a => a.Category!).Distinct().ToListAsync();
}

public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;
    public RecommendationService(AppDbContext db) => _db = db;
    public async Task<List<Medicine>> RecommendMedicinesAsync(string uid) => await _db.Medicines.Where(m => m.IsActive).OrderByDescending(m => m.Rating).Take(6).ToListAsync();
    public Task<List<string>> RecommendServicesAsync(string uid) => Task.FromResult(new List<string> { "Cek kesehatan rutin", "Konsultasi gizi", "Vitamin booster" });
    public Task<string> GetHealthTipAsync(string uid)
    {
        var tips = new[] { "💧 Minum 8 gelas air putih setiap hari.", "🏃 Luangkan 30 menit olahraga ringan.", "😴 Tidur 7-8 jam setiap malam.", "🥗 Konsumsi sayur & buah minimal 5 porsi.", "☀️ Berjemur pagi 10-15 menit." };
        return Task.FromResult(tips[Random.Shared.Next(tips.Length)]);
    }
}

public class InsuranceService : IInsuranceService
{
    public Task<bool> VerifyInsuranceAsync(string p, string n) => Task.FromResult(new[] { "BPJS", "Prudential", "Allianz", "Manulife", "AIA" }.Contains(p) && n.Length > 5);
    public Task<decimal> CalculateCoverageAsync(string p, string n, decimal c) { var r = p switch { "BPJS" => 0.8m, "Prudential" => 0.9m, "Allianz" => 0.85m, "Manulife" => 0.88m, _ => 0.5m }; return Task.FromResult(c * r); }
    public Task<List<string>> GetProvidersAsync() => Task.FromResult(new List<string> { "BPJS", "Prudential", "Allianz", "Manulife", "AIA" });
}
