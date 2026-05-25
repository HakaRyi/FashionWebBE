using Domain.Contracts.Social.Trend;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IPostTrendRepository
    {
        Task UpsertRangeAsync(List<PostTrend> trends);

        Task<List<PostTrendDto>> GetTrendingAsync(int limit, bool isAdmin);
    }
}