using Domain.Entities;
using System.Linq.Expressions;

namespace Domain.Interfaces

{
    public interface IEscrowSessionRepository
    {
        Task<EscrowSession> AddAsync(EscrowSession session);
        Task<EscrowSession?> GetByOrderIdAsync(int orderId);
        Task<List<EscrowSession>> GetTotalEscrowsByEventIdAsync(int eventId);
        Task<EscrowSession?> GetByIdAsync(int id, params Expression<Func<EscrowSession, object>>[] includes);
        Task<List<EscrowSession>> GetAllAsync(params Expression<Func<EscrowSession, object>>[] includes);
        Task<List<EscrowSession>> GetEscrowsByUserIdAsync(int userId);
        Task<List<EscrowSession>> GetEscrowsByBatchIdsAsync(List<int> eventIds, List<int> orderIds);
        void Update(EscrowSession session);
        IQueryable<EscrowSession> Query();
    }
}