using Application.Interfaces;
using Application.Request.OrderReq;
using Application.Response.OrderResp;
using Application.Response.RefundResp;
using Application.Utils.SignalR;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services.OrderImp
{
    public class OrderService : IOrderService
    {
        private const decimal serviceFee = 15000m;

        private readonly IOrderRepository _orderRepo;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestRepository _refundRepo;

        public OrderService(
            IOrderRepository orderRepo,
            IHubContext<OrderHub> hubContext,
            IEscrowSessionRepository escrowRepo,
            IWalletRepository walletRepo,
            ITransactionRepository transactionRepo,
            IUnitOfWork unitOfWork,
            IRefundRequestRepository refundRepo)
        {
            _orderRepo = orderRepo;
            _hubContext = hubContext;
            _escrowRepo = escrowRepo;
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _unitOfWork = unitOfWork;
            _refundRepo = refundRepo;
        }

        public async Task<OrderResponse> CreateOrderAsync(int sellerId, CreateOrderRequest request)
        {
            if (request == null)
                throw new Exception("Invalid order data.");

            if (request.BuyerId <= 0)
                throw new Exception("Invalid buyer.");

            if (request.Details == null || !request.Details.Any())
                throw new Exception("The order must contain at least one product.");

            if (request.Details.Any(d => d.ProductId == null))
                throw new Exception("Each order line must contain at least one item.");

            if (request.Details.Any(d => d.Quantity <= 0))
                throw new Exception("Product quantity must be greater than 0.");

            if (request.Details.Any(d => d.UnitPrice <= 0))
                throw new Exception("Unit price must be greater than 0.");

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
                               ?? throw new Exception("Unable to reload the order after creation.");

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
            if (order == null)
                return null;

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
                throw new Exception("Invalid order status.");

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
                throw new UnauthorizedAccessException();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (status == OrderStatus.Shipping)
                {
                    if (order.SellerId != currentUserId)
                        throw new UnauthorizedAccessException("Only the seller can confirm shipment.");

                    if (order.Status != OrderStatus.Processing)
                        throw new Exception("The order is not in a shippable state.");

                    order.Status = OrderStatus.Shipping;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);
                }
                else if (status == OrderStatus.Done)
                {
                    if (order.Status != OrderStatus.Completed)
                        throw new Exception("The order has not been delivered successfully yet.");

                    var sellerWallet = await _walletRepo.GetByAccountIdAsync(order.SellerId)
                                       ?? throw new Exception("Seller wallet not found.");

                    var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
                    if (escrow == null)
                        throw new Exception("Escrow session not found for this order.");

                    if (escrow.Status != EscrowStatus.Held)
                        throw new Exception("Escrow is not in a valid held state.");

                    decimal sellerBefore = sellerWallet.Balance;
                    decimal sellerReceiveAmount = order.TotalAmount - order.ServiceFee;

                    if (sellerReceiveAmount < 0)
                        throw new Exception("Invalid seller payout amount.");

                    sellerWallet.Balance += sellerReceiveAmount;
                    sellerWallet.UpdatedAt = DateTime.UtcNow;
                    _walletRepo.Update(sellerWallet);

                    escrow.Status = EscrowStatus.Released;
                    escrow.ResolvedAt = DateTime.UtcNow;
                    _escrowRepo.Update(escrow);

                    order.Status = OrderStatus.Done;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);

                    await _transactionRepo.AddAsync(new Transaction
                    {
                        WalletId = sellerWallet.WalletId,
                        PaymentId = null,
                        TransactionCode = GenerateTransactionCode("TRX"),
                        Amount = sellerReceiveAmount,
                        BalanceBefore = sellerBefore,
                        BalanceAfter = sellerWallet.Balance,
                        Type = TransactionType.Credit,
                        ReferenceType = TransactionReferenceType.OrderPayment,
                        ReferenceId = order.OrderId,
                        Description = $"Receive payment from order #{order.OrderId}",
                        CreatedAt = DateTime.UtcNow,
                        Status = TransactionStatus.Success
                    });
                }
                else if (status == OrderStatus.Cancelled)
                {
                    if (order.Status == OrderStatus.Processing || order.Status == OrderStatus.Shipping)
                    {
                        var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                                          ?? throw new Exception("Buyer wallet not found.");

                        decimal buyerBefore = buyerWallet.Balance;

                        buyerWallet.Balance += order.TotalAmount;
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
                            BalanceBefore = buyerBefore,
                            BalanceAfter = buyerWallet.Balance,
                            Type = TransactionType.Credit,
                            ReferenceType = TransactionReferenceType.OrderRefund,
                            ReferenceId = order.OrderId,
                            Description = $"Refund for order #{order.OrderId}",
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
                              ?? throw new Exception("Order not found after update.");

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

            if (order == null)
                throw new Exception("Order not found.");

            if (order.BuyerId != buyerId)
                throw new UnauthorizedAccessException();

            if (order.Status != OrderStatus.Confirm)
                throw new Exception("Invalid order status.");

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId);
            if (buyerWallet == null)
                throw new Exception("Buyer wallet not found.");

            if (buyerWallet.Balance < order.TotalAmount)
                throw new Exception("Insufficient balance.");

            await CheckSpendingLimitAsync(buyerWallet, order.TotalAmount);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId)
                    ?? throw new Exception("Buyer wallet not found.");

                if (buyerWallet.Balance < order.TotalAmount)
                    throw new Exception("Insufficient balance.");

                await CheckSpendingLimitAsync(buyerWallet, order.TotalAmount);

                decimal buyerBefore = buyerWallet.Balance;

                buyerWallet.Balance -= order.TotalAmount;
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
                    Description = $"Escrow payment for order #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow
                };

                await _escrowRepo.AddAsync(escrowSession);

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
                    Description = $"Pay for order #{order.OrderId}",
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
                              ?? throw new Exception("Order not found after payment.");

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
            if (order == null)
                throw new Exception("Order not found.");

            return MapToResponse(order);
        }

        public async Task<OrderResponse> UpdateOrderStatusByShipperAsync(int orderId, string status)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            order.Status = status.ToUpper();
            _orderRepo.Update(order);
            await _unitOfWork.SaveChangesAsync();

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{order.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.All
                .SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<OrderResponse> CreateRefundRequestAsync(
            int orderId,
            int buyerId,
            string reason,
            string proof1,
            string proof2)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null)
                throw new Exception("Order not found.");

            if (order.BuyerId != buyerId)
                throw new UnauthorizedAccessException();

            if (order.Status != OrderStatus.Completed)
                throw new Exception("Order is not eligible for refund.");

            var existingRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (existingRequest != null)
                throw new Exception("Refund request already exists.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                order.Status = OrderStatus.Refunding;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                var refundRequest = new RefundRequest
                {
                    OrderId = orderId,
                    Reason = reason,
                    ProofImage1 = proof1,
                    ProofImage2 = proof2,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                await _refundRepo.AddAsync(refundRequest);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            return MapToResponse(order);
        }      

        public async Task<List<RefundRequestResponse>> GetAllRefundRequestsAsync()
        {
            var requests = await _refundRepo.GetAllAsync();

            return requests.Select(r => new RefundRequestResponse
            {
                RefundRequestId = r.RefundRequestId,
                OrderId = r.OrderId,
                Reason = r.Reason,
                ProofImage1 = r.ProofImage1,
                ProofImage2 = r.ProofImage2,
                ItemImage = r.Order.OrderDetails.FirstOrDefault()?.ImageUrl,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();
        }

        public async Task<OrderResponse> RejectRefundAsync(int orderId, string adminNote)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (refundRequest == null)
                throw new Exception("Refund request not found.");

            if (refundRequest.Status != "PENDING")
                throw new Exception("Refund request already processed.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                order.Status = OrderStatus.Done;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                refundRequest.Status = "REJECTED";
                refundRequest.AdminNote = adminNote;
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            return MapToResponse(order);
        }

        public async Task<List<RefundRequestResponse>> GetMyRefundRequestsAsync(int buyerId)
        {
            var requests = await _refundRepo.GetAllAsync();

            var myRequests = requests
                .Where(r => r.Order.BuyerId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return myRequests.Select(r => new RefundRequestResponse
            {
                RefundRequestId = r.RefundRequestId,
                OrderId = r.OrderId,
                Reason = r.Reason,
                ProofImage1 = r.ProofImage1,
                ProofImage2 = r.ProofImage2,
                ItemImage = r.Order.OrderDetails.FirstOrDefault()?.ImageUrl,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();
        }

        public async Task<OrderResponse> UpdateRefundStatus(int orderId)
        {
            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (refundRequest == null)
                throw new Exception("Refund request not found.");

            if (refundRequest.Status != "PENDING")
                throw new Exception("Refund request already processed.");

            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null)
                throw new Exception("Order not found.");

            if (order.Status != OrderStatus.Refunding)
                throw new Exception("Order is not in refunding status.");

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId);
            if (buyerWallet == null)
                throw new Exception("Buyer wallet not found.");

            var escrow = await _escrowRepo.GetByOrderIdAsync(orderId);
            if (escrow == null)
                throw new Exception("Escrow session not found.");

            Order updatedOrder;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                decimal buyerBefore = buyerWallet.Balance;

                buyerWallet.Balance += order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                escrow.Status = EscrowStatus.Refunded;
                escrow.ResolvedAt = DateTime.UtcNow;
                _escrowRepo.Update(escrow);

                refundRequest.Status = "APPROVED";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                order.Status = OrderStatus.Refunded;
                order.UpdatedAt = DateTime.UtcNow;
                updatedOrder = _orderRepo.Update(order);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = buyerWallet.WalletId,
                    PaymentId = null,
                    TransactionCode = GenerateTransactionCode("REF"),
                    Amount = order.TotalAmount,
                    BalanceBefore = buyerBefore,
                    BalanceAfter = buyerWallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.OrderRefund,
                    ReferenceId = order.OrderId,
                    Description = $"Refund for returned order #{order.OrderId}",
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

            var response = MapToResponse(updatedOrder);

            await _hubContext.Clients.Group($"User_{updatedOrder.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{updatedOrder.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

            return response;
        }

        private async Task CheckSpendingLimitAsync(Wallet wallet, decimal debitAmount)
        {
            if (wallet == null)
                throw new Exception("Wallet not found.");

            if (debitAmount <= 0)
                throw new Exception("Invalid spending amount.");

            if (!wallet.MonthlySpendingLimit.HasValue || wallet.MonthlySpendingLimit.Value <= 0)
                return;

            var now = DateTime.UtcNow;

            decimal spentThisMonth = await _transactionRepo.GetMonthlyDebitTotalAsync(
                wallet.WalletId,
                now.Month,
                now.Year);

            decimal projectedSpent = spentThisMonth + debitAmount;
            decimal limitAmount = wallet.MonthlySpendingLimit.Value;

            if (wallet.IsHardSpendingLimit && projectedSpent > limitAmount)
            {
                throw new Exception(
                    $"You have exceeded your monthly spending limit. " +
                    $"Spent this month: {spentThisMonth:N0} VND, " +
                    $"new transaction: {debitAmount:N0} VND, " +
                    $"limit: {limitAmount:N0} VND.");
            }
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
    }
}