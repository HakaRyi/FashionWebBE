using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId)
        {
            return await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.OrderDetails)
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByBuyerIdAsync(int buyerId)
        {
            return await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.OrderDetails)
                .Where(o => o.BuyerId == buyerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public Task<Order> UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            return _context.SaveChangesAsync().ContinueWith(t => order);
        }
    }
}
