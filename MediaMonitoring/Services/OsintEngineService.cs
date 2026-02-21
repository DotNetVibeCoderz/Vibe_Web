using MediaMonitoring.Data;
using MediaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaMonitoring.Services
{
    /// <summary>
    /// Engine utama untuk OSINT: Crawling (Simulasi), Processing, dan Analysis.
    /// Dalam produksi, bagian crawling akan dihubungkan ke API nyata atau Web Scraper.
    /// </summary>
    public class OsintEngineService
    {
        private readonly MediaMonitoringContext _context;
        private readonly ConfigService _configService;
        private static readonly Random _random = new Random();

        // Data dummy untuk simulasi crawling yang realistis
        private static readonly string[] Sources = { "Twitter", "Facebook", "DetikNews", "Kompas", "Reddit", "BlogSpot", "YouTube", "Instagram" };
        private static readonly string[] Categories = { "Politik", "Ekonomi", "Keamanan", "Teknologi", "Sosial", "Bencana", "Kesehatan" };
        private static readonly string[] Locations = { "Jakarta", "Bandung", "Surabaya", "Medan", "Makassar", "Online", "Global" };
        
        // Contoh kata kunci untuk simulasi konten
        private static readonly string[] Topics = {
            "Pemilu 2024 semakin panas dengan debat kandidat yang sengit.",
            "Harga saham teknologi meroket akibat rilis AI terbaru.",
            "Banjir bandang melanda beberapa wilayah, warga mengungsi.",
            "Peluncuran smartphone terbaru dengan fitur hologram.",
            "Konflik geopolitik di timur tengah mempengaruhi harga minyak.",
            "Virus baru terdeteksi, WHO menyarankan vaksinasi ulang.",
            "Timnas Indonesia menang telak dalam pertandingan persahabatan.",
            "Kebijakan baru pemerintah tentang pajak digital menuai pro kontra."
        };

        public OsintEngineService(MediaMonitoringContext context, ConfigService configService)
        {
            _context = context;
            _configService = configService;
        }

        /// <summary>
        /// Menjalankan siklus crawling (Simulasi).
        /// Mengambil data dummy, menganalisisnya, dan menyimpan ke database.
        /// </summary>
        public async Task<int> RunCrawlingCycleAsync()
        {
            var maxPosts = int.Parse(await _configService.GetValueAsync("MaxPostsPerCrawl", "10"));
            var newPostsCount = 0;

            // Simulasi mengambil data dari berbagai sumber
            for (int i = 0; i < maxPosts; i++)
            {
                var source = Sources[_random.Next(Sources.Length)];
                var topic = Topics[_random.Next(Topics.Length)];
                
                // Tambahkan variasi agar terlihat alami
                var noise = _random.Next(1000, 9999);
                var content = $"{topic} [ID: {noise}] #trending #viral";
                
                var sentiment = AnalyzeSentimentSimple(content);
                var category = CategorizeContentSimple(content);

                var post = new MediaPost
                {
                    Source = source,
                    Content = content,
                    Url = $"https://{source.ToLower()}.com/post/{noise}",
                    Author = $"User_{_random.Next(100, 999)}",
                    PostedAt = DateTime.UtcNow.AddMinutes(-_random.Next(0, 60)),
                    Sentiment = sentiment.Label,
                    SentimentScore = sentiment.Score,
                    Category = category,
                    Location = Locations[_random.Next(Locations.Length)],
                    Language = "id",
                    IsProcessed = true,
                    Tags = GenerateTags(content)
                };

                _context.MediaPosts.Add(post);
                newPostsCount++;
            }

            await _context.SaveChangesAsync();
            
            // Cek aturan alert
            await CheckAlertRulesAsync();

            return newPostsCount;
        }

        /// <summary>
        /// Mendapatkan posting terbaru.
        /// </summary>
        public async Task<List<MediaPost>> GetRecentPostsAsync(int count = 10)
        {
            return await _context.MediaPosts
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Analisis sentimen sederhana berbasis kata kunci (Rule-based).
        /// Dalam produksi, gunakan Model ML/NLP seperti BERT atau Azure Text Analytics.
        /// </summary>
        private (string Label, double Score) AnalyzeSentimentSimple(string text)
        {
            var lowerText = text.ToLower();
            double score = 0.0;

            var positiveWords = new[] { "menang", "sukses", "bagus", "hebat", "naik", "untung", "sembuh", "meroket" };
            var negativeWords = new[] { "kalah", "gagal", "buruk", "jelek", "turun", "rugi", "sakit", "konflik", "banjir", "virus" };

            foreach (var word in positiveWords)
            {
                if (lowerText.Contains(word)) score += 0.2;
            }

            foreach (var word in negativeWords)
            {
                if (lowerText.Contains(word)) score -= 0.2;
            }

            // Normalisasi score antara -1 sampai 1
            if (score > 1) score = 1;
            if (score < -1) score = -1;

            string label = "Neutral";
            if (score > 0.1) label = "Positive";
            else if (score < -0.1) label = "Negative";

            return (label, Math.Round(score, 2));
        }

        /// <summary>
        /// Klasifikasi kategori sederhana berbasis kata kunci.
        /// </summary>
        private string CategorizeContentSimple(string text)
        {
            var lowerText = text.ToLower();

            if (lowerText.Contains("pilih") || lowerText.Contains("partai") || lowerText.Contains("presiden") || lowerText.Contains("politik")) return "Politik";
            if (lowerText.Contains("saham") || lowerText.Contains("uang") || lowerText.Contains("ekonomi") || lowerText.Contains("harga") || lowerText.Contains("pajak")) return "Ekonomi";
            if (lowerText.Contains("perang") || lowerText.Contains("tentara") || lowerText.Contains("polisi") || lowerText.Contains("konflik")) return "Keamanan";
            if (lowerText.Contains("komputer") || lowerText.Contains("internet") || lowerText.Contains("ai") || lowerText.Contains("smartphone")) return "Teknologi";
            if (lowerText.Contains("banjir") || lowerText.Contains("gempa") || lowerText.Contains("bencana")) return "Bencana";
            if (lowerText.Contains("virus") || lowerText.Contains("obat") || lowerText.Contains("rs") || lowerText.Contains("sehat")) return "Kesehatan";

            return "Sosial"; // Default
        }

        private string GenerateTags(string text)
        {
            // Ekstrak hashtag sederhana atau buat tags otomatis
            return "trending, viral, osint, monitoring";
        }

        /// <summary>
        /// Memeriksa apakah ada postingan baru yang memicu aturan Alert.
        /// </summary>
        private async Task CheckAlertRulesAsync()
        {
            var rules = await _context.AlertRules.Where(r => r.IsActive).ToListAsync();
            if (!rules.Any()) return;

            var recentPosts = await _context.MediaPosts
                .Where(p => p.CreatedAt > DateTime.UtcNow.AddMinutes(-5)) // Cek 5 menit terakhir
                .ToListAsync();

            foreach (var rule in rules)
            {
                foreach (var post in recentPosts)
                {
                    if (post.Content.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase) || 
                        (post.Tags != null && post.Tags.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Trigger found!
                        rule.TriggerCount++;
                        
                        // Di sini bisa ditambahkan logika kirim email/notifikasi
                        Console.WriteLine($"ALERT TRIGGERED: Keyword '{rule.Keyword}' found in post from {post.Source}. Severity: {rule.Severity}");
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Mendapatkan statistik ringkas untuk dashboard.
        /// </summary>
        public async Task<Dictionary<string, object>> GetDashboardStatsAsync()
        {
            var totalPosts = await _context.MediaPosts.CountAsync();
            var positiveCount = await _context.MediaPosts.CountAsync(p => p.Sentiment == "Positive");
            var negativeCount = await _context.MediaPosts.CountAsync(p => p.Sentiment == "Negative");
            var neutralCount = await _context.MediaPosts.CountAsync(p => p.Sentiment == "Neutral");

            // Trending categories
            var categoryStats = await _context.MediaPosts
                .GroupBy(p => p.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // Source distribution
            var sourceStats = await _context.MediaPosts
                .GroupBy(p => p.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                { "TotalPosts", totalPosts },
                { "Positive", positiveCount },
                { "Negative", negativeCount },
                { "Neutral", neutralCount },
                { "Categories", categoryStats },
                { "Sources", sourceStats }
            };
        }

        /// <summary>
        /// Mendapatkan data untuk grafik tren waktu (last N hours).
        /// </summary>
        public async Task<List<object>> GetTimeSeriesDataAsync(int hours = 24)
        {
            var startTime = DateTime.UtcNow.AddHours(-hours);
            
            var data = await _context.MediaPosts
                .Where(p => p.CreatedAt >= startTime)
                .GroupBy(p => new { Date = p.CreatedAt.Date, Hour = p.CreatedAt.Hour })
                .Select(g => new { 
                    Time = g.Key.Date.AddHours(g.Key.Hour), 
                    Count = g.Count(),
                    Positive = g.Count(x => x.Sentiment == "Positive"),
                    Negative = g.Count(x => x.Sentiment == "Negative")
                })
                .OrderBy(x => x.Time)
                .ToListAsync();

            return data.Cast<object>().ToList();
        }
    }
}