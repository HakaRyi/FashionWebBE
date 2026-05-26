using Application.Interfaces;
using Domain.Constants;
using Domain.Contracts.Social.Trend;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.PostImp
{
    public class TrendingTopicService : ITrendingTopicService
    {
        private readonly ITrendingTopicRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly IHashtagRepository _hashtagRepo;

        public TrendingTopicService(
            ITrendingTopicRepository repo,
            IUnitOfWork uow,
            IHashtagRepository hashtagRepo)
        {
            _repo = repo;
            _uow = uow;
            _hashtagRepo = hashtagRepo;
        }

        public async Task RecomputeTrendingTopicsAsync()
        {
            var hashtags = await _hashtagRepo
                .GetTrendingSourceAsync(days: 7);

            var topics = new List<TrendingTopic>();

            foreach (var hashtag in hashtags)
            {
                var validPosts = hashtag.PostHashtags
                    .Where(x =>
                        x.Post.Status == PostStatus.Published &&
                        x.Post.Visibility == PostVisibility.Visible &&
                        x.Post.CreatedAt.HasValue)
                    .ToList();

                if (!validPosts.Any())
                {
                    continue;
                }

                var totalPosts = validPosts.Count;

                var totalEngagement = validPosts.Sum(x =>
                    (x.Post.LikeCount ?? 0) +
                    (x.Post.CommentCount ?? 0) * 2 +
                    (x.Post.ShareCount ?? 0) * 3);

                var latestPostAt = validPosts
                    .Max(x => x.Post.CreatedAt!.Value);

                var hours =
                    (DateTime.UtcNow - latestPostAt)
                    .TotalHours;

                var decay =
                    Math.Max(1, Math.Log(hours + 2));

                var score =
                    totalEngagement / decay;

                topics.Add(new TrendingTopic
                {
                    HashtagId = hashtag.HashtagId,
                    Score = Math.Round(score, 2),
                    TotalPosts = totalPosts,
                    TotalEngagement = totalEngagement,
                    CalculatedAt = DateTime.UtcNow
                });
            }

            await _repo.UpsertRangeAsync(topics);

            await _uow.SaveChangesAsync();
        }

        public async Task<TrendingTopicSummaryDto>
            GetUserTrendingTopicsAsync(int limit)
        {
            var items =
                await _repo.GetTrendingTopicsAsync(
                    limit,
                    false);

            return new TrendingTopicSummaryDto
            {
                Items = items,
                GeneratedAt = DateTime.UtcNow,
                Scope = "user"
            };
        }

        public async Task<TrendingTopicSummaryDto>
            GetAdminTrendingTopicsAsync(int limit)
        {
            var items =
                await _repo.GetTrendingTopicsAsync(
                    limit,
                    true);

            return new TrendingTopicSummaryDto
            {
                Items = items,
                GeneratedAt = DateTime.UtcNow,
                Scope = "admin"
            };
        }
    }
}