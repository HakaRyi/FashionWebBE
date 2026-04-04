using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order);
        Order Update(Order order);

        IQueryable<Order> Query();

        Task<Order?> GetByIdAsync(int orderId);
        Task<Order?> GetByIdNoTrackingAsync(int orderId);

        Task<List<Order>> GetOrdersBySellerIdAsync(int sellerId);
        Task<List<Order>> GetOrdersByBuyerIdAsync(int buyerId);

        Task<List<Order>> GetByStatusAsync(string status);
        Task<List<Order>> GetByStatusesAsync(params string[] statuses);

        Task<List<Order>> GetPaidOrdersAsync();
        Task<List<Order>> GetCompletedOrdersAsync();
        Task<List<Order>> GetCancelledOrdersAsync();
        Task<List<Order>> GetShippingOrdersAsync();
    }
}