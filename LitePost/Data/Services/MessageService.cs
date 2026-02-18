using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;

namespace LitePost.Data.Services
{
    public class MessageService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public MessageService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<Message>> GetMessages(int userId1, int userId2)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Messages
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderBy(m => m.CreatedAt)
                .Include(m => m.Sender)
                .ToListAsync();
        }

        public async Task SendMessage(int senderId, int receiverId, string content)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            context.Messages.Add(message);
            await context.SaveChangesAsync();
        }

        public async Task<List<User>> GetConversations(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Get user IDs involved in conversations
            var sentTo = await context.Messages
                .Where(m => m.SenderId == userId)
                .Select(m => m.ReceiverId)
                .Distinct()
                .ToListAsync();

            var receivedFrom = await context.Messages
                .Where(m => m.ReceiverId == userId)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var allUserIds = sentTo.Union(receivedFrom).Distinct().ToList();

            return await context.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToListAsync();
        }
    }
}