using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            return await BuildRefundQuery(isTracking: true)
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<RefundRequest?> GetByIdAsync(int id)
        {
            return await BuildRefundQuery(isTracking: true)
                .FirstOrDefaultAsync(r => r.RefundRequestId == id);
        }

        public async Task<List<RefundRequest>> GetAllAsync()
        {
            return await BuildRefundQuery(isTracking: false)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        private IQueryable<RefundRequest> BuildRefundQuery(bool isTracking)
        {
            IQueryable<RefundRequest> query = _context.RefundRequests;

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Item)
                            .ThenInclude(i => i.Images)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Buyer)
                .Include(r => r.Order)
                    .ThenInclude(o => o.Seller);
        }
    }
}