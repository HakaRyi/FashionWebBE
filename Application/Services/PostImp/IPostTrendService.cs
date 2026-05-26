using Domain.Contracts.Social.Trend;

namespace Application.Services.PostImp
{
    public interface IPostTrendService
    {
        Task<TrendingSummaryDto> GetUserTrendingAsync(int limit);
        Task<TrendingSummaryDto> GetAdminTrendingAsync(int limit);
        Task RecomputeTrendingAsync();
    }
}