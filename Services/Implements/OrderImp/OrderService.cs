using Microsoft.AspNetCore.SignalR;
using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.OrderRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Request.OrderReq;
using Services.Response.OrderResp;
using Services.Utils.SignalR;

namespace Services.Implements.OrderImp
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(
            IOrderRepository orderRepo,
            IHubContext<OrderHub> hubContext,
            IEscrowSessionRepository escrowRepo,
            IWalletRepository walletRepo,
            ITransactionRepository transactionRepo,
            IUnitOfWork unitOfWork)
        {
            _orderRepo = orderRepo;
            _hubContext = hubContext;
            _escrowRepo = escrowRepo;
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<OrderResponse> CreateOrderAsync(int sellerId, CreateOrderRequest request)
        {
            if (request == null)
                throw new Exception("Dữ liệu đơn hàng không hợp lệ.");

            if (request.BuyerId <= 0)
                throw new Exception("Buyer không hợp lệ.");

            if (request.Details == null || !request.Details.Any())
                throw new Exception("Đơn hàng phải có ít nhất 1 sản phẩm.");

            if (request.Details.Any(d => d.ProductId == null))
                throw new Exception("Mỗi dòng đơn hàng phải có ít nhất 1 Item.");

            if (request.Details.Any(d => d.Quantity <= 0))
                throw new Exception("Số lượng sản phẩm phải lớn hơn 0.");

            if (request.Details.Any(d => d.UnitPrice <= 0))
                throw new Exception("Đơn giá sản phẩm phải lớn hơn 0.");

            const decimal serviceFee = 15000m;
            decimal totalAmount = request.SubTotal + serviceFee;

            var order = new Order
            {
                BuyerId = request.BuyerId,
                SellerId = sellerId,
                SubTotal = request.SubTotal,
                ServiceFee = serviceFee,
                TotalAmount = totalAmount,
                Status = OrderStatus.PendingPayment,
                Note = request.Note,
                ShippingAddress = request.ShippingAddress,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                CreatedAt = DateTime.UtcNow,
                OrderDetails = request.Details.Select(d => new OrderDetail
                {
                    OutfitId = null,
                    ItemId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    ImageUrl = d.ImageUrl,
                    ItemName = d.ItemName
                }).ToList()
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _orderRepo.CreateAsync(order);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var createdOrder = await _orderRepo.GetByIdAsync(order.OrderId)
                               ?? throw new Exception("Không thể tải lại đơn hàng sau khi tạo.");

            var response = MapToResponse(createdOrder);

            await _hubContext.Clients.Group($"User_{response.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{response.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderRepo.GetByIdAsync(orderId);
        }

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, int currentUserId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) return null;

            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
                throw new UnauthorizedAccessException();

            return MapToResponse(order);
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
            if (!OrderStatus.IsValid(status))
                throw new Exception("Trạng thái đơn hàng không hợp lệ.");

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");

            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
                throw new UnauthorizedAccessException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (status == OrderStatus.Shipping)
                {
                    if (order.SellerId != currentUserId)
                        throw new UnauthorizedAccessException("Chỉ seller mới được xác nhận giao hàng.");

                    if (order.Status != OrderStatus.Processing)
                        throw new Exception("Đơn hàng chưa ở trạng thái có thể giao.");

                    order.Status = OrderStatus.Shipping;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);
                }
                else if (status == OrderStatus.Completed)
                {
                    if (order.BuyerId != currentUserId)
                        throw new UnauthorizedAccessException("Chỉ buyer mới được xác nhận hoàn tất.");

                    if (order.Status != OrderStatus.Shipping)
                        throw new Exception("Đơn hàng chưa ở trạng thái giao hàng.");

                    var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                                      ?? throw new Exception("Ví buyer không tồn tại.");
                    var sellerWallet = await _walletRepo.GetByAccountIdAsync(order.SellerId)
                                       ?? throw new Exception("Ví seller không tồn tại.");

                    if (buyerWallet.LockedBalance < order.TotalAmount)
                        throw new Exception("Số dư khóa không đủ để giải ngân.");

                    decimal buyerBefore = buyerWallet.Balance;
                    decimal sellerBefore = sellerWallet.Balance;

                    buyerWallet.Balance -= order.TotalAmount;
                    buyerWallet.LockedBalance -= order.TotalAmount;
                    buyerWallet.UpdatedAt = DateTime.UtcNow;

                    sellerWallet.Balance += order.TotalAmount;
                    sellerWallet.UpdatedAt = DateTime.UtcNow;

                    _walletRepo.Update(buyerWallet);
                    _walletRepo.Update(sellerWallet);

                    var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
                    if (escrow != null)
                    {
                        escrow.Status = EscrowStatus.Released;
                        escrow.ResolvedAt = DateTime.UtcNow;
                        _escrowRepo.Update(escrow);
                    }

                    order.Status = OrderStatus.Completed;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);

                    await _transactionRepo.AddAsync(new Transaction
                    {
                        WalletId = buyerWallet.WalletId,
                        PaymentId = null,
                        TransactionCode = GenerateTransactionCode("TRX"),
                        Amount = order.TotalAmount,
                        BalanceBefore = buyerBefore,
                        BalanceAfter = buyerWallet.Balance,
                        Type = TransactionType.Debit,
                        ReferenceType = TransactionReferenceType.OrderPayment,
                        ReferenceId = order.OrderId,
                        Description = $"Hoàn tất thanh toán đơn hàng #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow,
                        Status = TransactionStatus.Success
                    });

                    await _transactionRepo.AddAsync(new Transaction
                    {
                        WalletId = sellerWallet.WalletId,
                        PaymentId = null,
                        TransactionCode = GenerateTransactionCode("TRX"),
                        Amount = order.TotalAmount,
                        BalanceBefore = sellerBefore,
                        BalanceAfter = sellerWallet.Balance,
                        Type = TransactionType.Credit,
                        ReferenceType = TransactionReferenceType.OrderPayment,
                        ReferenceId = order.OrderId,
                        Description = $"Nhận tiền từ đơn hàng #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow,
                        Status = TransactionStatus.Success
                    });
                }
                else if (status == OrderStatus.Cancelled || status == OrderStatus.Refunded)
                {
                    if (order.Status == OrderStatus.Processing || order.Status == OrderStatus.Shipping)
                    {
                        var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                                          ?? throw new Exception("Ví buyer không tồn tại.");

                        if (buyerWallet.LockedBalance < order.TotalAmount)
                            throw new Exception("Số dư khóa không đủ để hoàn tiền.");

                        buyerWallet.LockedBalance -= order.TotalAmount;
                        buyerWallet.UpdatedAt = DateTime.UtcNow;
                        _walletRepo.Update(buyerWallet);

                        var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
                        if (escrow != null)
                        {
                            escrow.Status = EscrowStatus.Refunded;
                            escrow.ResolvedAt = DateTime.UtcNow;
                            _escrowRepo.Update(escrow);
                        }

                        await _transactionRepo.AddAsync(new Transaction
                        {
                            WalletId = buyerWallet.WalletId,
                            PaymentId = null,
                            TransactionCode = GenerateTransactionCode("TRX"),
                            Amount = order.TotalAmount,
                            BalanceBefore = buyerWallet.Balance,
                            BalanceAfter = buyerWallet.Balance,
                            Type = TransactionType.Credit,
                            ReferenceType = TransactionReferenceType.OrderRefund,
                            ReferenceId = order.OrderId,
                            Description = $"Hoàn tiền đơn hàng #{order.OrderId}",
                            CreatedAt = DateTime.UtcNow,
                            Status = TransactionStatus.Success
                        });
                    }

                    order.Status = status;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);
                }
                else
                {
                    order.Status = status;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);
                }

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                              ?? throw new Exception("Không tìm thấy đơn hàng sau cập nhật.");

            var response = MapToResponse(updatedOrder);

            await _hubContext.Clients.Group($"User_{updatedOrder.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{updatedOrder.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null) throw new Exception("Order not found");
            if (order.BuyerId != buyerId) throw new UnauthorizedAccessException();
            if (order.Status != OrderStatus.PendingPayment)
                throw new Exception("Invalid order status");

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId);
            if (buyerWallet == null) throw new Exception("Buyer wallet not found");

            decimal availableBalance = buyerWallet.Balance - buyerWallet.LockedBalance;
            if (availableBalance < order.TotalAmount)
                throw new Exception("Insufficient available balance");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                buyerWallet.LockedBalance += order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                order.Status = OrderStatus.Processing;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                var escrowSession = new EscrowSession
                {
                    OrderId = order.OrderId,
                    SenderId = buyerId,
                    ReceiverId = order.SellerId,
                    Amount = order.TotalAmount,
                    ServiceFee = order.ServiceFee,
                    Status = EscrowStatus.Held,
                    Description = $"Thanh toán tạm giữ cho đơn hàng #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow
                };

                await _escrowRepo.AddAsync(escrowSession);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = buyerWallet.WalletId,
                    PaymentId = null,
                    TransactionCode = GenerateTransactionCode("TRX"),
                    Amount = order.TotalAmount,
                    BalanceBefore = buyerWallet.Balance,
                    BalanceAfter = buyerWallet.Balance,
                    Type = TransactionType.Debit,
                    ReferenceType = TransactionReferenceType.OrderPayment,
                    ReferenceId = order.OrderId,
                    Description = $"Giữ tiền thanh toán đơn hàng #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(order.OrderId)
                              ?? throw new Exception("Không tìm thấy đơn hàng sau thanh toán.");

            var response = MapToResponse(updatedOrder);

            await _hubContext.Clients.Group($"User_{updatedOrder.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{updatedOrder.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

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
                SellerName = order.Seller?.UserName ?? "Unknown",
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
                    ItemId = d.ItemId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.UnitPrice * d.Quantity,
                    ItemName = d.ItemName,
                    ImageUrl = d.ImageUrl
                }).ToList()
            };
        }

        private static string GenerateTransactionCode(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
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
            _orderRepo.Update(order);

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.Group($"User_{order.SellerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", response);

            return response;
        }
    }
}