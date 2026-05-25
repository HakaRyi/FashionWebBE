using Domain.Contracts.Social.Trend;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ITrendingTopicRepository
    {
        Task UpsertRangeAsync(List<TrendingTopic> topics);

        Task<List<TrendingTopicDto>> GetTrendingTopicsAsync(
            int limit,
            bool isAdmin);
    }
}