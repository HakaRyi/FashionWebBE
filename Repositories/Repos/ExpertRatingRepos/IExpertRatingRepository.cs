using Repositories.Entities;

namespace Repositories.Repos.ExpertRatingRepos
{
    public interface IExpertRatingRepository
    {
        Task AddAsync(ExpertRating rating);
        void Update(ExpertRating rating);
        Task<ExpertRating?> GetByPostAndExpertAsync(int postId, int expertId);
        Task<IEnumerable<ExpertRating>> GetRatingsByPostIdAsync(int postId);
    }
}
