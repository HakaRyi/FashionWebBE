using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class EscrowStatusHistoryRepository : IEscrowStatusHistoryRepository
    {
        private readonly FashionDbContext _context;

        public EscrowStatusHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<EscrowStatusHistory?> GetByIdAsync(int id)
        {
            return await _context.Set<EscrowStatusHistory>().FindAsync(id);
        }

        public async Task<IEnumerable<EscrowStatusHistory>> GetByEscrowSessionIdAsync(int escrowSessionId)
        {
            return await _context.Set<EscrowStatusHistory>()
                .Where(e => e.EscrowSessionId == escrowSessionId)
                .OrderByDescending(e => e.ChangedAt)
                .Include(e => e.ChangedBy)
                .ToListAsync();
        }

        public async Task AddAsync(EscrowStatusHistory escrowStatusHistory)
        {
            await _context.Set<EscrowStatusHistory>().AddAsync(escrowStatusHistory);
        }

        public void Update(EscrowStatusHistory escrowStatusHistory)
        {
            _context.Set<EscrowStatusHistory>().Update(escrowStatusHistory);
        }

        public void Delete(EscrowStatusHistory escrowStatusHistory)
        {
            _context.Set<EscrowStatusHistory>().Remove(escrowStatusHistory);
        }
    }
}
