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
    public class RefundRequestRepository : IRefundRequestRepository
    {
        private readonly FashionDbContext _context;

        public RefundRequestRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<RefundRequest> AddAsync(RefundRequest request)
        {
            await _context.RefundRequests.AddAsync(request);
            return request;
        }

        public RefundRequest Update(RefundRequest request)
        {
            _context.RefundRequests.Update(request);
            return request;
        }

        public async Task<RefundRequest?> GetByOrderIdAsync(int orderId)
        {
            return await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<RefundRequest?> GetByIdAsync(int id)
        {
            return await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.RefundRequestId == id);
        }
        public async Task<List<RefundRequest>> GetAllAsync()
        {
            return await _context.RefundRequests
                .Include(r => r.Order)
                .ThenInclude(o => o.OrderDetails)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
