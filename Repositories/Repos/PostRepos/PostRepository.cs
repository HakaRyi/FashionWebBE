using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
using Repositories.Dto.Admin;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Post;
using Repositories.Entities;

namespace Repositories.Repos.PostRepos
{
    public class PostRepository : IPostRepository
    {
        private readonly FashionDbContext _db;

        public PostRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<List<Post>> GetAllPostAsync()
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<PostFeedDto>> GetFeedWithSocialAsync(int viewerId, DateTime? cursor, int pageSize)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Where(p =>
                    p.Status == PostStatus.Published &&
                    p.Visibility == PostVisibility.Visible);

            if (cursor.HasValue)
            {
                query = query.Where(p => p.CreatedAt < cursor.Value);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(pageSize)
                .Select(p => new PostFeedDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),
                    IsEvent = p.EventId.HasValue ? true : false,
                    EventName = p.EventId.HasValue ? p.Event!.Title : null,
                    Title = p.Title,
                    Content = p.Content,

                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    LikeCount = p.LikeCount ?? 0,
                    CommentCount = p.CommentCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                    Status = p.Status,
                    Visibility = p.Visibility,

                    IsLiked = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == viewerId),

                    IsSaved = _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == viewerId)
                })
                .ToListAsync();
        }

        public async Task<PostDetailDto?> GetPostDetailAsync(int postId, int viewerId)
        {
            return await _db.Posts
                .AsNoTracking()
                .Where(p =>
                    p.PostId == postId &&
                    (
                        p.AccountId == viewerId ||
                        (
                            p.Status == PostStatus.Published &&
                            p.Visibility == PostVisibility.Visible
                        )
                    ))
                .Select(p => new PostDetailDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    Title = p.Title,
                    Content = p.Content,

                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    LikeCount = p.LikeCount ?? 0,
                    CommentCount = p.CommentCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,

                    IsLiked = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == viewerId),

                    IsSaved = _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == viewerId),

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Comments = new List<Repositories.Dto.Social.Comment.CommentDto>()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResultDto<MyPostDto>> GetMyPostsPagedAsync(int ownerId, int page, int pageSize)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Where(p => p.AccountId == ownerId);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new MyPostDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),
                    IsEvent = p.EventId.HasValue ? true : false,
                    EventName = p.EventId.HasValue ? p.Event!.Title : null,
                    Title = p.Title,
                    Content = p.Content,

                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    LikeCount = p.LikeCount ?? 0,
                    CommentCount = p.CommentCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Status = p.Status,
                    Visibility = p.Visibility,

                    IsLiked = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == ownerId),

                    IsSaved = _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == ownerId),

                    IsOwner = true,

                    // user ko được sửa khi đang Verifying hoặc PendingAdmin
                    // Rejected / Published thì được sửa, sửa xong sẽ về Verifying lại
                    CanEdit = p.Status != PostStatus.Verifying
                           && p.Status != PostStatus.PendingAdmin,

                    CanDelete = true,

                    CanHide = p.Status == PostStatus.Published
                           && p.Visibility == PostVisibility.Visible,

                    CanUnhide = p.Status == PostStatus.Published
                             && p.Visibility == PostVisibility.Hidden,

                    IsPubliclyVisible = p.Status == PostStatus.Published
                                     && p.Visibility == PostVisibility.Visible
                })
                .ToListAsync();

            return new PagedResultDto<MyPostDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                HasMore = (page * pageSize) < total
            };
        }

        public async Task<PagedResultDto<PostFeedDto>> GetUserPublicPostsPagedAsync(int ownerId, int? viewerId, int page, int pageSize)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Where(p =>
                    p.AccountId == ownerId &&
                    p.Status == PostStatus.Published &&
                    p.Visibility == PostVisibility.Visible);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PostFeedDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    Title = p.Title,
                    Content = p.Content,

                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    LikeCount = p.LikeCount ?? 0,
                    CommentCount = p.CommentCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Status = p.Status,
                    Visibility = p.Visibility,

                    IsLiked = viewerId.HasValue && _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == viewerId.Value),

                    IsSaved = viewerId.HasValue && _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == viewerId.Value)
                })
                .ToListAsync();

            return new PagedResultDto<PostFeedDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                HasMore = (page * pageSize) < total
            };
        }

        public async Task<List<PostFeedDto>> GetTrendingPostsAsync(int limit, int viewerId)
        {
            return await _db.Posts
                .AsNoTracking()
                .Where(p =>
                    p.Status == PostStatus.Published &&
                    p.Visibility == PostVisibility.Visible)
                .OrderByDescending(p =>
                    (p.LikeCount ?? 0) * 2 +
                    (p.CommentCount ?? 0) * 3 +
                    (p.ShareCount ?? 0) * 5)
                .Take(limit)
                .Select(p => new PostFeedDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,

                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    Title = p.Title,
                    Content = p.Content,

                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                    LikeCount = p.LikeCount ?? 0,
                    CommentCount = p.CommentCount ?? 0,
                    ShareCount = p.ShareCount ?? 0,

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Status = p.Status,
                    Visibility = p.Visibility,

                    IsLiked = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == viewerId),

                    IsSaved = _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == viewerId)
                })
                .ToListAsync();
        }

        public async Task<PagedResultDto<AdminReviewPostDto>> GetPendingAdminPostsPagedAsync(int page, int pageSize)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.PendingAdmin);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminReviewPostDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,
                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),
                    Title = p.Title,
                    Content = p.Content,
                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    Status = p.Status,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return new PagedResultDto<AdminReviewPostDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                HasMore = (page * pageSize) < total
            };
        }

        public async Task<PagedResultDto<AdminReviewPostDto>> GetRejectedPostsPagedAsync(int page, int pageSize)
        {
            var query = _db.Posts
                .AsNoTracking()
                .Where(p => p.Status == PostStatus.Rejected);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminReviewPostDto
                {
                    PostId = p.PostId,
                    AccountId = p.AccountId,
                    UserName = p.Account.UserName!,
                    AvatarUrl = p.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),
                    Title = p.Title,
                    Content = p.Content,
                    Images = p.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    Status = p.Status,
                    Visibility = p.Visibility,
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return new PagedResultDto<AdminReviewPostDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                HasMore = (page * pageSize) < total
            };
        }

        public async Task AddAsync(Post post)
        {
            await _db.Posts.AddAsync(post);
        }

        public void Update(Post post)
        {
            _db.Posts.Update(post);
        }

        public void Delete(Post post)
        {
            _db.Posts.Remove(post);
        }

        public async Task<List<Post>> GetAllPendingAdminPostAsync()
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.PendingAdmin)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllPublishedAsync()
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.Status == PostStatus.Published)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Post>> GetAllByUserAsync(int userId)
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .Where(p => p.AccountId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId)
        {
            return await _db.Posts
                .Include(p => p.Account)
                .Include(p => p.Images)
                .Include(p => p.ExpertRatings)
                .Where(p => p.EventId == eventId && p.Status == "Published")
                .ToListAsync();
        }

        public async Task<double> GetMaxRawCommunityScoreAsync(int eventId, double pointPerLike, double pointPerShare)
        {
            var maxRawScore = await _db.Posts
                .Where(p => p.EventId == eventId && p.Status != "Deleted")
                .MaxAsync(p => (double?)((p.LikeCount ?? 0) * pointPerLike + (p.ShareCount ?? 0) * pointPerShare)) ?? 0;

            return maxRawScore;
        }

        public async Task<List<Post>> GetGradedPostsByEventIdAsync(int eventId)
        {
            return await _db.Posts
                .Include(p => p.Scoreboard)
                .Where(p => p.EventId == eventId && p.Scoreboard != null)
                .ToListAsync();
        }


    }
}