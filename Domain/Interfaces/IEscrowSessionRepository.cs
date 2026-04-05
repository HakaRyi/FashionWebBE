using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IEscrowSessionRepository
    {
        Task<EscrowSession> AddAsync(EscrowSession session);
        Task<EscrowSession?> GetByIdAsync(int sessionId);
        Task<EscrowSession?> GetByOrderIdAsync(int orderId);
        Task<EscrowSession?> GetByEventIdAsync(int eventId);
        Task<EscrowSession?> GetActiveEscrowByEventIdAsync(int eventId);
        void Update(EscrowSession session);
        IQueryable<EscrowSession> Query();
    }
}