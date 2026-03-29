using Repositories.Entities;

namespace Repositories.Repos.EventWinnerRepos
{
    public interface IEventWinnerRepository
    {
        Task AddAsync(EventWinner winner);
        Task AddRangeAsync(IEnumerable<EventWinner> winners);
        Task<IEnumerable<EventWinner>> GetByEventIdAsync(int eventId);
        Task<EventWinner?> GetByIdAsync(int eventWinnerId);
    }
}
