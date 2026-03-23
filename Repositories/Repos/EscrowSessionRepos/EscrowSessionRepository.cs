using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.EscrowSessionRepos
{
    public class EscrowSessionRepository : IEscrowSessionRepository
    {
        private readonly FashionDbContext _context;
        public EscrowSessionRepository(FashionDbContext context) => _context = context;

        public async Task AddAsync(EscrowSession session) => await _context.EscrowSessions.AddAsync(session);

        public async Task<EscrowSession?> GetByIdAsync(int sessionId) => await _context.EscrowSessions.FindAsync(sessionId);

        public async Task<EscrowSession?> GetByEventIdAsync(int eventId) =>
            await _context.EscrowSessions.FirstOrDefaultAsync(s => s.EventId == eventId);

        public void Update(EscrowSession session) => _context.EscrowSessions.Update(session);
    }
}
