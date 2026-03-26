using Repositories.Entities;

namespace Repositories.Repos.EscrowSessionRepos
{
    public interface IEscrowSessionRepository
    {
        Task<EscrowSession> AddAsync(EscrowSession session);
        Task<EscrowSession?> GetByIdAsync(int sessionId);
        Task<EscrowSession?> GetByOrderIdAsync(int orderId);
        Task<EscrowSession?> GetByEventIdAsync(int eventId);
        void Update(EscrowSession session);
        IQueryable<EscrowSession> Query();
    }
}