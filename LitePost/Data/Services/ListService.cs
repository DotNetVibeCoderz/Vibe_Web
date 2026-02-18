using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;

namespace LitePost.Data.Services
{
    public class ListService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public ListService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<UserList>> GetUserLists(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.UserLists
                .Include(l => l.Members)
                .Where(l => l.OwnerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserList?> GetList(int listId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.UserLists
                .Include(l => l.Owner)
                .Include(l => l.Members)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(l => l.Id == listId);
        }

        public async Task<UserList> CreateList(int userId, string name, string? description, bool isPrivate)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var list = new UserList
            {
                OwnerId = userId,
                Name = name,
                Description = description,
                IsPrivate = isPrivate,
                CreatedAt = DateTime.UtcNow
            };
            context.UserLists.Add(list);
            await context.SaveChangesAsync();
            return list;
        }

        public async Task DeleteList(int listId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var list = await context.UserLists.FindAsync(listId);
            if (list != null)
            {
                context.UserLists.Remove(list);
                await context.SaveChangesAsync();
            }
        }

        public async Task AddMember(int listId, int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            if (!await context.UserListMembers.AnyAsync(m => m.ListId == listId && m.UserId == userId))
            {
                context.UserListMembers.Add(new UserListMember
                {
                    ListId = listId,
                    UserId = userId,
                    AddedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveMember(int listId, int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var member = await context.UserListMembers.FindAsync(listId, userId);
            if (member != null)
            {
                context.UserListMembers.Remove(member);
                await context.SaveChangesAsync();
            }
        }
        
        public async Task<List<Post>> GetListFeed(int listId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var memberIds = await context.UserListMembers
                .Where(m => m.ListId == listId)
                .Select(m => m.UserId)
                .ToListAsync();

            return await context.Posts
                .Where(p => memberIds.Contains(p.UserId))
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
        }
    }
}