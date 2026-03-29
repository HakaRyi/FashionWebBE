using Microsoft.AspNetCore.SignalR;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.OrderRepos;
using Repositories.Repos.WalletRepos;
using Services.Request.OrderReq;
using Services.Response.OrderResp;
using Services.Utils.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.OrderImp
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IAccountRepository _accountRepo;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IWalletRepository _walletRepo;

        public OrderService(IOrderRepository orderRepo
            , IHubContext<OrderHub> hubContext
            , IAccountRepository accountRepo
            , IEscrowSessionRepository escrowRepo
            , IWalletRepository walletRepo
            )
        {
            _orderRepo = orderRepo;
            _hubContext = hubContext;
            _accountRepo = accountRepo;
            _escrowRepo = escrowRepo;
            _walletRepo = walletRepo;
        }

        public async Task<OrderResponse> CreateOrderAsync(int sellerId, CreateOrderRequest request)
        {
            decimal serviceFee = 15000;
            var totalAmount = request.SubTotal + serviceFee;

            var order = new Order
            {
                BuyerId = request.BuyerId,
                SellerId = sellerId,
                SubTotal = request.SubTotal,
                ServiceFee = serviceFee,
                TotalAmount = totalAmount,
                Status = "PENDING",
                Note = request.Note,
                ShippingAddress = request.ShippingAddress,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                CreatedAt = DateTime.UtcNow,
                OrderDetails = request.Details.Select(d => new OrderDetail
                {
                    OutfitId = null,
                    ProductId = null,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    ImageUrl = d.ImageUrl,
                    ItemName = d.ItemName
                }).ToList()
            };

            var createdOrder = await _orderRepo.CreateAsync(order);

            var buyer = await _accountRepo.GetAccountById(createdOrder.BuyerId);
            var seller = await _accountRepo.GetAccountById(createdOrder.SellerId);

            var response = new OrderResponse
            {
                OrderId = createdOrder.OrderId,
                BuyerId = createdOrder.BuyerId,
                BuyerName = buyer?.UserName ?? "Unknown",
                SellerId = createdOrder.SellerId,
                SellerName = seller?.UserName ?? "You",
                TotalAmount = createdOrder.TotalAmount,
                Status = createdOrder.Status,
                CreatedAt = createdOrder.CreatedAt,
                OrderDetails = createdOrder.OrderDetails.Select(d => new OrderDetailResponse
                {
                    OrderDetailId = d.OrderDetailId,
                    OrderId = d.OrderId,
                    OutfitId = d.OutfitId,
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    ItemName = d.ItemName,
                    ImageUrl = d.ImageUrl
                }).ToList()
            };

            await _hubContext.Clients.Group($"User_{response.BuyerId}").SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderRepo.GetByIdAsync(orderId);
        }

        public async Task<List<OrderResponse>> GetSalesOrdersAsync(int sellerId)
        {
            var orders = await _orderRepo.GetOrdersBySellerIdAsync(sellerId);
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<List<OrderResponse>> GetPurchasesOrdersAsync(int buyerId)
        {
            var orders = await _orderRepo.GetOrdersByBuyerIdAsync(buyerId);
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, int currentUserId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null) throw new Exception("Order not found");
            if (order.BuyerId != currentUserId && order.SellerId != currentUserId) throw new UnauthorizedAccessException();

            order.Status = status;
            await _orderRepo.UpdateAsync(order);

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.Group($"User_{order.SellerId}").SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null) throw new Exception("Order not found");
            if (order.BuyerId != buyerId) throw new UnauthorizedAccessException();
            if (order.Status != "CONFIRMED") throw new Exception("Invalid order status");

            var buyer = await _accountRepo.GetAccountById(buyerId);
            if (buyer == null) throw new Exception("Buyer not found");

            if (buyer.Wallet.Balance < order.TotalAmount) throw new Exception("Insufficient balance");

            buyer.Wallet.Balance -= order.TotalAmount;
            await _walletRepo.UpdateWalletAsync(buyer.Wallet);

            order.Status = "PAID";
            await _orderRepo.UpdateAsync(order);

            var escrowSession = new EscrowSession
            {
                OrderId = order.OrderId,
                SenderId = buyerId,
                ReceiverId = order.SellerId,
                Amount = order.TotalAmount,
                ServiceFee = order.ServiceFee,
                Status = "HELD",
                Description = $"Thanh toán tạm giữ cho đơn hàng #{order.OrderId}",
                CreatedAt = DateTime.UtcNow
            };

            await _escrowRepo.AddAsync(escrowSession);

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.Group($"User_{order.SellerId}").SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<List<OrderResponse>> GetPaidOrdersAsync()
        {
            var orders = await _orderRepo.GetPaidOrdersAsync();
            return orders.Select(MapToResponse).ToList();
        }

        private OrderResponse MapToResponse(Order order)
        {
            return new OrderResponse
            {
                OrderId = order.OrderId,
                BuyerId = order.BuyerId,
                BuyerName = order.Buyer?.UserName ?? "Unknown",
                SellerId = order.SellerId,
                SellerName = order.Seller?.UserName ?? "You",
                SubTotal = order.SubTotal,
                ServiceFee = order.ServiceFee,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Note = order.Note,
                ShippingAddress = order.ShippingAddress,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                CreatedAt = order.CreatedAt,
                OrderDetails = order.OrderDetails.Select(d => new OrderDetailResponse
                {
                    OrderDetailId = d.OrderDetailId,
                    OrderId = d.OrderId,
                    OutfitId = d.OutfitId,
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    ItemName = d.ItemName,
                    ImageUrl = d.ImageUrl
                }).ToList()
            };
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, int currentUserId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null)
            {
                return null;
            }

            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
            {
                throw new UnauthorizedAccessException();
            }

            OrderResponse response = MapToResponse(order);
            return response;
        }

        public async Task<List<OrderResponse>> GetCompletedOrdersAsync()
        {
            var orders = await _orderRepo.GetCompletedOrdersAsync();
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<List<OrderResponse>> GetCancelledOrdersAsync()
        {
            var orders = await _orderRepo.GetCancelledOrdersAsync();
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<List<OrderResponse>> GetShippingOrdersAsync()
        {
            var orders = await _orderRepo.GetShippingOrdersAsync();
            return orders.Select(MapToResponse).ToList();
        }

        public async Task<OrderResponse> GetOrderDetailByIdAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");
            return MapToResponse(order);
        }

        public async Task<OrderResponse> UpdateOrderStatusByShipperAsync(int orderId, string status)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null) throw new Exception("Order not found");

            order.Status = status;
            await _orderRepo.UpdateAsync(order);

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.Group($"User_{order.SellerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", response);

            return response;
        }
    }
}
