using Microsoft.EntityFrameworkCore;
using SimpleBlog.Data;
using SimpleBlog.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleBlog.Services
{
    public class BlogService
    {
        private readonly ApplicationDbContext _context;

        public BlogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BlogPost>> GetPublishedPostsAsync()
        {
            return await _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.BlogPostTags).ThenInclude(bt => bt.Tag)
                .Where(b => b.IsPublished)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<BlogPost?> GetPostBySlugAsync(string slug)
        {
            return await _context.BlogPosts
                .Include(b => b.Category)
                .Include(b => b.BlogPostTags).ThenInclude(bt => bt.Tag)
                .Include(b => b.Comments)
                .FirstOrDefaultAsync(b => b.Slug == slug);
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task AddCommentAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task LikePostAsync(Guid postId)
        {
            var post = await _context.BlogPosts.FindAsync(postId);
            if (post != null)
            {
                post.Likes++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<BlogPost>> SearchPostsAsync(string query, bool useSemantic = false)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<BlogPost>();

            var lowerQuery = query.ToLower().Trim();

            if (!useSemantic)
            {
                // Standard Case-Insensitive Search
                return await _context.BlogPosts
                    .Include(b => b.Category)
                    .Where(b => b.IsPublished && 
                               (b.Title.ToLower().Contains(lowerQuery) || 
                                b.Content.ToLower().Contains(lowerQuery) ||
                                b.Summary.ToLower().Contains(lowerQuery)))
                    .ToListAsync();
            }
            else
            {
                // Semantic-like Search (Keyword Relevance Ranking)
                // We split the query into individual keywords
                var keywords = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                var allPosts = await _context.BlogPosts
                    .Include(b => b.Category)
                    .Where(b => b.IsPublished)
                    .ToListAsync();

                // Calculate relevance score based on keyword frequency in Title and Content
                var scoredPosts = allPosts.Select(p => new {
                    Post = p,
                    Score = keywords.Sum(k => 
                        (p.Title.ToLower().Contains(k) ? 10 : 0) + // Title match is more important
                        (p.Summary.ToLower().Contains(k) ? 5 : 0) +
                        (p.Content.ToLower().Contains(k) ? 1 : 0)
                    )
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Post)
                .ToList();

                return scoredPosts;
            }
        }
    }
}
