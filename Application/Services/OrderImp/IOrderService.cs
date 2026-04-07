using Application.Request.OrderReq;
using Application.Response.OrderResp;
using Application.Response.RefundResp;
using Domain.Entities;

namespace Application.Services.OrderImp
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(int sellerId, CreateOrderRequest request);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<OrderResponse> GetOrderDetailByIdAsync(int orderId);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int currentUserId);
        Task<List<OrderResponse>> GetSalesOrdersAsync(int sellerId);
        Task<List<OrderResponse>> GetPurchasesOrdersAsync(int buyerId);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, int currentUserId);
        Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId);
        Task<OrderResponse> UpdateOrderStatusByShipperAsync(int orderId, string status);
        Task<List<OrderResponse>> GetPaidOrdersAsync();
        Task<List<OrderResponse>> GetCompletedOrdersAsync();
        Task<List<OrderResponse>> GetCancelledOrdersAsync();
        Task<List<OrderResponse>> GetShippingOrdersAsync();
        Task<OrderResponse> CreateRefundRequestAsync(int orderId, int buyerId, string reason, string proof1, string proof2);
        Task<OrderResponse> ProcessRefundAsync(int orderId);

        Task<List<RefundRequestResponse>> GetAllRefundRequestsAsync();
        Task<OrderResponse> RejectRefundAsync(int orderId, string adminNote);
        Task<List<RefundRequestResponse>> GetMyRefundRequestsAsync(int buyerId);
    }
}