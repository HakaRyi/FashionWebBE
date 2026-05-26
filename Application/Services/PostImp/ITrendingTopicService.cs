using Domain.Contracts.Social.Trend;

namespace Application.Services.PostImp
{
    public interface ITrendingTopicService
    {
        Task RecomputeTrendingTopicsAsync();
        Task<TrendingTopicSummaryDto> GetUserTrendingTopicsAsync(int limit);
        Task<TrendingTopicSummaryDto> GetAdminTrendingTopicsAsync(int limit);
    }
}