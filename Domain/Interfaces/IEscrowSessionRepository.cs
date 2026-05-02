using Domain.Entities;
using System.Linq.Expressions;

namespace Domain.Interfaces

{
    public interface IEscrowSessionRepository
    {
        Task<EscrowSession> AddAsync(EscrowSession session);
        Task<EscrowSession?> GetByOrderIdAsync(int orderId);
        Task<EscrowSession?> GetActiveEscrowByEventIdAsync(int eventId);
        Task<EscrowSession?> GetByIdAsync(int id, params Expression<Func<EscrowSession, object>>[] includes);
        Task<List<EscrowSession>> GetAllAsync(params Expression<Func<EscrowSession, object>>[] includes);
        Task<List<EscrowSession>> GetEscrowsByUserIdAsync(int userId);
        void Update(EscrowSession session);
        IQueryable<EscrowSession> Query();
    }
}