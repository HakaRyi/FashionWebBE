using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IEventWinnerRepository
    {
        Task AddAsync(EventWinner winner);
        Task AddRangeAsync(IEnumerable<EventWinner> winners);
        Task<IEnumerable<EventWinner>> GetByEventIdAsync(int eventId);
        Task<EventWinner?> GetByIdAsync(int eventWinnerId);
    }
}
