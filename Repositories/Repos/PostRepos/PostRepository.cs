using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
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

        public async Task<Post?> GetByIdAsync(int postId)
        {
            return await _db.Posts
                .FirstOrDefaultAsync(p => p.PostId == postId);
        }

        public async Task<List<PostFeedDto>> GetFeedWithSocialAsync(
            int viewerId,
            DateTime? cursor,
            int pageSize)
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

                    Title = p.Tittle,
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

        public async Task<PostDetailDto?> GetPostDetailAsync(
            int postId,
            int viewerId)
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

                    Title = p.Tittle,
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

                    Comments = new List<Dto.Social.Comment.CommentDto>()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResultDto<MyPostDto>> GetMyPostsPagedAsync(
            int ownerId,
            int page,
            int pageSize)
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

                    Title = p.Tittle,
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
                    CanEdit = p.Status != PostStatus.Verifying
                           && p.Status != PostStatus.Rejected,
                    CanDelete = true,
                    CanHide = p.Visibility == PostVisibility.Visible
                           && p.Status != PostStatus.Rejected,
                    CanUnhide = p.Visibility == PostVisibility.Hidden
                             && p.Status != PostStatus.Rejected,
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

        public async Task<PagedResultDto<PostFeedDto>> GetUserPublicPostsPagedAsync(
            int ownerId,
            int? viewerId,
            int page,
            int pageSize)
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

                    Title = p.Tittle,
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

        public async Task<List<PostFeedDto>> GetTrendingPostsAsync(
            int limit,
            int viewerId)
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

                    Title = p.Tittle,
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
    }
}