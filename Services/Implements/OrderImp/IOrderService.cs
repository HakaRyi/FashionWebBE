using Repositories.Entities;
using Services.Request.OrderReq;
using Services.Response.OrderResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.OrderImp
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(int sellerId, CreateOrderRequest request);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<OrderResponse>> GetSalesOrdersAsync(int sellerId);
        Task<List<OrderResponse>> GetPurchasesOrdersAsync(int buyerId);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int currentUserId);
        Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId);
        Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, int currentUserId);
    }
}
