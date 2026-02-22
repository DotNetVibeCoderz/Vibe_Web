using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTube.Data;
using NetTube.Models;

namespace NetTube.Services
{
    public class VideoService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public VideoService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Video>> GetAllVideosAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Videos.Include(v => v.Uploader).ToListAsync();
        }

        public async Task<Video?> GetVideoByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Videos
                .Include(v => v.Uploader)
                .Include(v => v.Likes)
                .Include(v => v.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task AddVideoAsync(Video video)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Videos.Add(video);
            await context.SaveChangesAsync();
        }

        public async Task DeleteVideoAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var video = await context.Videos.FindAsync(id);
            if (video != null)
            {
                context.Videos.Remove(video);
                await context.SaveChangesAsync();
            }
        }

        public async Task RecordHistoryAsync(int userId, int videoId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var history = new WatchHistory { UserId = userId, VideoId = videoId, WatchedAt = DateTime.UtcNow };
            context.WatchHistories.Add(history);
            await context.SaveChangesAsync();
        }

        public async Task<List<WatchHistory>> GetUserHistoryAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.WatchHistories
                .Include(h => h.Video)
                    .ThenInclude(v => v.Uploader)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.WatchedAt)
                .ToListAsync();
        }

        public async Task ClearHistoryAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var histories = context.WatchHistories.Where(h => h.UserId == userId);
            context.WatchHistories.RemoveRange(histories);
            await context.SaveChangesAsync();
        }

        public async Task RemoveHistoryItemAsync(int historyId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var history = await context.WatchHistories.FindAsync(historyId);
            if (history != null)
            {
                context.WatchHistories.Remove(history);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Playlist>> GetUserPlaylistsAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Playlists
                .Include(p => p.PlaylistVideos)
                    .ThenInclude(pv => pv.Video)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<Playlist> CreatePlaylistAsync(int userId, string title)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var playlist = new Playlist { UserId = userId, Title = title, CreatedAt = DateTime.UtcNow };
            context.Playlists.Add(playlist);
            await context.SaveChangesAsync();
            return playlist;
        }

        public async Task DeletePlaylistAsync(int playlistId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var playlist = await context.Playlists.FindAsync(playlistId);
            if (playlist != null)
            {
                context.Playlists.Remove(playlist);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Video>> GetSubscribedVideosAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var subscribedIds = await context.Subscriptions
                .Where(s => s.SubscriberId == userId)
                .Select(s => s.CreatorId)
                .ToListAsync();

            return await context.Videos
                .Include(v => v.Uploader)
                .Where(v => subscribedIds.Contains(v.UserId))
                .OrderByDescending(v => v.UploadedAt)
                .ToListAsync();
        }

        public async Task AddCommentAsync(Comment comment)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.Comments.Add(comment);
            await context.SaveChangesAsync();
        }
        
        public async Task<CreatorDashboardStats> GetCreatorStatsAsync(int userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            
            var stats = new CreatorDashboardStats();
            
            var userVideos = await context.Videos.Where(v => v.UserId == userId).ToListAsync();
            
            stats.TotalVideos = userVideos.Count;
            stats.TotalViews = userVideos.Sum(v => v.Views);
            
            stats.TotalWatchTimeHours = (stats.TotalViews * 3) / 60;
            
            stats.TotalSubscribers = await context.Subscriptions.CountAsync(s => s.CreatorId == userId);
            
            stats.RecentSubscribers = await context.Subscriptions
                .Include(s => s.Subscriber)
                .Where(s => s.CreatorId == userId)
                .OrderByDescending(s => s.Id)
                .Take(5)
                .Select(s => s.Subscriber!)
                .ToListAsync();
                
            return stats;
        }

        public async Task<bool> ToggleLikeAsync(int userId, int videoId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existingLike = await context.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.VideoId == videoId);
            
            if (existingLike != null)
            {
                context.Likes.Remove(existingLike);
                await context.SaveChangesAsync();
                return false;
            }
            else
            {
                context.Likes.Add(new Like { UserId = userId, VideoId = videoId });
                await context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> ToggleSubscribeAsync(int subscriberId, int creatorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existingSub = await context.Subscriptions.FirstOrDefaultAsync(s => s.SubscriberId == subscriberId && s.CreatorId == creatorId);
            
            if (existingSub != null)
            {
                context.Subscriptions.Remove(existingSub);
                await context.SaveChangesAsync();
                return false; 
            }
            else
            {
                context.Subscriptions.Add(new Subscription { SubscriberId = subscriberId, CreatorId = creatorId });
                await context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> IsSubscribedAsync(int subscriberId, int creatorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subscriptions.AnyAsync(s => s.SubscriberId == subscriberId && s.CreatorId == creatorId);
        }
        
        public async Task<int> GetSubscriberCountAsync(int creatorId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subscriptions.CountAsync(s => s.CreatorId == creatorId);
        }
    }

    public class CreatorDashboardStats
    {
        public int TotalVideos { get; set; }
        public int TotalViews { get; set; }
        public int TotalWatchTimeHours { get; set; }
        public int TotalSubscribers { get; set; }
        public List<User> RecentSubscribers { get; set; } = new();
    }
}