using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IExpertRatingRepository
    {
        Task AddAsync(ExpertRating rating);
        void Update(ExpertRating rating);
        Task<ExpertRating?> GetByPostAndExpertAsync(int postId, int expertId);
        Task<IEnumerable<ExpertRating>> GetRatingsByPostIdAsync(int postId);
        Task<IEnumerable<ExpertRating>> GetDetailedRatingsByPostIdAsync(int postId);
        Task<IEnumerable<ExpertRating>> GetAllRatingsByEventIdAsync(int eventId);
    }
}
