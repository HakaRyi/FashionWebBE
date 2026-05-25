using Domain.Contracts.Social.Trend;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TrendingTopicRepository
        : ITrendingTopicRepository
    {
        private readonly FashionDbContext _db;

        public TrendingTopicRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task UpsertRangeAsync(
            List<TrendingTopic> topics)
        {
            var hashtagIds = topics
                .Select(x => x.HashtagId)
                .ToList();

            var existing = await _db.TrendingTopics
                .Where(x => hashtagIds.Contains(x.HashtagId))
                .ToListAsync();

            foreach (var topic in topics)
            {
                var old = existing
                    .FirstOrDefault(x =>
                        x.HashtagId == topic.HashtagId);

                if (old == null)
                {
                    _db.TrendingTopics.Add(topic);
                }
                else
                {
                    old.Score = topic.Score;
                    old.TotalPosts = topic.TotalPosts;
                    old.TotalEngagement = topic.TotalEngagement;
                    old.CalculatedAt = topic.CalculatedAt;
                }
            }
        }

        public async Task<List<TrendingTopicDto>>
            GetTrendingTopicsAsync(
                int limit,
                bool isAdmin)
        {
            return await _db.TrendingTopics
                .AsNoTracking()
                .Include(x => x.Hashtag)
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new TrendingTopicDto
                {
                    HashtagId = x.HashtagId,

                    Keyword = x.Hashtag.Name,

                    Score = x.Score,

                    TotalPosts = x.TotalPosts,

                    TotalEngagement = x.TotalEngagement,

                    CalculatedAt = x.CalculatedAt
                })
                .ToListAsync();
        }
    }
}