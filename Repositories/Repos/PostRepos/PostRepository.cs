using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
using Repositories.Dto.Response;
using Repositories.Entities;

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
                .AsNoTracking()
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<Post>> GetAllPublishedAsync()
        {
            return await _context.Posts
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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

                    // EVENT
                    EventId = p.EventId,
                    EventName = p.Event.Title,

                    // POST
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

                    // SOCIAL
                    LikeCount = _context.Reactions
                        .Count(r => r.PostId == p.PostId),

                    CommentCount = _context.Comments
                        .Count(c => c.PostId == p.PostId),

                    IsLiked = _context.Reactions
                        .Any(r => r.PostId == p.PostId && r.AccountId == userId)
                })
                .ToListAsync();
        }

        public async Task<List<PostResponse>> GetPostsByUserAsync(int userId, int pageSize)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(p => p.AccountId == userId && p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Take(pageSize)
                .Select(p => new PostResponse
                {
                    PostId = p.PostId,

                    UserName = p.Account.UserName,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

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

                    LikeCount = _context.Reactions.Count(r => r.PostId == p.PostId),
                    CommentCount = _context.Comments.Count(c => c.PostId == p.PostId),

                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<List<PostResponse>> GetTrendingPostsAsync(int limit)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Published)
                .Select(p => new
                {
                    Post = p,
                    LikeCount = _context.Reactions.Count(r => r.PostId == p.PostId),
                    CommentCount = _context.Comments.Count(c => c.PostId == p.PostId)
                })
                .OrderByDescending(x =>
                    (x.LikeCount * 2) +
                    (x.CommentCount * 3) +
                    (x.Post.ShareCount * 5))
                .Take(limit)
                .Select(x => new PostResponse
                {
                    PostId = x.Post.PostId,

                    UserName = x.Post.Account.UserName,

                    AvatarUrl = x.Post.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    EventId = x.Post.EventId,
                    EventName = x.Post.Event.Title,

                    Title = x.Post.Tittle,
                    Content = x.Post.Content,

                    ImageUrls = x.Post.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    IsExpertPost = x.Post.IsExpertPost,
                    Status = x.Post.Status,

                    Score = x.Post.Score,
                    ShareCount = x.Post.ShareCount,

                    LikeCount = x.LikeCount,
                    CommentCount = x.CommentCount,

                    CreatedAt = x.Post.CreatedAt,
                    UpdatedAt = x.Post.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
        }

        public void Update(Post post)
        {
            _context.Posts.Update(post);
        }

        public void Delete(Post post)
        {
            _context.Posts.Remove(post);
        }
    }
}