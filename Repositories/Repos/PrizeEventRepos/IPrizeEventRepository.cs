using Repositories.Entities;

namespace Repositories.Repos.PrizeEventRepos
{
    public interface IPrizeEventRepository
    {
        Task AddRangeAsync(IEnumerable<PrizeEvent> prizes);
        Task<IEnumerable<PrizeEvent>> GetByEventIdAsync(int eventId);
        Task<PrizeEvent?> GetByIdAsync(int prizeId);
        void Update(PrizeEvent prize);
    }
}
