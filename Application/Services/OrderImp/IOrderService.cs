using Application.Request.OrderReq;
using Application.Response.OrderResp;
using Application.Response.RefundResp;
using Domain.Entities;

namespace Application.Services.OrderImp
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(int sellerId, int buyerId, CreateOrderRequest request);
        Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId);

        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int currentUserId);
        Task<OrderResponse> GetOrderDetailByIdAsync(int orderId);

        Task<List<OrderResponse>> GetSalesOrdersAsync(int sellerId);
        Task<List<OrderResponse>> GetPurchasesOrdersAsync(int buyerId);
        Task<List<OrderResponse>> GetPaidOrdersAsync();
        Task<List<OrderResponse>> GetCompletedOrdersAsync();
        Task<List<OrderResponse>> GetCancelledOrdersAsync();
        Task<List<OrderResponse>> GetShippingOrdersAsync();

        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, int currentUserId);
        Task<OrderResponse> UpdateOrderStatusByShipperAsync(int orderId, string status);

        Task<OrderResponse> CreateRefundRequestAsync(
            int orderId,
            int buyerId,
            CreateRefundRequestDto request);

        Task<List<RefundRequestResponse>> GetAllRefundRequestsAsync();
        Task<List<RefundRequestResponse>> GetMyRefundRequestsAsync(int buyerId);
        Task<OrderResponse> RejectRefundAsync(int orderId, string adminNote);
        Task<OrderResponse> UpdateRefundStatus(int orderId);
        Task<List<OrderResponse>> GetDeliveredOrdersAsync();
        Task<OrderResponse> AutoCompleteDeliveredOrderAsync(int orderId);
    }
}