using Domain.Constants;
using Domain.Contracts.Social.Post;
using Domain.Dto.Admin;
//using Domain.Dto.Common;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Domain.Contracts.Social.Post;
using Domain.Contracts.Common;

namespace Infrastructure.Repositories
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
                .Include(p => p.Scoreboard)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<Post?> GetByIdFullRangeAsync(int postId)
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Scoreboard)
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .ThenInclude(e => e.EventExperts)
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

                    IsEvent = p.EventId.HasValue,
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
                        s.AccountId == viewerId),

                    IsExpertPost = p.IsExpertPost ?? false,

                    IsLikedByExpert = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.Account.ExpertProfile != null &&
                        r.Account.ExpertProfile.Verified == true)
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

                    IsExpertPost = p.IsExpertPost ?? false,

                    IsLikedByExpert = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.Account.ExpertProfile != null &&
                        r.Account.ExpertProfile.Verified == true),

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Comments = new List<Domain.Dto.Social.Comment.CommentDto>()
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

                    IsEvent = p.EventId.HasValue,
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

                    IsLiked = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == ownerId),

                    IsSaved = _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == ownerId),

                    IsExpertPost = p.IsExpertPost ?? false,

                    IsLikedByExpert = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.Account.ExpertProfile != null &&
                        r.Account.ExpertProfile.Verified == true),

                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow,

                    Status = p.Status,
                    Visibility = p.Visibility,

                    IsOwner = true,

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
                HasMore = page * pageSize < total
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

                    IsEvent = p.EventId.HasValue,
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

                    IsLiked = viewerId.HasValue && _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.AccountId == viewerId.Value),

                    IsSaved = viewerId.HasValue && _db.PostSaves.Any(s =>
                        s.PostId == p.PostId &&
                        s.AccountId == viewerId.Value),

                    IsExpertPost = p.IsExpertPost ?? false,

                    IsLikedByExpert = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.Account.ExpertProfile != null &&
                        r.Account.ExpertProfile.Verified == true)
                })
                .ToListAsync();

            return new PagedResultDto<PostFeedDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                HasMore = page * pageSize < total
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

                    IsEvent = p.EventId.HasValue,
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
                        s.AccountId == viewerId),

                    IsExpertPost = p.IsExpertPost ?? false,

                    IsLikedByExpert = _db.Reactions.Any(r =>
                        r.PostId == p.PostId &&
                        r.Account.ExpertProfile != null &&
                        r.Account.ExpertProfile.Verified == true)
                })
                .ToListAsync();
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
                .Include(p => p.Event)
                .Include(p => p.Scoreboard)
                .Where(p => p.EventId == eventId && p.Status == "Published")
                .ToListAsync();
        }

        public async Task<IEnumerable<Post>> GetPostsForReview(int eventId, int? accountId)
        {
            return await _db.Posts
                .Include(p => p.Account)
                .Include(p => p.Images)
                .Include(p => p.Event)
                .Include(p => p.ExpertRatings)
                    .ThenInclude(r => r.CriterionRatings)
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

        public async Task<Post?> GetPostForShareAsync(int postId)
        {
            return await _db.Posts
                .Include(p => p.Images)
                .Include(p => p.Account)
                    .ThenInclude(a => a.Avatars)
                .Include(p => p.Event)
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<int> CountAccountPostsAsync(int accountId)
        {
            return await _db.Posts.CountAsync(p => p.AccountId == accountId && p.Status == PostStatus.Published);
        }

        public async Task<(List<Post> Posts, List<Account> Users)> SearchRawDataAsync(string keyword, int limit)
        {
            var searchLower = keyword.ToLower();

            var users = await _db.Users
                .Include(u => u.ExpertProfile)
                .Include(u => u.Avatars)
                .Where(u => u.UserName!.ToLower().Contains(searchLower) ||
                            (u.ExpertProfile != null && u.ExpertProfile.ExpertiseField!.ToLower().Contains(searchLower)))
                .Take(limit)
                .ToListAsync();

            var posts = await _db.Posts
                .Include(p => p.Account).ThenInclude(a => a.Avatars)
                .Include(p => p.Images)
                .Where(p => (p.Title!.ToLower().Contains(searchLower) || p.Content!.ToLower().Contains(searchLower)) &&
                            p.Status == PostStatus.Published && p.Visibility == PostVisibility.Visible)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return (posts, users);
        }

        public async Task<List<int>> GetLikedPostIdsAsync(int viewerId, List<int> postIds)
        {
            return await _db.Reactions
                .Where(r => r.AccountId == viewerId && postIds.Contains(r.PostId))
                .Select(r => r.PostId)
                .ToListAsync();
        }

        public async Task<bool> AnyAsync(Expression<Func<Post, bool>> predicate)
        {
            return await _db.Posts.AnyAsync(predicate);
        }
    }
}