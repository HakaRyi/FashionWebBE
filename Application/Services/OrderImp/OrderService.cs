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
        const decimal serviceFee = 15000m;

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
                else if (status == OrderStatus.Done)
                {
                    if (order.BuyerId != currentUserId)
                        throw new UnauthorizedAccessException("Chỉ buyer mới được xác nhận hoàn tất.");

                    if (order.Status != OrderStatus.Completed)
                        throw new Exception("Đơn hàng chưa giao thành công.");

                    var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                                      ?? throw new Exception("Ví buyer không tồn tại.");
                    var sellerWallet = await _walletRepo.GetByAccountIdAsync(order.SellerId)
                                       ?? throw new Exception("Ví seller không tồn tại.");

                    if (buyerWallet.LockedBalance < order.TotalAmount)
                        throw new Exception("Số dư khóa không đủ để giải ngân.");

                    decimal buyerBefore = buyerWallet.Balance;
                    decimal sellerBefore = sellerWallet.Balance;

                    buyerWallet.LockedBalance -= order.TotalAmount;
                    buyerWallet.UpdatedAt = DateTime.UtcNow;

                    sellerWallet.Balance += order.TotalAmount - serviceFee;
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

                    order.Status = OrderStatus.Done;
                    order.UpdatedAt = DateTime.UtcNow;
                    _orderRepo.Update(order);

                    //await _transactionRepo.AddAsync(new Transaction
                    //{
                    //    WalletId = buyerWallet.WalletId,
                    //    PaymentId = null,
                    //    TransactionCode = GenerateTransactionCode("TRX"),
                    //    Amount = order.TotalAmount,
                    //    BalanceBefore = buyerBefore,
                    //    BalanceAfter = buyerWallet.Balance,
                    //    Type = TransactionType.Debit,
                    //    ReferenceType = TransactionReferenceType.OrderPayment,
                    //    ReferenceId = order.OrderId,
                    //    Description = $"Hoàn tất thanh toán đơn hàng #{order.OrderId}",
                    //    CreatedAt = DateTime.UtcNow,
                    //    Status = TransactionStatus.Success
                    //});

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
            if (order.Status != OrderStatus.Confirm)
                throw new Exception("Invalid order status");

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId);
            if (buyerWallet == null) throw new Exception("Buyer wallet not found");

            decimal availableBalance = buyerWallet.Balance - buyerWallet.LockedBalance;
            if (availableBalance < order.TotalAmount)
                throw new Exception("Insufficient available balance");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                buyerWallet.Balance -= order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

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

            order.Status = status.ToUpper();
            _orderRepo.Update(order);
            await _unitOfWork.SaveChangesAsync();

            var response = MapToResponse(order);

            await _hubContext.Clients.Group($"User_{order.BuyerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.Group($"User_{order.SellerId}").SendAsync("ReceiveNewOrder", response);
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", response);

            return response;
        }

        public async Task<OrderResponse> CreateRefundRequestAsync(int orderId, int buyerId, string reason, string proof1, string proof2)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);

            if (order == null) throw new Exception("Order not found");
            if (order.BuyerId != buyerId) throw new UnauthorizedAccessException();
            if (order.Status != OrderStatus.Completed) throw new Exception("Order is not eligible for refund");

            var existingRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (existingRequest != null) throw new Exception("Refund request already exists");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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

        public async Task<OrderResponse> ProcessRefundAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId);
            if (order == null) throw new Exception("Order not found");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (refundRequest == null) throw new Exception("Refund request not found");
            if (refundRequest.Status != "PENDING") throw new Exception("Refund request already processed");

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId);
            if (buyerWallet == null) throw new Exception("Buyer wallet not found");

            var escrow = await _escrowRepo.GetByOrderIdAsync(orderId);
            if (escrow == null) throw new Exception("Escrow session not found");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                buyerWallet.Balance += order.TotalAmount;
                buyerWallet.LockedBalance -= order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                order.Status = OrderStatus.Refunded;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                escrow.Status = EscrowStatus.Refunded;
                _escrowRepo.Update(escrow);

                refundRequest.Status = "APPROVED";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = buyerWallet.WalletId,
                    PaymentId = null,
                    TransactionCode = GenerateTransactionCode("REF"),
                    Amount = order.TotalAmount,
                    BalanceBefore = buyerWallet.Balance,
                    BalanceAfter = buyerWallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.OrderRefund,
                    ReferenceId = order.OrderId,
                    Description = $"Hoàn tiền từ đơn hàng bị trả #{order.OrderId}",
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
                               ?? throw new Exception("Error retrieving order after refund");

            var response = MapToResponse(updatedOrder);

            await _hubContext.Clients.Group($"User_{updatedOrder.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{updatedOrder.SellerId}")
                .SendAsync("ReceiveNewOrder", response);

            return response;
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
            if (order == null) throw new Exception("Order not found");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (refundRequest == null) throw new Exception("Refund request not found");
            if (refundRequest.Status != "PENDING") throw new Exception("Refund request already processed");

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

            var myRequests = requests.Where(r => r.Order.BuyerId == buyerId)
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
    }
}