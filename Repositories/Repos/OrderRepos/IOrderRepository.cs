using Repositories.Entities;

namespace Repositories.Repos.OrderRepos
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Task<Order?> GetByIdAsync(int orderId);
        Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId);
        Task<List<Order>> GetOrdersByBuyerIdAsync(int buyerId);
        Task<List<Order>> GetPaidOrdersAsync();
        Task<List<Order>> GetCompletedOrdersAsync();
        Task<List<Order>> GetCancelledOrdersAsync();
        Task<List<Order>> GetShippingOrdersAsync();
        Order Update(Order order);
        IQueryable<Order> Query();
    }
}