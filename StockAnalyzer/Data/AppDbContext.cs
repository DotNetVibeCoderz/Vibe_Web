using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Models;

namespace StockAnalyzer.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<StockEmiten> StockEmitens { get; set; } = null!;
    public DbSet<TechnicalIndicator> TechnicalIndicators { get; set; } = null!;
    public DbSet<FundamentalData> FundamentalData { get; set; } = null!;
    public DbSet<SentimentData> SentimentData { get; set; } = null!;
    public DbSet<SectorSentiment> SectorSentiments { get; set; } = null!;
    public DbSet<LLMProviderConfig> LLMProviderConfigs { get; set; } = null!;
    public DbSet<LLMAnalysisMapping> LLMAnalysisMappings { get; set; } = null!;
    public DbSet<StockRecommendation> StockRecommendations { get; set; } = null!;
    public DbSet<TopRecommendation> TopRecommendations { get; set; } = null!;
    public DbSet<AppConfiguration> AppConfigurations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>(e => { e.Property(u => u.DisplayName).HasMaxLength(100); e.Property(u => u.PreferredTheme).HasMaxLength(10); });
        modelBuilder.Entity<StockEmiten>(e => { e.HasIndex(s => s.StockCode).IsUnique(); e.HasIndex(s => s.Sector); e.HasIndex(s => s.IsActive); });
        modelBuilder.Entity<TechnicalIndicator>(e => { e.HasIndex(t => new { t.StockEmitenId, t.TradeDate }).IsUnique(); e.HasIndex(t => t.TradeDate); e.HasOne(t => t.StockEmiten).WithMany(s => s.TechnicalIndicators).HasForeignKey(t => t.StockEmitenId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<FundamentalData>(e => { e.HasIndex(f => new { f.StockEmitenId, f.Period }).IsUnique(); e.HasOne(f => f.StockEmiten).WithMany(s => s.FundamentalData).HasForeignKey(f => f.StockEmitenId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<SentimentData>(e => { e.HasIndex(s => s.StockEmitenId); e.HasIndex(s => s.PublishedDate); e.HasOne(s => s.StockEmiten).WithMany(x => x.SentimentData).HasForeignKey(s => s.StockEmitenId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<SectorSentiment>(e => e.HasIndex(s => new { s.Sector, s.AnalysisDate }).IsUnique());
        modelBuilder.Entity<LLMProviderConfig>(e => e.HasIndex(l => l.ProviderName).IsUnique());
        modelBuilder.Entity<LLMAnalysisMapping>(e => { e.HasIndex(l => l.AnalysisType).IsUnique(); e.HasOne(l => l.LLMProviderConfig).WithMany().HasForeignKey(l => l.LLMProviderConfigId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<StockRecommendation>(e => { e.HasIndex(r => new { r.StockEmitenId, r.RecommendationDate }); e.HasOne(r => r.StockEmiten).WithMany(s => s.Recommendations).HasForeignKey(r => r.StockEmitenId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<TopRecommendation>(e => e.HasIndex(t => t.GeneratedDate));
        modelBuilder.Entity<AppConfiguration>(e => e.HasIndex(a => new { a.Category, a.ConfigKey }).IsUnique());
        SeedAll(modelBuilder);
    }

    private static decimal Dr(Random r, double lo, double hi) => (decimal)(r.NextDouble() * (hi - lo) + lo);

    private static void SeedAll(ModelBuilder mb)
    {
        mb.Entity<LLMProviderConfig>().HasData(
            new LLMProviderConfig { Id = 1, ProviderName = "OpenAI", DisplayName = "OpenAI GPT", ApiBaseUrl = "https://api.openai.com/v1", ModelName = "gpt-4o", IsEnabled = false, Priority = 10 },
            new LLMProviderConfig { Id = 2, ProviderName = "Gemini", DisplayName = "Google Gemini", ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta", ModelName = "gemini-2.0-flash", IsEnabled = false, Priority = 20 },
            new LLMProviderConfig { Id = 3, ProviderName = "Anthropic", DisplayName = "Anthropic Claude", ApiBaseUrl = "https://api.anthropic.com/v1", ModelName = "claude-3-sonnet-20240229", IsEnabled = false, Priority = 30 },
            new LLMProviderConfig { Id = 4, ProviderName = "Ollama", DisplayName = "Ollama Local", ApiBaseUrl = "http://localhost:11434", ModelName = "llama3.1", IsEnabled = true, Priority = 40, TimeoutSeconds = 120 },
            new LLMProviderConfig { Id = 5, ProviderName = "OpenAICompatible", DisplayName = "OpenAI Compatible", ApiBaseUrl = "http://localhost:1234/v1", ModelName = "local-model", IsEnabled = false, Priority = 50, TimeoutSeconds = 120 }
        );
        mb.Entity<LLMAnalysisMapping>().HasData(
            new LLMAnalysisMapping { Id = 1, AnalysisType = "TechnicalReview", LLMProviderConfigId = 4 },
            new LLMAnalysisMapping { Id = 2, AnalysisType = "FundamentalReview", LLMProviderConfigId = 4 },
            new LLMAnalysisMapping { Id = 3, AnalysisType = "SentimentAnalysis", LLMProviderConfigId = 2 },
            new LLMAnalysisMapping { Id = 4, AnalysisType = "StockRecommendation", LLMProviderConfigId = 4 }
        );

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        var admin = new ApplicationUser { Id = "seed-admin", UserName = "admin@stockanalyzer.com", NormalizedUserName = "ADMIN@STOCKANALYZER.COM", Email = "admin@stockanalyzer.com", NormalizedEmail = "ADMIN@STOCKANALYZER.COM", EmailConfirmed = true, DisplayName = "Admin", PreferredTheme = "light", SecurityStamp = "S1", ConcurrencyStamp = "C1" };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");
        var demo = new ApplicationUser { Id = "seed-demo", UserName = "demo@stockanalyzer.com", NormalizedUserName = "DEMO@STOCKANALYZER.COM", Email = "demo@stockanalyzer.com", NormalizedEmail = "DEMO@STOCKANALYZER.COM", EmailConfirmed = true, DisplayName = "Demo User", PreferredTheme = "dark", SecurityStamp = "S2", ConcurrencyStamp = "C2" };
        demo.PasswordHash = hasher.HashPassword(demo, "Demo123!");
        mb.Entity<ApplicationUser>().HasData(admin, demo);

        var rng = new Random(42);
        int sid = 1, tid = 1;

        StockSeed(mb, ref sid, ref tid, rng, "BBCA", "Bank Central Asia Tbk", "Banking", "Bank BUKU IV", 10125m, 123.5m, 1250437m);
        StockSeed(mb, ref sid, ref tid, rng, "BBRI", "Bank Rakyat Indonesia Tbk", "Banking", "Bank BUKU IV", 5675m, 151.5m, 859762m);
        StockSeed(mb, ref sid, ref tid, rng, "BMRI", "Bank Mandiri Tbk", "Banking", "Bank BUKU IV", 7250m, 93.3m, 676425m);
        StockSeed(mb, ref sid, ref tid, rng, "BBNI", "Bank Negara Indonesia Tbk", "Banking", "Bank BUKU IV", 5375m, 37.3m, 200487m);
        StockSeed(mb, ref sid, ref tid, rng, "TLKM", "Telkom Indonesia Tbk", "Technology", "Telecom", 3950m, 99.1m, 391445m);
        StockSeed(mb, ref sid, ref tid, rng, "ASII", "Astra International Tbk", "Consumer", "Automotive", 5275m, 40.5m, 213638m);
        StockSeed(mb, ref sid, ref tid, rng, "UNVR", "Unilever Indonesia Tbk", "Consumer", "FMCG", 3950m, 38.1m, 150495m);
        StockSeed(mb, ref sid, ref tid, rng, "ADRO", "Adaro Energy Tbk", "Energy", "Coal", 2850m, 30.7m, 87495m);
        StockSeed(mb, ref sid, ref tid, rng, "PTBA", "Bukit Asam Tbk", "Energy", "Coal", 2875m, 11.2m, 32200m);
        StockSeed(mb, ref sid, ref tid, rng, "ICBP", "Indofood CBP Tbk", "Consumer", "Food", 11575m, 11.7m, 135428m);
        StockSeed(mb, ref sid, ref tid, rng, "INDF", "Indofood Sukses Makmur Tbk", "Consumer", "Food", 7275m, 8.8m, 64020m);
        StockSeed(mb, ref sid, ref tid, rng, "KLBF", "Kalbe Farma Tbk", "Healthcare", "Pharma", 1575m, 46.9m, 73867m);
        StockSeed(mb, ref sid, ref tid, rng, "ANTM", "Aneka Tambang Tbk", "Mining", "Metal", 1675m, 24.0m, 40200m);
        StockSeed(mb, ref sid, ref tid, rng, "MEDC", "Medco Energi Tbk", "Energy", "OilGas", 1450m, 25.1m, 36395m);
        StockSeed(mb, ref sid, ref tid, rng, "PGAS", "Perusahaan Gas Negara Tbk", "Infrastructure", "Gas", 1325m, 24.2m, 32065m);
        StockSeed(mb, ref sid, ref tid, rng, "JSMR", "Jasa Marga Tbk", "Infrastructure", "Toll", 5375m, 7.3m, 39237m);
        StockSeed(mb, ref sid, ref tid, rng, "GOTO", "GoTo Gojek Tokopedia Tbk", "Technology", "Digital", 78m, 1183.9m, 92344m);
        StockSeed(mb, ref sid, ref tid, rng, "BUKA", "Bukalapak.com Tbk", "Technology", "E-Commerce", 148m, 103.1m, 15259m);
        StockSeed(mb, ref sid, ref tid, rng, "MYOR", "Mayora Indah Tbk", "Consumer", "Food", 2575m, 22.4m, 57680m);
        StockSeed(mb, ref sid, ref tid, rng, "CPIN", "Charoen Pokphand Tbk", "Consumer", "Feed", 5375m, 16.4m, 88150m);
        StockSeed(mb, ref sid, ref tid, rng, "SMGR", "Semen Indonesia Tbk", "Infrastructure", "Cement", 5725m, 6.8m, 38930m);
        StockSeed(mb, ref sid, ref tid, rng, "INTP", "Indocement Tunggal Tbk", "Infrastructure", "Cement", 8325m, 3.7m, 30802m);
        StockSeed(mb, ref sid, ref tid, rng, "UNTR", "United Tractors Tbk", "Energy", "Equipment", 24575m, 3.7m, 90927m);
        StockSeed(mb, ref sid, ref tid, rng, "ITMG", "Indo Tambangraya Megah Tbk", "Energy", "Coal", 26750m, 1.1m, 29425m);
        StockSeed(mb, ref sid, ref tid, rng, "EXCL", "XL Axiata Tbk", "Technology", "Telecom", 2370m, 13.1m, 31047m);
    }

    private static void StockSeed(ModelBuilder mb, ref int sid, ref int tid, Random rng,
        string code, string name, string sector, string sub, decimal price, decimal shares, decimal mcap)
    {
        mb.Entity<StockEmiten>().HasData(new StockEmiten
        {
            Id = sid, StockCode = code, CompanyName = name, Sector = sector,
            SubSector = sub, ListedShares = shares, MarketCap = mcap,
            CurrentPrice = price, ChangePercent = Dr(rng, -5, 5),
            IsActive = true, LastUpdated = DateTime.UtcNow
        });

        decimal sp = price;
        for (int i = 0; i < 90; i++)
        {
            var dt = DateTime.UtcNow.Date.AddDays(-90 + i);
            var dm = Dr(rng, -2, 2) / 100m;
            var op = sp * (1m + dm);
            var cl = op * (1m + Dr(rng, -1.5, 1.5) / 100m);
            var hi = Math.Max(op, cl) * (1m + Dr(rng, 0, 2) / 100m);
            var lo = Math.Min(op, cl) * (1m - Dr(rng, 0, 2) / 100m);
            var vol = (long)(rng.NextDouble() * 50_000_000 + 5_000_000);
            var bv = (long)((decimal)vol * Dr(rng, 0.3, 0.7));

            mb.Entity<TechnicalIndicator>().HasData(new TechnicalIndicator
            {
                Id = tid++, StockEmitenId = sid, TradeDate = dt,
                OpenPrice = Math.Round(op, 0), HighPrice = Math.Round(hi, 0),
                LowPrice = Math.Round(lo, 0), ClosePrice = Math.Round(cl, 0),
                AdjustedClose = Math.Round(cl, 0), Volume = vol,
                BuyVolume = bv, SellVolume = vol - bv,
                MA20 = Math.Round(cl * (1m + Dr(rng, -2, 2) / 100m), 0),
                RSI = Dr(rng, 20, 80), MACD = Dr(rng, -50, 50),
                MACDSignal = Dr(rng, -50, 50),
                BollingerUpper = Math.Round(cl * 1.05m, 0),
                BollingerLower = Math.Round(cl * 0.95m, 0),
                DataSource = "Seed"
            });
            sp = cl;
        }

        var perV = Dr(rng, 5, 30);
        var derV = Dr(rng, 0, 2);
        mb.Entity<FundamentalData>().HasData(new FundamentalData
        {
            Id = sid, StockEmitenId = sid, Period = "Q1 2025",
            ReportDate = new DateTime(2025, 3, 31), PER = perV,
            EPS = Math.Round(sp / perV, 2), ROE = Dr(rng, 5, 35),
            ROA = Dr(rng, 2, 22), NetProfitMargin = Dr(rng, 5, 35),
            GrossProfitMargin = Dr(rng, 20, 60), PBV = Dr(rng, 0.5, 5.5),
            DER = derV, CurrentRatio = Dr(rng, 0.5, 3.5),
            RevenueGrowth = Dr(rng, -5, 30), EarningsGrowth = Dr(rng, -5, 35),
            OperatingCashFlow = Dr(rng, 500, 5500),
            FreeCashFlow = Dr(rng, -500, 3000),
            TotalAssets = mcap * 2, TotalLiabilities = mcap * derV,
            TotalEquity = mcap, Revenue = mcap * Dr(rng, 0.1, 0.6),
            NetIncome = mcap * Dr(rng, 0.02, 0.17),
            DataSource = "Seed"
        });
        sid++;
    }
}
