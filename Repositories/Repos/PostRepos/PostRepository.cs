using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
using Repositories.Dto.Response;
using Repositories.Entities;
using System.Threading.Tasks;

namespace Repositories.Repos.PostRepos
{
    public class PostRepository : IPostRepository
    {
        private readonly FashionDbContext _context;

        public PostRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetAllPublishedAsync()
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllByUserAsync(int userId)
        {
            return await _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.AccountId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetFeedByCursorAsync(DateTime? cursor, int pageSize)
        {
            var query = _context.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.Published);

            if (cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursor.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<PostResponse>> GetFeedWithSocialAsync(
            int userId,
            DateTime? cursor,
            int pageSize)
        {
            var query = _context.Posts
                .Where(p => p.Status == PostStatus.Published);

            if (cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursor.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(pageSize)
                .Select(p => new PostResponse
                {
                    PostId = p.PostId,

                    // USER
                    UserName = p.Account.UserName,
                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    // POST
                    EventId = p.EventId,
                    EventName = p.Event.Title,
                    Title = p.Tittle,
                    Content = p.Content,
                    ImageUrls = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    IsExpertPost = p.IsExpertPost,
                    Status = p.Status,
                    Score = p.Score,
                    ShareCount = p.ShareCount,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,

                    // 🔥 SOCIAL
                    LikeCount = _context.Reactions
                        .Count(r => r.PostId == p.PostId),

                    CommentCount = _context.Comments
                        .Count(c => c.PostId == p.PostId),

                    IsLiked = _context.Reactions
                        .Any(r => r.PostId == p.PostId && r.AccountId == userId)
                })
                .ToListAsync();
        }

        public async Task AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
        }

        public async Task  Update(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
        }

        public void Delete(Post post)
        {
            _context.Posts.Remove(post);
        }
    }
}