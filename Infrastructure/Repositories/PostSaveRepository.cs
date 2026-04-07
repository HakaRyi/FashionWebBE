using Microsoft.EntityFrameworkCore;
using Domain.Constants;
using Domain.Dto.Common;
using Domain.Dto.Social.SavedPost;
using Domain.Entities;
using Infrastructure.Persistence;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class PostSaveRepository : IPostSaveRepository
    {
        private readonly FashionDbContext _db;

        public PostSaveRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsAsync(int postId, int accountId)
        {
            return await _db.PostSaves
                .AsNoTracking()
                .AnyAsync(x => x.PostId == postId && x.AccountId == accountId);
        }

        public async Task AddAsync(PostSave postSave)
        {
            await _db.PostSaves.AddAsync(postSave);
        }

        public async Task<PostSave?> GetByPostAndUserAsync(int postId, int accountId)
        {
            return await _db.PostSaves
                .FirstOrDefaultAsync(x => x.PostId == postId && x.AccountId == accountId);
        }

        public void Delete(PostSave postSave)
        {
            _db.PostSaves.Remove(postSave);
        }

        public async Task<PagedResultDto<SavedPostDto>> GetSavedPostsAsync(int accountId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _db.PostSaves
                .AsNoTracking()
                .Where(x => x.AccountId == accountId
                         && x.Post.Status == PostStatus.Published
                         && x.Post.Visibility == PostVisibility.Visible);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SavedPostDto
                {
                    PostId = x.PostId,
                    AccountId = x.Post.AccountId,
                    UserName = x.Post.Account.UserName!,
                    AvatarUrl = x.Post.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),
                    Title = x.Post.Title,
                    Content = x.Post.Content,
                    Images = x.Post.Images
                        .OrderBy(i => i.CreatedAt)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    LikeCount = x.Post.LikeCount ?? 0,
                    CommentCount = x.Post.CommentCount ?? 0,
                    ShareCount = x.Post.ShareCount ?? 0,
                    IsLiked = _db.Reactions.Any(r => r.PostId == x.PostId && r.AccountId == accountId),
                    IsSaved = true,
                    CreatedAt = x.Post.CreatedAt ?? DateTime.UtcNow,
                    SavedAt = x.CreatedAt
                })
                .ToListAsync();

            return new PagedResultDto<SavedPostDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                HasMore = page * pageSize < totalCount
            };
        }
    }
}