using Application.Interfaces;
using Domain.Contracts.Social.Trend;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.PostImp
{
    public class PostTrendService : IPostTrendService
    {
        private readonly IPostTrendRepository _trendRepo;
        private readonly IPostRepository _postRepo;
        private readonly IUnitOfWork _uow;

        public PostTrendService(
            IPostTrendRepository trendRepo,
            IPostRepository postRepo,
            IUnitOfWork uow)
        {
            _trendRepo = trendRepo;
            _postRepo = postRepo;
            _uow = uow;
        }

        public async Task<TrendingSummaryDto> GetUserTrendingAsync(int limit)
        {
            var items = await _trendRepo.GetTrendingAsync(limit, isAdmin: false);

            return new TrendingSummaryDto
            {
                Items = items,
                GeneratedAt = DateTime.UtcNow,
                Scope = "user"
            };
        }

        public async Task<TrendingSummaryDto> GetAdminTrendingAsync(int limit)
        {
            var items = await _trendRepo.GetTrendingAsync(limit, isAdmin: true);

            return new TrendingSummaryDto
            {
                Items = items,
                GeneratedAt = DateTime.UtcNow,
                Scope = "admin"
            };
        }

        public async Task RecomputeTrendingAsync()
        {
            var posts = await _postRepo.GetRecentPostsAsync(days: 7);

            var trends = new List<PostTrend>();

            foreach (var post in posts)
            {
                var (score, engagement, decay) = Calculate(post);

                trends.Add(new PostTrend
                {
                    PostId = post.PostId,
                    Score = score,
                    EngagementScore = engagement,
                    TimeDecay = decay,
                    CalculatedAt = DateTime.UtcNow
                });
            }

            await _trendRepo.UpsertRangeAsync(trends);
            await _uow.SaveChangesAsync();
        }

        public (double score, double engagement, double decay) Calculate(Post post)
        {
            var engagement =
                (post.LikeCount ?? 0) * 1 +
                (post.CommentCount ?? 0) * 2 +
                (post.ShareCount ?? 0) * 3;

            var hours = (DateTime.UtcNow - post.CreatedAt!.Value).TotalHours;
            var decay = Math.Max(1, Math.Log(hours + 2));

            var score = engagement / decay;

            return (score, engagement, decay);
        } 
    }
}