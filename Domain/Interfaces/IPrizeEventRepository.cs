using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IPrizeEventRepository
    {
        Task AddRangeAsync(IEnumerable<PrizeEvent> prizes);
        Task<IEnumerable<PrizeEvent>> GetByEventIdAsync(int eventId);
        Task<PrizeEvent?> GetByIdAsync(int prizeId);
        void Update(PrizeEvent prize);
    }
}
