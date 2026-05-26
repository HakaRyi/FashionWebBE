using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IHashtagRepository
    {
        Task<Hashtag?> GetByNameAsync(string name);

        Task<Hashtag?> GetByIdAsync(int hashtagId);

        Task<List<Hashtag>> GetByNamesAsync(List<string> names);

        Task<List<Hashtag>> GetTrendingSourceAsync(
            int days = 7);

        Task<List<Hashtag>> GetTopUsedAsync(int limit);

        Task AddAsync(Hashtag hashtag);

        Task AddRangeAsync(List<Hashtag> hashtags);

        void Update(Hashtag hashtag);
    }
}