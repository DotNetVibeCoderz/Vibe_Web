using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Kopdar.Data;
using Kopdar.Models;

namespace Kopdar.Services;

public class AppService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public event Action OnStateChanged;
    public event Action<Message> OnMessageReceived;

    public AppUser CurrentUser { get; private set; }

    public AppService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public async Task LoginAsync(string username, string password)
    {
        using var db = _dbFactory.CreateDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
        if (user != null)
        {
            CurrentUser = user;
            NotifyStateChanged();
        }
    }
    
    public void Logout()
    {
        CurrentUser = null;
        NotifyStateChanged();
    }

    public async Task<bool> RegisterAsync(string username, string email, string password)
    {
        using var db = _dbFactory.CreateDbContext();
        if (!await db.Users.AnyAsync(u => u.Username == username))
        {
            db.Users.Add(new AppUser 
            { 
                Username = username, 
                Email = email, 
                PasswordHash = password, 
                Latitude = -6.2, 
                Longitude = 106.8,
                Bio = "Kopdar new user! Ready to chat.",
                Gender = "Unspecified",
                ProfilePictureUrl = "/images/logo.svg"
            });
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task UpdateProfileAsync(int userId, string bio, string gender)
    {
        using var db = _dbFactory.CreateDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Bio = string.IsNullOrEmpty(bio) ? "No bio yet" : bio;
            user.Gender = string.IsNullOrEmpty(gender) ? "Unspecified" : gender;
            await db.SaveChangesAsync();
            
            if (CurrentUser?.Id == userId)
            {
                CurrentUser.Bio = user.Bio;
                CurrentUser.Gender = user.Gender;
            }
            NotifyStateChanged();
        }
    }

    public async Task UpdateProfilePictureAsync(int userId, string url)
    {
        using var db = _dbFactory.CreateDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user != null && !string.IsNullOrEmpty(url))
        {
            user.ProfilePictureUrl = url;
            await db.SaveChangesAsync();
            if (CurrentUser?.Id == userId)
            {
                CurrentUser.ProfilePictureUrl = url;
            }
            NotifyStateChanged();
        }
    }

    // Follows
    public async Task<List<AppUser>> GetFollowersAsync(int userId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Follows
            .Include(f => f.Follower)
            .Where(f => f.FollowingId == userId)
            .Select(f => f.Follower)
            .ToListAsync();
    }

    public async Task<List<AppUser>> GetFollowingAsync(int userId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Follows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == userId)
            .Select(f => f.Following)
            .ToListAsync();
    }

    public async Task ToggleFollowAsync(int targetUserId)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var existingFollow = await db.Follows.FirstOrDefaultAsync(f => f.FollowerId == CurrentUser.Id && f.FollowingId == targetUserId);
        
        if (existingFollow != null)
        {
            db.Follows.Remove(existingFollow);
        }
        else
        {
            db.Follows.Add(new Follow { FollowerId = CurrentUser.Id, FollowingId = targetUserId });
            
            // Add notification
            db.Notifications.Add(new Notification { UserId = targetUserId, Content = $"**{CurrentUser.Username}** started following you.", IsRead = false });
        }
        await db.SaveChangesAsync();
        NotifyStateChanged();
    }

    // Timeline
    public async Task<List<Post>> GetTimelineAsync(int skip = 0, int take = 100)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments).ThenInclude(c => c.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task CreatePostAsync(string content, string mediaUrl)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var post = new Post 
        { 
            UserId = CurrentUser.Id, 
            Content = content ?? "", 
            MediaUrl = string.IsNullOrEmpty(mediaUrl) ? "" : mediaUrl 
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task LikePostAsync(int postId)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var existingLike = await db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == CurrentUser.Id);
        if (existingLike == null)
        {
            db.Likes.Add(new Like { PostId = postId, UserId = CurrentUser.Id });
            
            var post = await db.Posts.FindAsync(postId);
            if (post != null && post.UserId != CurrentUser.Id)
            {
                // Add notification
                db.Notifications.Add(new Notification { UserId = post.UserId, Content = $"**{CurrentUser.Username}** liked your post.", IsRead = false });
            }

            await db.SaveChangesAsync();
            NotifyStateChanged();
        }
    }

    public async Task AddCommentAsync(int postId, string content)
    {
        if (CurrentUser == null || string.IsNullOrWhiteSpace(content)) return;
        using var db = _dbFactory.CreateDbContext();
        db.Comments.Add(new Comment { PostId = postId, UserId = CurrentUser.Id, Content = content });
        
        var post = await db.Posts.FindAsync(postId);
        if (post != null && post.UserId != CurrentUser.Id)
        {
            // Add notification
            db.Notifications.Add(new Notification { UserId = post.UserId, Content = $"**{CurrentUser.Username}** commented on your post.", IsRead = false });
        }

        await db.SaveChangesAsync();
        NotifyStateChanged();
    }

    // Chat
    public async Task<List<AppUser>> GetChatContactsAsync()
    {
        if (CurrentUser == null) return new List<AppUser>();
        using var db = _dbFactory.CreateDbContext();
        
        var senderIds = await db.Messages.Where(m => m.ReceiverId == CurrentUser.Id).Select(m => m.SenderId).ToListAsync();
        var receiverIds = await db.Messages.Where(m => m.SenderId == CurrentUser.Id && m.ReceiverId != null).Select(m => m.ReceiverId.Value).ToListAsync();
        
        var contactIds = senderIds.Union(receiverIds).Distinct().ToList();
        
        return await db.Users.Where(u => contactIds.Contains(u.Id)).ToListAsync();
    }

    public async Task<List<Message>> GetChatHistoryAsync(int otherUserId)
    {
        if (CurrentUser == null) return new List<Message>();
        using var db = _dbFactory.CreateDbContext();
        return await db.Messages
            .Include(m => m.Sender)
            .Where(m => (m.SenderId == CurrentUser.Id && m.ReceiverId == otherUserId) || 
                        (m.SenderId == otherUserId && m.ReceiverId == CurrentUser.Id))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task DeleteChatThreadAsync(int otherUserId)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var msgs = db.Messages.Where(m => 
            (m.SenderId == CurrentUser.Id && m.ReceiverId == otherUserId) || 
            (m.SenderId == otherUserId && m.ReceiverId == CurrentUser.Id));
        
        db.Messages.RemoveRange(msgs);
        await db.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task<List<Message>> GetGroupChatHistoryAsync(int groupId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Messages
            .Include(m => m.Sender)
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task SendMessageAsync(int? receiverId, int? groupId, string content, string mediaUrl = "")
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var msg = new Message
        {
            SenderId = CurrentUser.Id,
            ReceiverId = receiverId,
            GroupId = groupId,
            Content = content ?? "",
            MediaUrl = string.IsNullOrEmpty(mediaUrl) ? "" : mediaUrl
        };
        db.Messages.Add(msg);

        // Add Notification for Single Chat
        if (receiverId.HasValue && receiverId.Value != CurrentUser.Id)
        {
            db.Notifications.Add(new Notification { UserId = receiverId.Value, Content = $"**{CurrentUser.Username}** sent you a message.", IsRead = false });
        }

        // Add Notification for Group Chat
        if (groupId.HasValue)
        {
            var group = await db.ChatGroups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId.Value);
            if (group != null)
            {
                foreach (var member in group.Members.Where(m => m.UserId != CurrentUser.Id))
                {
                    db.Notifications.Add(new Notification { UserId = member.UserId, Content = $"**{CurrentUser.Username}** sent a message in **{group.Name}**.", IsRead = false });
                }
            }
        }

        await db.SaveChangesAsync();
        
        msg.Sender = await db.Users.FindAsync(CurrentUser.Id);
        OnMessageReceived?.Invoke(msg);
        NotifyStateChanged(); // Update unread counts
    }
    
    // Groups
    public async Task<List<ChatGroup>> GetMyGroupsAsync()
    {
        if (CurrentUser == null) return new List<ChatGroup>();
        using var db = _dbFactory.CreateDbContext();
        return await db.GroupMembers
            .Include(gm => gm.Group)
            .Where(gm => gm.UserId == CurrentUser.Id)
            .Select(gm => gm.Group)
            .ToListAsync();
    }

    public async Task<List<ChatGroup>> GetUserCreatedGroupsAsync(int userId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ChatGroups
            .Where(g => g.CreatorId == userId)
            .ToListAsync();
    }

    public async Task<ChatGroup> GetGroupProfileAsync(int groupId)
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ChatGroups
            .Include(g => g.Creator)
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task CreateGroupAsync(string name, string desc, List<int> memberIds)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        
        var group = new ChatGroup
        {
            Name = name,
            Description = desc,
            CreatorId = CurrentUser.Id,
            Latitude = CurrentUser.Latitude,
            Longitude = CurrentUser.Longitude,
            IsPublic = true
        };
        db.ChatGroups.Add(group);
        await db.SaveChangesAsync();

        var members = new List<GroupMember>();
        members.Add(new GroupMember { GroupId = group.Id, UserId = CurrentUser.Id }); 
        foreach(var id in memberIds)
        {
            if (id != CurrentUser.Id)
            {
                members.Add(new GroupMember { GroupId = group.Id, UserId = id });
                db.Notifications.Add(new Notification { UserId = id, Content = $"**{CurrentUser.Username}** added you to the group **{group.Name}**.", IsRead = false });
            }
        }
        db.GroupMembers.AddRange(members);
        await db.SaveChangesAsync();
        NotifyStateChanged();
    }

    public async Task JoinGroupAsync(int groupId)
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();

        var alreadyMember = await db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == CurrentUser.Id);
        if (!alreadyMember)
        {
            db.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = CurrentUser.Id });
            await db.SaveChangesAsync();
            NotifyStateChanged();
        }
    }

    // Nearby / Explore
    public async Task<List<AppUser>> GetAllActiveUsersAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.Users.Where(u => u.IsActive).ToListAsync();
    }

    public async Task<List<ChatGroup>> GetAllPublicGroupsAsync()
    {
        using var db = _dbFactory.CreateDbContext();
        return await db.ChatGroups.Where(g => g.IsPublic).ToListAsync();
    }

    public async Task<List<AppUser>> GetNearbyUsersAsync(double lat, double lon, double radiusKm)
    {
        using var db = _dbFactory.CreateDbContext();
        var allUsers = await db.Users.Where(u => u.IsActive && u.Id != (CurrentUser != null ? CurrentUser.Id : 0)).ToListAsync();
        return allUsers.Where(u => CalculateDistance(lat, lon, u.Latitude, u.Longitude) <= radiusKm).ToList();
    }
    
    public async Task<List<ChatGroup>> GetNearbyGroupsAsync(double lat, double lon, double radiusKm)
    {
        using var db = _dbFactory.CreateDbContext();
        var allGroups = await db.ChatGroups.Where(g => g.IsPublic).ToListAsync();
        return allGroups.Where(g => CalculateDistance(lat, lon, g.Latitude, g.Longitude) <= radiusKm).ToList();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371; 
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * 
                Math.Sin(dLon/2) * Math.Sin(dLon/2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
        return r * c;
    }

    public async Task<List<(AppUser User, double Score)>> SearchUsersSemanticAsync(string query)
    {
        using var db = _dbFactory.CreateDbContext();
        var allUsers = await db.Users.Where(u => u.IsActive).ToListAsync();
        
        if (string.IsNullOrWhiteSpace(query)) 
            return allUsers.Select(u => (u, 1.0)).ToList();

        var queryTokens = Tokenize(query);
        var results = new List<(AppUser User, double Score)>();

        foreach (var user in allUsers)
        {
            var bioTokens = Tokenize(user.Bio ?? "");
            var nameTokens = Tokenize(user.Username);
            bioTokens.AddRange(nameTokens);

            double score = CalculateCosineSimilarity(queryTokens, bioTokens);
            
            if (user.Username.Contains(query, StringComparison.OrdinalIgnoreCase))
                score += 0.5;

            if (score > 0) results.Add((user, score));
        }

        return results.OrderByDescending(r => r.Score).ToList();
    }

    private List<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split(new[] { ' ', '.', ',', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

    private double CalculateCosineSimilarity(List<string> vecA, List<string> vecB)
    {
        var allTokens = vecA.Union(vecB).Distinct().ToList();
        var vectorA = allTokens.Select(t => vecA.Count(x => x == t)).ToList();
        var vectorB = allTokens.Select(t => vecB.Count(x => x == t)).ToList();

        double dotProduct = 0;
        double magA = 0;
        double magB = 0;

        for (int i = 0; i < allTokens.Count; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magA += vectorA[i] * vectorA[i];
            magB += vectorB[i] * vectorB[i];
        }

        magA = Math.Sqrt(magA);
        magB = Math.Sqrt(magB);

        if (magA == 0 || magB == 0) return 0;
        return dotProduct / (magA * magB);
    }

    // Notifications
    public async Task<int> GetUnreadNotificationCountAsync()
    {
        if (CurrentUser == null) return 0;
        using var db = _dbFactory.CreateDbContext();
        return await db.Notifications.CountAsync(n => n.UserId == CurrentUser.Id && !n.IsRead);
    }

    public async Task<List<Notification>> GetUserNotificationsAsync()
    {
        if (CurrentUser == null) return new List<Notification>();
        using var db = _dbFactory.CreateDbContext();
        return await db.Notifications
            .Where(n => n.UserId == CurrentUser.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task MarkNotificationsAsReadAsync()
    {
        if (CurrentUser == null) return;
        using var db = _dbFactory.CreateDbContext();
        var unreadNotifs = await db.Notifications.Where(n => n.UserId == CurrentUser.Id && !n.IsRead).ToListAsync();
        foreach (var n in unreadNotifs)
        {
            n.IsRead = true;
        }
        await db.SaveChangesAsync();
        NotifyStateChanged();
    }
}