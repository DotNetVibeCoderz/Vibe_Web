using MediaMonitoring.Models;

namespace MediaMonitoring.Services.Integrations
{
    public interface ISocialMediaService
    {
        string PlatformName { get; }
        Task<List<MediaPost>> FetchPostsAsync(string keyword, int count = 50);
        Task<bool> TestConnectionAsync();
    }

    public class TwitterApiService : ISocialMediaService
    {
        private readonly ConfigService _configService;
        public string PlatformName => "Twitter/X";

        public TwitterApiService(ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<List<MediaPost>> FetchPostsAsync(string keyword, int count = 50)
        {
            var apiKey = await _configService.GetValueAsync("TwitterApiKey");
            if (string.IsNullOrEmpty(apiKey)) return new List<MediaPost>();
            
            Console.WriteLine($"Fetching from Twitter API: {keyword}");
            return new List<MediaPost>();
        }

        public async Task<bool> TestConnectionAsync()
        {
            var apiKey = await _configService.GetValueAsync("TwitterApiKey");
            return !string.IsNullOrEmpty(apiKey);
        }
    }

    public class FacebookApiService : ISocialMediaService
    {
        private readonly ConfigService _configService;
        public string PlatformName => "Facebook";

        public FacebookApiService(ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<List<MediaPost>> FetchPostsAsync(string keyword, int count = 50)
        {
            var appId = await _configService.GetValueAsync("FacebookAppId");
            if (string.IsNullOrEmpty(appId)) return new List<MediaPost>();
            
            Console.WriteLine($"Fetching from Facebook API: {keyword}");
            return new List<MediaPost>();
        }

        public async Task<bool> TestConnectionAsync()
        {
            var appId = await _configService.GetValueAsync("FacebookAppId");
            return !string.IsNullOrEmpty(appId);
        }
    }

    public class InstagramApiService : ISocialMediaService
    {
        private readonly ConfigService _configService;
        public string PlatformName => "Instagram";

        public InstagramApiService(ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<List<MediaPost>> FetchPostsAsync(string keyword, int count = 50)
        {
            Console.WriteLine($"Fetching from Instagram API: {keyword}");
            return new List<MediaPost>();
        }

        public async Task<bool> TestConnectionAsync() => true;
    }

    public class YouTubeApiService : ISocialMediaService
    {
        private readonly ConfigService _configService;
        public string PlatformName => "YouTube";

        public YouTubeApiService(ConfigService configService)
        {
            _configService = configService;
        }

        public async Task<List<MediaPost>> FetchPostsAsync(string keyword, int count = 50)
        {
            var apiKey = await _configService.GetValueAsync("YouTubeApiKey", "");
            if (string.IsNullOrEmpty(apiKey)) return new List<MediaPost>();
            
            Console.WriteLine($"Fetching from YouTube API: {keyword}");
            return new List<MediaPost>();
        }

        public async Task<bool> TestConnectionAsync()
        {
            var apiKey = await _configService.GetValueAsync("YouTubeApiKey", "");
            return !string.IsNullOrEmpty(apiKey);
        }
    }
}