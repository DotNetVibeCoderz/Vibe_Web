using Microsoft.EntityFrameworkCore;
using LitePost.Data.Models;

namespace LitePost.Data.Services
{
    public class PostService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public PostService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<Post>> GetFeed()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var posts = await context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .Include(p => p.QuotePost)
                    .ThenInclude(qp => qp!.User)
                .Include(p => p.Poll)
                    .ThenInclude(p => p!.Options)
                        .ThenInclude(o => o.Votes)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();

            // Filter out future scheduled posts
            return posts.Where(p => p.ScheduledFor == null || p.ScheduledFor <= DateTime.UtcNow).ToList();
        }
        
        public async Task<List<Post>> GetPostsByUser(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .Include(p => p.QuotePost)
                    .ThenInclude(qp => qp!.User)
                .Include(p => p.Poll)
                    .ThenInclude(p => p!.Options)
                        .ThenInclude(o => o.Votes)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetBookmarks(int userId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var bookmarkIds = await context.Bookmarks
                .Where(b => b.UserId == userId)
                .Select(b => b.PostId)
                .ToListAsync();

            return await context.Posts
                .Where(p => bookmarkIds.Contains(p.Id))
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .Include(p => p.QuotePost)
                    .ThenInclude(qp => qp!.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> SearchPosts(string query)
        {
            using var context = _dbContextFactory.CreateDbContext();
            if (string.IsNullOrWhiteSpace(query)) return new List<Post>();

            return await context.Posts
                .Where(p => p.Content.Contains(query))
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<Post>> GetVerifiedFeed()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Posts
                .Where(p => p.User.IsVerified)
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Replies)
                .OrderByDescending(p => p.CreatedAt)
                .Take(50)
                .ToListAsync();
        }
        
        public async Task CreatePost(int userId, string content, PostType type = PostType.Tweet, 
            string? mediaUrl = null, string? location = null, DateTime? scheduledFor = null, 
            List<string>? pollOptions = null, DateTime? pollEndsAt = null)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var post = new Post
            {
                UserId = userId,
                Content = content,
                Type = type,
                MediaUrl = mediaUrl,
                Location = location,
                ScheduledFor = scheduledFor,
                CreatedAt = DateTime.UtcNow
            };

            context.Posts.Add(post);
            await context.SaveChangesAsync();

            // Handle Poll Creation
            if (pollOptions != null && pollOptions.Count >= 2 && pollEndsAt.HasValue)
            {
                var poll = new Poll
                {
                    PostId = post.Id,
                    Question = content, // Use post content as question
                    EndsAt = pollEndsAt.Value
                };
                context.Polls.Add(poll);
                await context.SaveChangesAsync();

                foreach (var optionText in pollOptions)
                {
                    if (!string.IsNullOrWhiteSpace(optionText))
                    {
                        context.PollOptions.Add(new PollOption
                        {
                            PollId = poll.Id,
                            Text = optionText
                        });
                    }
                }
                await context.SaveChangesAsync();
                
                // Link poll to post
                post.PollId = poll.Id;
                await context.SaveChangesAsync();
            }
        }

        public async Task Repost(int userId, int originalPostId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Check if already reposted
            var existingRepost = await context.Posts.FirstOrDefaultAsync(p => p.UserId == userId && p.QuotePostId == originalPostId && p.Type == PostType.Repost);
            
            if (existingRepost != null)
            {
                // Undo repost
                context.Posts.Remove(existingRepost);
            }
            else
            {
                var post = new Post
                {
                    UserId = userId,
                    Content = string.Empty, // Repost has no content usually
                    Type = PostType.Repost,
                    QuotePostId = originalPostId,
                    CreatedAt = DateTime.UtcNow
                };
                context.Posts.Add(post);
            }
            await context.SaveChangesAsync();
        }

        public async Task LikePost(int userId, int postId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var like = await context.Likes.FindAsync(userId, postId);
            if (like == null)
            {
                context.Likes.Add(new Like { UserId = userId, PostId = postId });
            }
            else
            {
                context.Likes.Remove(like);
            }
            await context.SaveChangesAsync();
        }

        public async Task ToggleBookmark(int userId, int postId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var bookmark = await context.Bookmarks.FindAsync(userId, postId);
            if (bookmark == null)
            {
                context.Bookmarks.Add(new Bookmark { UserId = userId, PostId = postId });
            }
            else
            {
                context.Bookmarks.Remove(bookmark);
            }
            await context.SaveChangesAsync();
        }

        public async Task<bool> IsBookmarked(int userId, int postId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Bookmarks.AnyAsync(b => b.UserId == userId && b.PostId == postId);
        }
        
        public async Task<bool> IsReposted(int userId, int postId)
        {
             using var context = _dbContextFactory.CreateDbContext();
             return await context.Posts.AnyAsync(p => p.UserId == userId && p.QuotePostId == postId && p.Type == PostType.Repost);
        }

        public async Task VotePoll(int userId, int pollOptionId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var option = await context.PollOptions.Include(o => o.Poll).FirstOrDefaultAsync(o => o.Id == pollOptionId);
            if (option == null) return;

            // Check if user already voted in this poll
            var hasVoted = await context.PollVotes
                .Include(v => v.PollOption)
                .AnyAsync(v => v.UserId == userId && v.PollOption.PollId == option.PollId);

            if (!hasVoted)
            {
                context.PollVotes.Add(new PollVote { UserId = userId, PollOptionId = pollOptionId });
                await context.SaveChangesAsync();
            }
        }
    }
}