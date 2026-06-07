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
    public class OrderStatusHistoryRepository : IOrderStatusHistoryRepository
    {
        private readonly FashionDbContext _context;

        public OrderStatusHistoryRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(OrderStatusHistory history)
        {
            await _context.Set<OrderStatusHistory>().AddAsync(history);
        }

        public async Task<IEnumerable<OrderStatusHistory>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Set<OrderStatusHistory>()
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.ChangedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<OrderStatusHistory?> GetLatestByOrderIdAsync(int orderId)
        {
            return await _context.Set<OrderStatusHistory>()
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.ChangedAt)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }
    }
}
