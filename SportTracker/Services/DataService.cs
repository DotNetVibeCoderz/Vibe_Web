using SportTracker.Data;
using SportTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace SportTracker.Services
{
    public class DataService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public DataService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<Activity>> GetFeedActivitiesAsync(int userId, int limit = 20)
        {
            using var db = await _factory.CreateDbContextAsync();
            var followingIds = await db.Follows
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FollowedId)
                .ToListAsync();

            followingIds.Add(userId); // include own activities

            return await db.Activities
                .Include(a => a.User)
                .Include(a => a.Likes)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .Where(a => followingIds.Contains(a.UserId))
                .OrderByDescending(a => a.Date)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Activity>> GetUserActivitiesAsync(int userId)
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Activities
                .Include(a => a.Likes)
                .Include(a => a.Comments)
                    .ThenInclude(c => c.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
        }

        public async Task AddActivityAsync(Activity activity)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
        }

        public async Task<List<Club>> GetClubsAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Clubs.Include(c => c.Members).ToListAsync();
        }
        
        public async Task<bool> IsUserInClubAsync(int userId, int clubId)
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.UserClubs.AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);
        }

        public async Task JoinClubAsync(int userId, int clubId)
        {
            using var db = await _factory.CreateDbContextAsync();
            if (!await db.UserClubs.AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId))
            {
                db.UserClubs.Add(new UserClub { UserId = userId, ClubId = clubId });
                await db.SaveChangesAsync();
            }
        }
        
        public async Task LeaveClubAsync(int userId, int clubId)
        {
            using var db = await _factory.CreateDbContextAsync();
            var userClub = await db.UserClubs.FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ClubId == clubId);
            if (userClub != null)
            {
                db.UserClubs.Remove(userClub);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetLeaderboardAsync(int limit = 10)
        {
            using var db = await _factory.CreateDbContextAsync();
            var users = await db.Users.Include(u => u.Activities).ToListAsync();
            return users
                .OrderByDescending(u => u.Activities.Sum(a => a.DistanceKm))
                .Take(limit)
                .ToList();
        }

        public async Task LikeActivityAsync(int activityId, int userId)
        {
            using var db = await _factory.CreateDbContextAsync();
            if (!await db.Likes.AnyAsync(l => l.ActivityId == activityId && l.UserId == userId))
            {
                db.Likes.Add(new Like { ActivityId = activityId, UserId = userId });
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveLikeAsync(int activityId, int userId)
        {
            using var db = await _factory.CreateDbContextAsync();
            var like = await db.Likes.FirstOrDefaultAsync(l => l.ActivityId == activityId && l.UserId == userId);
            if (like != null)
            {
                db.Likes.Remove(like);
                await db.SaveChangesAsync();
            }
        }

        public async Task AddCommentAsync(int activityId, int userId, string content)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.Comments.Add(new Comment { ActivityId = activityId, UserId = userId, Content = content });
            await db.SaveChangesAsync();
        }

        // --- Master Data Methods ---
        public async Task<List<Activity>> GetAllActivitiesAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Activities.Include(a => a.User).ToListAsync();
        }

        public async Task UpdateActivityAsync(Activity activity)
        {
            using var db = await _factory.CreateDbContextAsync();
            db.Activities.Update(activity);
            await db.SaveChangesAsync();
        }

        public async Task DeleteActivityAsync(int id)
        {
            using var db = await _factory.CreateDbContextAsync();
            var act = await db.Activities.FindAsync(id);
            if (act != null)
            {
                db.Activities.Remove(act);
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var db = await _factory.CreateDbContextAsync();
            return await db.Users.ToListAsync();
        }
    }
}