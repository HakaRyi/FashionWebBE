using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.OrderRepos
{
    public class OrderRepository : IOrderRepository
    {
        private readonly FashionDbContext _context;

        public OrderRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            return order;
        }

        public Order Update(Order order)
        {
            _context.Orders.Update(order);
            return order;
        }

        public IQueryable<Order> Query()
        {
            return _context.Orders.AsQueryable();
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await BuildOrderDetailQuery(isTracking: true)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> GetByIdNoTrackingAsync(int orderId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByBuyerIdAsync(int buyerId)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusAsync(string status)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusesAsync(params string[] statuses)
        {
            return await BuildOrderDetailQuery(isTracking: false)
                .Where(o => statuses.Contains(o.Status))
                .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetPaidOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Processing);
        }

        public async Task<List<Order>> GetCompletedOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Completed);
        }

        public async Task<List<Order>> GetCancelledOrdersAsync()
        {
            return await GetByStatusesAsync(OrderStatus.Cancelled, OrderStatus.Refunded);
        }

        public async Task<List<Order>> GetShippingOrdersAsync()
        {
            return await GetByStatusAsync(OrderStatus.Shipping);
        }

        private IQueryable<Order> BuildOrderDetailQuery(bool isTracking)
        {
            IQueryable<Order> query = _context.Orders;

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.OrderDetails)
                .Include(o => o.EscrowSession);
        }
    }
}