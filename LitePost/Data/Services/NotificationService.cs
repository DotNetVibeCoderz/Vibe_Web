using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;

namespace LitePost.Data.Services
{
    public class NotificationService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public NotificationService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<Notification>> GetUserNotifications(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Notifications
                .Include(n => n.Actor)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task CreateNotification(int userId, int actorId, string message, NotificationType type, int? relatedPostId = null)
        {
            if (userId == actorId) return; // Don't notify self actions

            using var context = _dbContextFactory.CreateDbContext();
            var notification = new Notification
            {
                UserId = userId,
                ActorId = actorId,
                Message = message,
                Type = type,
                RelatedPostId = relatedPostId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
        }

        public async Task MarkAsRead(int notificationId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var notification = await context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await context.SaveChangesAsync();
            }
        }
    }
}