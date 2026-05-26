using Domain.Contracts.Social.Trend;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PostTrendRepository : IPostTrendRepository
    {
        private readonly FashionDbContext _db;

        public PostTrendRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task UpsertRangeAsync(List<PostTrend> trends)
        {
            var postIds = trends.Select(x => x.PostId).ToList();

            var existing = await _db.PostTrends
                .Where(x => postIds.Contains(x.PostId))
                .ToListAsync();

            foreach (var trend in trends)
            {
                var old = existing.FirstOrDefault(x => x.PostId == trend.PostId);

                if (old == null)
                {
                    _db.PostTrends.Add(trend);
                }
                else
                {
                    old.Score = trend.Score;
                    old.EngagementScore = trend.EngagementScore;
                    old.TimeDecay = trend.TimeDecay;
                    old.CalculatedAt = trend.CalculatedAt;
                }
            }
        }

        public async Task<List<PostTrendDto>> GetTrendingAsync(int limit, bool isAdmin)
        {
            var query = _db.PostTrends
                .AsNoTracking()
                .Include(t => t.Post)
                    .ThenInclude(p => p.Account)
                        .ThenInclude(a => a.Avatars)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(t =>
                    t.Post.Visibility == "Visible" &&
                    t.Post.Status == "Published");
            }

            return await query
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new PostTrendDto
                {
                    PostId = x.PostId,
                    Title = x.Post.Title,
                    Content = x.Post.Content,
                    UserName = x.Post.Account.UserName!,
                    AvatarUrl = x.Post.Account.Avatars
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.ImageUrl)
                        .FirstOrDefault(),

                    Score = x.Score,
                    EngagementScore = x.EngagementScore,
                    TimeDecay = x.TimeDecay,

                    CreatedAt = x.Post.CreatedAt ?? DateTime.UtcNow,
                    LikeCount = x.Post.LikeCount ?? 0,
                    CommentCount = x.Post.CommentCount ?? 0,
                    ShareCount = x.Post.ShareCount ?? 0
                })
                .ToListAsync();
        }
    }
}