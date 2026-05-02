using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Repositories
{
    public class EscrowSessionRepository : IEscrowSessionRepository
    {
        private readonly FashionDbContext _context;

        public EscrowSessionRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<EscrowSession> AddAsync(EscrowSession session)
        {
            await _context.EscrowSessions.AddAsync(session);
            return session;
        }

        public async Task<EscrowSession?> GetByIdAsync(int id, params Expression<Func<EscrowSession, object>>[] includes)
        {
            IQueryable<EscrowSession> query = _context.EscrowSessions;
            foreach (var include in includes) query = query.Include(include);

            return await query.FirstOrDefaultAsync(x => x.EscrowSessionId == id);
        }

        public async Task<List<EscrowSession>> GetAllAsync(params Expression<Func<EscrowSession, object>>[] includes)
        {
            IQueryable<EscrowSession> query = _context.EscrowSessions;
            foreach (var include in includes) query = query.Include(include);

            return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
        }

        public async Task<List<EscrowSession>> GetEscrowsByUserIdAsync(int userId)
        {
            return await _context.EscrowSessions
                .Include(e => e.Sender)
                .Include(e => e.Event)
                .Where(e => e.SenderId == userId || e.ReceiverId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<EscrowSession?> GetActiveEscrowByEventIdAsync(int eventId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.Status == "Held");
        }


        public async Task<EscrowSession?> GetByOrderIdAsync(int orderId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task<EscrowSession?> GetByEventIdAsync(int eventId)
        {
            return await _context.EscrowSessions
                .FirstOrDefaultAsync(x => x.EventId == eventId);
        }

        public void Update(EscrowSession session)
        {
            _context.EscrowSessions.Update(session);
        }

        public IQueryable<EscrowSession> Query()
        {
            return _context.EscrowSessions.AsQueryable();
        }

    }
}