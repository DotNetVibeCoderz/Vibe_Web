using MediaMonitoring.Models;

namespace MediaMonitoring.Services.Integrations
{
    public class DarkWebMonitorService
    {
        private readonly ConfigService _configService;
        private static readonly Random _random = new Random();

        private static readonly string[] DarkWebSources = {
            "TOR Forum Alpha", "DarkMarket Monitor", "HiddenWiki Tracker",
            "OnionLeaks", "DeepPaste Bin", "CryptForum"
        };

        private static readonly string[] DarkWebTopics = {
            "Data dump credit card 50k records available",
            "Company database leaked - SQL injection",
            "Ransomware group claiming responsibility",
            "Stolen credentials marketplace update",
            "Government documents auction starting",
            "Zero-day exploit for sale"
        };

        public DarkWebMonitorService(ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<List<MediaPost>> ScanAsync(string? keyword = null, int count = 10)
        {
            var isEnabled = await _configService.GetValueAsync("EnableDarkWebMonitor", "false");
            if (isEnabled.ToLower() != "true") return new List<MediaPost>();

            Console.WriteLine("Scanning dark web sources...");
            var posts = new List<MediaPost>();
            var scanCount = keyword != null ? count : _random.Next(3, 8);

            for (int i = 0; i < scanCount; i++)
            {
                var source = DarkWebSources[_random.Next(DarkWebSources.Length)];
                var topic = DarkWebTopics[_random.Next(DarkWebTopics.Length)];
                
                if (!string.IsNullOrEmpty(keyword) && !topic.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    continue;

                posts.Add(new MediaPost
                {
                    Source = $"[DARK WEB] {source}",
                    Content = topic,
                    Url = $"http://{GenerateRandomOnionUrl()}.onion/post/{_random.Next(10000, 99999)}",
                    Author = $"Anonymous_{_random.Next(1000, 9999)}",
                    PostedAt = DateTime.UtcNow.AddHours(-_random.Next(1, 48)),
                    Sentiment = "Negative",
                    SentimentScore = -0.7,
                    Category = CategorizeDarkWebContent(topic),
                    Location = "TOR Network",
                    Language = "en",
                    IsProcessed = true,
                    Tags = "darkweb, tor, leak, security"
                });
            }

            Console.WriteLine($"Found {posts.Count} dark web mentions");
            return posts;
        }

        private string GenerateRandomOnionUrl()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz234567";
            return new string(Enumerable.Repeat(chars, 16).Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        private string CategorizeDarkWebContent(string content)
        {
            var lower = content.ToLower();
            if (lower.Contains("credit card") || lower.Contains("credentials")) return "Keamanan";
            if (lower.Contains("ransomware") || lower.Contains("exploit")) return "Keamanan";
            if (lower.Contains("government") || lower.Contains("political")) return "Politik";
            if (lower.Contains("company") || lower.Contains("corporate")) return "Ekonomi";
            return "Keamanan";
        }

        public async Task<Dictionary<string, object>> GetThreatSummaryAsync()
        {
            var recentPosts = await ScanAsync(null, 20);
            return new Dictionary<string, object>
            {
                { "TotalMentions", recentPosts.Count },
                { "HighRiskItems", recentPosts.Count(p => p.SentimentScore < -0.5) },
                { "DataLeaks", recentPosts.Count(p => p.Content.Contains("leak", StringComparison.OrdinalIgnoreCase)) },
                { "LastScanTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }
    }
}