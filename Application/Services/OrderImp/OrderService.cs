using Application.Interfaces;
using Application.Request.NotificationReq;
using Application.Request.OrderReq;
using Application.Response.OrderResp;
using Application.Response.RefundResp;
using Application.Services.NotificationImp;
using Application.Utils;
using Application.Utils.SignalR;
using Domain.Constants;
using Domain.Contracts.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Application.Services.OrderImp
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IItemVariantRepository _variantRepo;
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly IEscrowSessionRepository _escrowRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefundRequestRepository _refundRepo;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly INotificationService _notificationService;
        private readonly ISystemSettingRepository _settingRepo;
        private readonly IOrderStatusHistoryRepository _orderStatusHistoryRepository;
        private readonly IEscrowStatusHistoryRepository _escrowStatusHistoryRepository;

        public OrderService(
            IOrderRepository orderRepo,
            IItemVariantRepository variantRepo,
            IHubContext<OrderHub> hubContext,
            IEscrowSessionRepository escrowRepo,
            IWalletRepository walletRepo,
            ITransactionRepository transactionRepo,
            IUnitOfWork unitOfWork,
            IRefundRequestRepository refundRepo,
            ICloudStorageService cloudStorageService,
            INotificationService notificationService,
            ISystemSettingRepository settingRepo,
            IOrderStatusHistoryRepository orderStatusHistoryRepository,
            IEscrowStatusHistoryRepository escrowStatusHistoryRepository)
        {
            _orderRepo = orderRepo;
            _variantRepo = variantRepo;
            _hubContext = hubContext;
            _escrowRepo = escrowRepo;
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _unitOfWork = unitOfWork;
            _refundRepo = refundRepo;
            _cloudStorageService = cloudStorageService;
            _notificationService = notificationService;
            _settingRepo = settingRepo;
            _orderStatusHistoryRepository = orderStatusHistoryRepository;
            _escrowStatusHistoryRepository = escrowStatusHistoryRepository;
        }

        private async Task<decimal> GetServiceFeeAsync()
            => await _settingRepo.GetDecimalValueAsync("ORDER_SERVICE_FEE", 15000m);

        private async Task SaveStatusHistoryAsync(int orderId, string status, string actorType, int? changedById, string? note = null)
        {
            var history = new OrderStatusHistory
            {
                OrderId = orderId,
                Status = status.ToUpperInvariant(),
                ChangedAt = DateTime.UtcNow,
                ActorType = actorType,
                ChangedById = changedById,
                Note = note
            };
            await _orderStatusHistoryRepository.AddAsync(history);
        }

        private async Task SaveEscrowStatusHistoryAsync(EscrowSession escrowSession, string fromStatus, string toStatus, decimal amountBefore, decimal amountAfter, int? changedById, string? reason)
        {
            var escrowHistory = new EscrowStatusHistory
            {
                EscrowSession = escrowSession,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                AmountBefore = amountBefore,
                AmountAfter = amountAfter,
                ChangedById = changedById,
                Reason = reason,
                ChangedAt = DateTime.UtcNow
            };

            await _escrowStatusHistoryRepository.AddAsync(escrowHistory);
        }

        public async Task<OrderResponse> CreateOrderAsync(int sellerId, int buyerId, CreateOrderRequest request)
        {
            if (request == null)
                throw new ArgumentException("Invalid order data.");

            if (buyerId <= 0)
                throw new ArgumentException("Invalid buyer.");

            if (request.Details == null || !request.Details.Any())
                throw new ArgumentException("The order must contain at least one product.");

            if (request.Details.Any(d => d.ItemVariantId <= 0))
                throw new ArgumentException("Each order line must contain a valid item variant.");

            if (request.Details.Any(d => d.Quantity <= 0))
                throw new ArgumentException("Product quantity must be greater than 0.");

            decimal serviceFee = await GetServiceFeeAsync();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                decimal subTotal = 0m;
                var orderDetails = new List<OrderDetail>();
                int? detectedSellerId = null;

                foreach (var detail in request.Details)
                {
                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId)
                        ?? throw new KeyNotFoundException($"Variant {detail.ItemVariantId} not found.");

                    if (variant.Status != ItemVariantStatus.Active)
                        throw new InvalidOperationException($"Variant {detail.ItemVariantId} is not active.");

                    if (variant.Item == null)
                        throw new KeyNotFoundException("Variant item not found.");

                    if (variant.Item.Status != ItemStatus.Active)
                        throw new InvalidOperationException("Item is not active.");

                    if (!variant.Item.IsForSale)
                        throw new InvalidOperationException("Item is not available for sale.");

                    int variantSellerId = variant.Item.Wardrobe.AccountId;

                    if (variantSellerId == buyerId)
                        throw new InvalidOperationException("Buyer cannot create an order for their own item.");

                    if (detectedSellerId == null)
                    {
                        detectedSellerId = variantSellerId;
                    }
                    else if (detectedSellerId.Value != variantSellerId)
                    {
                        throw new InvalidOperationException("All order items must belong to the same seller.");
                    }

                    if (variantSellerId != sellerId)
                        throw new ArgumentException("Invalid seller for selected item.");

                    if (!_variantRepo.HasEnoughStock(variant, detail.Quantity))
                        throw new InvalidOperationException($"Not enough stock for SKU {variant.Sku}.");

                    _variantRepo.ReserveStock(variant, detail.Quantity);

                    var item = variant.Item;
                    var mainImageUrl = item.Images
                        .OrderBy(x => x.CreatedAt)
                        .Select(x => x.ImageUrl)
                        .FirstOrDefault();

                    decimal lineTotal = variant.Price * detail.Quantity;
                    subTotal += lineTotal;

                    orderDetails.Add(new OrderDetail
                    {
                        ItemId = item.ItemId,
                        ItemVariantId = variant.ItemVariantId,
                        ItemNameSnapshot = item.ItemName ?? "Unknown Item",
                        VariantSnapshot = BuildVariantSnapshot(variant),
                        SkuSnapshot = variant.Sku,
                        ImageUrlSnapshot = mainImageUrl,
                        Quantity = detail.Quantity,
                        UnitPrice = variant.Price,
                        LineTotal = lineTotal
                    });
                }

                var order = new Order
                {
                    BuyerId = buyerId,
                    SellerId = detectedSellerId ?? sellerId,
                    OrderCode = GenerateOrderCode(),
                    SubTotal = subTotal,
                    ServiceFee = serviceFee,
                    TotalAmount = subTotal + serviceFee,
                    Status = OrderStatus.PendingPayment,
                    Note = request.Note,
                    ShippingAddress = request.ShippingAddress,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone,
                    CreatedAt = DateTime.UtcNow,
                    OrderDetails = orderDetails
                };

                await _orderRepo.CreateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                await SaveStatusHistoryAsync(order.OrderId, OrderStatus.PendingPayment, "Buyer", buyerId, "Order initialized by customer.");

                await _unitOfWork.CommitAsync();

                var createdOrder = await _orderRepo.GetByIdAsync(order.OrderId)
                    ?? throw new KeyNotFoundException("Unable to reload the order after creation.");

                var response = MapToResponse(createdOrder);

                await NotifyOrder(response);
                await NotifyOrderEventAsync(response, NotificationType.OrderCreated, buyerId);

                return response;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderResponse> PayOrderWithWalletAsync(int orderId, int buyerId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.BuyerId != buyerId)
                throw new UnauthorizedAccessException("You are not allowed to pay for this order.");

            if (order.Status != OrderStatus.PendingPayment)
                throw new InvalidOperationException("Only pending payment orders can be paid.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var buyerWallet = await _walletRepo.GetByAccountIdForUpdateAsync(buyerId)
                    ?? throw new KeyNotFoundException("Buyer wallet not found.");

                if (buyerWallet.Balance < order.TotalAmount)
                    throw new InvalidOperationException("Insufficient balance. Please top up your wallet and try again.");

                await CheckSpendingLimitAsync(buyerWallet, order.TotalAmount);

                decimal buyerBefore = buyerWallet.Balance;

                buyerWallet.Balance -= order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ItemVariantId.HasValue)
                        throw new InvalidOperationException("Order detail does not contain item variant.");

                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value)
                        ?? throw new KeyNotFoundException("Variant not found.");

                    _variantRepo.ConfirmReservedStock(variant, detail.Quantity);
                }

                order.Status = OrderStatus.Processing;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(order.OrderId, OrderStatus.Processing, "Buyer", buyerId, "Payment successful via Wallet.");

                var escrowSession = new EscrowSession
                {
                    OrderId = order.OrderId,
                    SenderId = buyerId,
                    ReceiverId = order.SellerId,
                    Amount = order.TotalAmount,
                    ServiceFee = order.ServiceFee,
                    Status = EscrowStatus.Held,
                    Description = $"Escrow held for order #{order.OrderId}. Total: {order.TotalAmount:N0} (Fee: {order.ServiceFee:N0})",
                    CreatedAt = DateTime.UtcNow
                };
                await _escrowRepo.AddAsync(escrowSession);

                await SaveEscrowStatusHistoryAsync(
                    escrowSession: escrowSession,
                    fromStatus: "NONE",
                    toStatus: EscrowStatus.Held,
                    amountBefore: 0,
                    amountAfter: order.TotalAmount,
                    changedById: buyerId,
                    reason: $"Order #{order.OrderId} paid successfully. System funds put on escrow hold."
                );

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = buyerWallet.WalletId,
                    EscrowSessionId = escrowSession.EscrowSessionId,
                    TransactionCode = GenerateTransactionCode("TRX"),
                    Amount = order.TotalAmount,
                    BalanceBefore = buyerBefore,
                    BalanceAfter = buyerWallet.Balance,
                    Type = TransactionType.Debit,
                    ReferenceType = TransactionReferenceType.OrderPayment,
                    ReferenceId = order.OrderId,
                    Description = $"Payment for order #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                await _unitOfWork.CommitAsync();

                var response = MapToResponse(order);
                await NotifyOrder(response);
                await NotifyOrderEventAsync(response, NotificationType.OrderPaid, buyerId);

                return response;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
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
                throw new UnauthorizedAccessException("You are not allowed to access this order.");

            return MapToResponse(order, includeHistory: true);
        }

        public async Task<List<OrderResponse>> GetSalesOrdersAsync(int sellerId)
        {
            var orders = await _orderRepo.GetOrdersBySellerIdAsync(sellerId);
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetPurchasesOrdersAsync(int buyerId)
        {
            var orders = await _orderRepo.GetOrdersByBuyerIdAsync(buyerId);
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetPaidOrdersAsync()
        {
            var orders = await _orderRepo.GetPaidOrdersAsync();
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetCompletedOrdersAsync()
        {
            var orders = await _orderRepo.GetCompletedOrdersAsync();
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetDeliveredOrdersAsync()
        {
            var orders = await _orderRepo.GetDeliveredOrdersAsync();
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetCancelledOrdersAsync()
        {
            var orders = await _orderRepo.GetCancelledOrdersAsync();
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<List<OrderResponse>> GetShippingOrdersAsync()
        {
            var orders = await _orderRepo.GetShippingOrdersAsync();
            return orders.Select(o => MapToResponse(o, includeHistory: false)).ToList();
        }

        public async Task<OrderResponse> GetOrderDetailByIdAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            return MapToResponse(order);
        }

        public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, string status, int currentUserId)
        {
            if (!OrderStatus.IsValid(status))
                throw new ArgumentException("Invalid order status.");

            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
                throw new UnauthorizedAccessException("You are not allowed to update this order.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                string actorType = (currentUserId == order.BuyerId) ? "Buyer" : "Seller";
                string? noteHistory = null;

                switch (status)
                {
                    case OrderStatus.Shipping:
                        MarkShipping(order, currentUserId);
                        noteHistory = "Order handed over to shipping partner.";
                        break;

                    case OrderStatus.Delivered:
                        throw new InvalidOperationException("Delivered status must be updated by shipper flow.");

                    case OrderStatus.Completed:
                        await CompleteOrderAndReleaseEscrowAsync(order, currentUserId, isSystemAction: false);
                        noteHistory = "Order successfully completed.";
                        break;

                    case OrderStatus.Cancelled:
                        await HandleCancelAsync(order, currentUserId);
                        noteHistory = $"Order cancelled. Reason: {order.CancelReason}";
                        break;

                    default:
                        throw new InvalidOperationException("This status update is not allowed in this flow.");
                }

                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(order.OrderId, status, actorType, currentUserId, noteHistory);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after update.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);

            var notificationType = response.Status switch
            {
                OrderStatus.Shipping => NotificationType.OrderShipping,
                OrderStatus.Completed => NotificationType.OrderCompleted,
                OrderStatus.Cancelled => NotificationType.OrderCancelled,
                _ => null
            };

            if (notificationType != null)
                await NotifyOrderEventAsync(response, notificationType, currentUserId);

            return response;
        }

        public async Task<OrderResponse> UpdateOrderStatusByShipperAsync(int orderId, string status)
        {
            if (!OrderStatus.IsValid(status))
                throw new ArgumentException("Invalid order status.");

            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                string? noteHistory = null;

                switch (status)
                {
                    case OrderStatus.Shipping:
                        if (order.Status != OrderStatus.Processing)
                            throw new InvalidOperationException("Only processing orders can be marked as shipping.");

                        order.Status = OrderStatus.Shipping;
                        order.UpdatedAt = DateTime.UtcNow;
                        noteHistory = "Shipper confirmed item pickup. Transit initiated.";
                        break;

                    case OrderStatus.Delivered:
                        if (order.Status != OrderStatus.Shipping)
                            throw new InvalidOperationException("Only shipping orders can be marked as delivered.");

                        order.Status = OrderStatus.Delivered;
                        order.UpdatedAt = DateTime.UtcNow;
                        noteHistory = "Shipper delivered order to destination address successfully.";
                        break;

                    case OrderStatus.ReturnPickedUp:
                        if (order.Status != OrderStatus.ReturnApproved)
                            throw new InvalidOperationException("Only return-approved orders can be picked up for return.");

                        order.Status = OrderStatus.ReturnPickedUp;
                        order.UpdatedAt = DateTime.UtcNow;
                        noteHistory = "Shipper picked up the returned item from the buyer.";
                        break;

                    case OrderStatus.ReturnShipping:
                        if (order.Status != OrderStatus.ReturnPickedUp)
                            throw new InvalidOperationException("Only picked-up returns can be marked as return shipping.");

                        order.Status = OrderStatus.ReturnShipping;
                        order.UpdatedAt = DateTime.UtcNow;
                        noteHistory = "Returned item is in transit back to the seller.";
                        break;

                    case OrderStatus.ReturnDelivered:
                        if (order.Status != OrderStatus.ReturnShipping && order.Status != OrderStatus.ReturnPickedUp)
                            throw new InvalidOperationException("Only return-shipping orders can be marked as return delivered.");

                        order.Status = OrderStatus.ReturnDelivered;
                        order.UpdatedAt = DateTime.UtcNow;
                        noteHistory = "Returned item delivered to the seller. Awaiting seller confirmation (auto-refund in 24h).";
                        break;

                    default:
                        throw new InvalidOperationException("Shipper can only update order to shipping, delivered, or the return transit states.");
                }

                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(order.OrderId, status, "Shipper", null, noteHistory);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after shipper update.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);

            switch (response.Status)
            {
                case OrderStatus.Shipping:
                    await NotifyOrderUserAsync(
                        response,
                        NotificationType.OrderShipping,
                        response.SellerId,
                        response.BuyerId,
                        "Order is shipping",
                        $"Your order {response.OrderCode} is now shipping.");
                    break;

                case OrderStatus.Delivered:
                    await NotifyOrderUserAsync(
                        response,
                        NotificationType.OrderDelivered,
                        response.SellerId,
                        response.BuyerId,
                        "Order delivered",
                        $"Your order {response.OrderCode} has been delivered. Please confirm it if everything is okay.");
                    break;

                case OrderStatus.ReturnPickedUp:
                    await NotifyOrderUserAsync(
                        response,
                        NotificationType.RefundApproved,
                        response.SellerId,
                        response.BuyerId,
                        "Return picked up",
                        $"The shipper has picked up the returned item for order {response.OrderCode}.");
                    break;

                case OrderStatus.ReturnShipping:
                    await NotifyOrderUserAsync(
                        response,
                        NotificationType.RefundApproved,
                        response.BuyerId,
                        response.SellerId,
                        "Return on the way",
                        $"The returned item for order {response.OrderCode} is on its way back to you.");
                    break;

                case OrderStatus.ReturnDelivered:
                    await NotifyOrderUserAsync(
                        response,
                        NotificationType.RefundApproved,
                        response.BuyerId,
                        response.SellerId,
                        "Returned item delivered",
                        $"The returned item for order {response.OrderCode} has arrived. Please confirm, otherwise it will auto-refund in 24h.");
                    break;
            }

            return response;
        }

        public async Task<OrderResponse> CreateRefundRequestAsync(
            int orderId,
            int buyerId,
            CreateRefundRequestDto request)
        {
            if (request == null)
                throw new ArgumentException("Invalid refund request data.");

            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("Refund reason is required.");

            if (request.ProofImage1 == null || request.ProofImage1.Length == 0)
                throw new ArgumentException("At least one proof image is required.");

            ValidateRefundImage(request.ProofImage1);

            if (request.ProofImage2 != null && request.ProofImage2.Length > 0)
                ValidateRefundImage(request.ProofImage2);

            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.BuyerId != buyerId)
                throw new UnauthorizedAccessException("You are not allowed to request a refund for this order.");

            if (order.Status != OrderStatus.Delivered)
                throw new InvalidOperationException("Only delivered orders can request a refund.");

            var existingRequest = await _refundRepo.GetByOrderIdAsync(orderId);
            if (existingRequest != null)
                throw new InvalidOperationException("Refund request already exists.");

            string proofImage1Url = await _cloudStorageService.UploadImageAsync(request.ProofImage1);
            string? proofImage2Url = null;

            if (request.ProofImage2 != null && request.ProofImage2.Length > 0)
                proofImage2Url = await _cloudStorageService.UploadImageAsync(request.ProofImage2);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                order.Status = OrderStatus.Refunding;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                var refundRequest = new RefundRequest
                {
                    OrderId = orderId,
                    Reason = request.Reason.Trim(),
                    ProofImage1 = proofImage1Url,
                    ProofImage2 = proofImage2Url,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                await _refundRepo.AddAsync(refundRequest);

                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.Refunding,
                    "Buyer",
                    buyerId,
                    $"Buyer opened a refund request. Reason: {request.Reason.Trim()}");

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after refund request.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.RefundRequested, buyerId);

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
                ItemImage = GetRefundItemImage(r),
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();
        }

        public async Task<List<RefundRequestResponse>> GetMyRefundRequestsAsync(int buyerId)
        {
            var requests = await _refundRepo.GetAllAsync();

            return requests
                .Where(r => r.Order.BuyerId == buyerId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RefundRequestResponse
                {
                    RefundRequestId = r.RefundRequestId,
                    OrderId = r.OrderId,
                    Reason = r.Reason,
                    ProofImage1 = r.ProofImage1,
                    ProofImage2 = r.ProofImage2,
                    ItemImage = GetRefundItemImage(r),
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt
                }).ToList();
        }

        public async Task<OrderResponse> RejectRefundAsync(int orderId, string adminNote)
        {
            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Refund request not found.");

            if (refundRequest.Status != "PENDING")
                throw new InvalidOperationException("Refund request already processed.");

            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Refunding)
                throw new InvalidOperationException("Order is not in refunding status.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                refundRequest.Status = "REJECTED";
                refundRequest.AdminNote = adminNote;
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                await CompleteOrderAndReleaseEscrowAsync(
                    order,
                    order.BuyerId,
                    isSystemAction: true);

                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.Completed,
                    "System",
                    null,
                    $"Admin rejected refund request. Note: {adminNote}. Dispute closed and order marked as Completed.");

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after reject refund.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.RefundRejected, response.BuyerId);
            await NotifyOrderEventAsync(response, NotificationType.RefundRejected, response.SellerId);

            return response;
        }

        public async Task<OrderResponse> UpdateRefundStatus(int orderId)
        {
            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Refund request not found.");

            if (refundRequest.Status != "PENDING")
                throw new InvalidOperationException("Refund request already processed.");

            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Refunding)
                throw new InvalidOperationException("Order is not in refunding status.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                refundRequest.Status = "APPROVED";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                order.Status = OrderStatus.ReturnApproved;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.ReturnApproved,
                    "System",
                    null,
                    "Admin approved the refund request. Awaiting return shipment to the seller. No funds released yet.");

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after approve refund.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);

            await NotifyOrderUserAsync(
                response,
                NotificationType.RefundApproved,
                response.SellerId,
                response.BuyerId,
                "Refund approved",
                $"Your refund request for order {response.OrderCode} has been approved. Please hand the item to the shipper for return.");

            await NotifyOrderUserAsync(
                response,
                NotificationType.RefundApproved,
                response.BuyerId,
                response.SellerId,
                "Return incoming",
                $"Refund for order {response.OrderCode} was approved. The item will be returned to you. Please confirm once received.");

            return response;
        }

        public async Task<OrderResponse> ConfirmReturnReceivedAsync(int orderId, int sellerId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.SellerId != sellerId)
                throw new UnauthorizedAccessException("Only the seller can confirm the returned item.");

            if (order.Status != OrderStatus.ReturnDelivered)
                throw new InvalidOperationException("Only return-delivered orders can be confirmed by the seller.");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Refund request not found.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await ProcessRefundToBuyerAsync(order, refundRequest, sellerId, isSystemAction: false);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after return confirmation.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.RefundApproved, response.SellerId);

            return response;
        }

        public async Task<OrderResponse> AutoRefundReturnDeliveredAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.ReturnDelivered)
                throw new InvalidOperationException("Only return-delivered orders can be auto refunded.");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Refund request not found.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await ProcessRefundToBuyerAsync(order, refundRequest, null, isSystemAction: true);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after auto refund.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.RefundApproved, response.SellerId);

            return response;
        }

        private async Task ProcessRefundToBuyerAsync(
            Order order,
            RefundRequest refundRequest,
            int? changedById,
            bool isSystemAction)
        {
            var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
            if (escrow == null)
                throw new KeyNotFoundException("Escrow session not found.");

            if (escrow.Status != EscrowStatus.Held)
                throw new InvalidOperationException("Escrow is not in a valid held state.");

            var buyerWallet = await _walletRepo.GetByAccountIdForUpdateAsync(order.BuyerId)
                ?? throw new KeyNotFoundException("Buyer wallet not found.");

            decimal buyerBefore = buyerWallet.Balance;

            buyerWallet.Balance += order.TotalAmount;
            buyerWallet.UpdatedAt = DateTime.UtcNow;
            _walletRepo.Update(buyerWallet);

            string oldEscrowStatus = escrow.Status;
            decimal escrowAmountBefore = escrow.Amount;

            escrow.Status = EscrowStatus.Refunded;
            escrow.ResolvedAt = DateTime.UtcNow;
            escrow.Description = $"Refund completed: {order.TotalAmount:N0} returned to Buyer. Escrow closed.";
            _escrowRepo.Update(escrow);

            await SaveEscrowStatusHistoryAsync(
                escrowSession: escrow,
                fromStatus: oldEscrowStatus,
                toStatus: EscrowStatus.Refunded,
                amountBefore: escrowAmountBefore,
                amountAfter: 0,
                changedById: changedById ?? 1,
                reason: isSystemAction
                    ? $"Return auto-completed for Order #{order.OrderId} after 24h. Funds returned to Buyer."
                    : $"Seller confirmed returned item for Order #{order.OrderId}. Funds returned to Buyer.");

            foreach (var detail in order.OrderDetails)
            {
                if (!detail.ItemVariantId.HasValue) continue;

                var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                if (variant != null && variant.Status != ItemVariantStatus.Deleted && variant.Status != ItemVariantStatus.Archived)
                    _variantRepo.Restock(variant, detail.Quantity);
            }

            refundRequest.Status = "COMPLETED";
            refundRequest.ProcessedAt = DateTime.UtcNow;
            _refundRepo.Update(refundRequest);

            order.Status = OrderStatus.Refunded;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepo.Update(order);

            await SaveStatusHistoryAsync(
                order.OrderId,
                OrderStatus.Refunded,
                isSystemAction ? "System" : "Seller",
                changedById,
                isSystemAction
                    ? "Return auto-completed after 24h timeout. Funds returned to Buyer wallet. Items restocked."
                    : "Seller confirmed returned items. Funds returned to Buyer wallet. Items restocked.");

            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = buyerWallet.WalletId,
                EscrowSessionId = escrow.EscrowSessionId,
                TransactionCode = GenerateTransactionCode("REF"),
                Amount = order.TotalAmount,
                BalanceBefore = buyerBefore,
                BalanceAfter = buyerWallet.Balance,
                Type = TransactionType.Credit,
                ReferenceType = TransactionReferenceType.OrderRefund,
                ReferenceId = order.OrderId,
                Description = $"Refund completed for order #{order.OrderId}. Total amount returned.",
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Success
            });
        }

        public async Task<OrderResponse> AutoCompleteDeliveredOrderAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.Delivered)
                throw new InvalidOperationException("Only delivered orders can be auto completed.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await CompleteOrderAndReleaseEscrowAsync(order, order.BuyerId, isSystemAction: true);

                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after auto complete.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.OrderCompleted, response.BuyerId);

            return response;
        }

        public async Task<OrderResponse> AutoCancelPendingPaymentOrderAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            if (order.Status != OrderStatus.PendingPayment)
                throw new InvalidOperationException("Only pending payment orders can be auto cancelled.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ItemVariantId.HasValue) continue;

                    var variant = await _variantRepo.GetByIdAsync(detail.ItemVariantId.Value);
                    if (variant != null)
                        _variantRepo.ReleaseReservedStock(variant, detail.Quantity);
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                order.CancelReason = "Order was automatically cancelled because payment was not completed within 30 minutes.";
                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.Cancelled,
                    "System",
                    null,
                    "System auto-cancelled order due to payment timeout (30-minute window exceeded).");

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after auto cancel.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.OrderCancelled, response.BuyerId);

            return response;
        }

        public async Task<PagedResultDto<OrderResponse>> GetMyPurchasesFilteredAsync(int buyerId, OrderFilterRequest request)
        {
            var (page, pageSize, status) = PreProcessAndValidateFilterRequest(request);

            var result = await _orderRepo.GetOrdersByBuyerIdFilteredAsync(
                buyerId, page, pageSize, status,
                request.FromDate, request.ToDate, request.SellerName, request.OrderCode);

            return new PagedResultDto<OrderResponse>
            {
                Items = result.Orders.Select(o => MapToResponse(o, includeHistory: false)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                HasMore = page * pageSize < result.TotalCount
            };
        }

        public async Task<PagedResultDto<OrderResponse>> GetMySalesFilteredAsync(int sellerId, OrderFilterRequest request)
        {
            var (page, pageSize, status) = PreProcessAndValidateFilterRequest(request);

            var result = await _orderRepo.GetOrdersBySellerIdFilteredAsync(
                sellerId, page, pageSize, status,
                request.FromDate, request.ToDate, request.BuyerName, request.OrderCode);

            return new PagedResultDto<OrderResponse>
            {
                Items = result.Orders.Select(o => MapToResponse(o, includeHistory: false)).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                HasMore = page * pageSize < result.TotalCount
            };
        }

        private (int Page, int PageSize, string? Status) PreProcessAndValidateFilterRequest(OrderFilterRequest? request)
        {
            request ??= new OrderFilterRequest();

            int page = request.Page <= 0 ? 1 : request.Page;
            int pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            if (pageSize > 50)
                pageSize = 50;

            string? status = string.IsNullOrWhiteSpace(request.Status)
                ? null
                : request.Status.Trim();

            if (!string.IsNullOrWhiteSpace(status) && !OrderStatus.IsValid(status))
                throw new ArgumentException("Invalid order status.");

            if (request.FromDate.HasValue &&
                request.ToDate.HasValue &&
                request.FromDate.Value.Date > request.ToDate.Value.Date)
            {
                throw new ArgumentException("From date cannot be later than to date.");
            }

            return (page, pageSize, status);
        }

        private void MarkShipping(Order order, int currentUserId)
        {
            if (order.SellerId != currentUserId)
                throw new UnauthorizedAccessException("Only the seller can confirm shipment.");

            if (order.Status != OrderStatus.Processing)
                throw new InvalidOperationException("The order is not in a shippable state.");

            order.Status = OrderStatus.Shipping;
        }

        private async Task CompleteOrderAndReleaseEscrowAsync(Order order, int actorId, bool isSystemAction = false)
        {
            if (!isSystemAction && order.BuyerId != actorId)
                throw new UnauthorizedAccessException("Only the buyer can complete this order.");

            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Refunding)
                throw new InvalidOperationException("Only delivered or refund-rejected orders can be completed.");

            var sellerWallet = await _walletRepo.GetByAccountIdForUpdateAsync(order.SellerId)
                ?? throw new KeyNotFoundException("Seller wallet not found.");

            var adminWallet = await _walletRepo.GetByAccountIdForUpdateAsync(1)
                ?? throw new KeyNotFoundException("Admin wallet not found.");

            var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
            if (escrow == null)
                throw new KeyNotFoundException("Escrow session not found for this order.");

            if (escrow.Status != EscrowStatus.Held)
                throw new InvalidOperationException("Escrow is not in a valid held state.");

            // 2. Tính toán dòng tiền từ Quỹ Escrow gốc
            decimal escrowTotalAmount = escrow.Amount;
            decimal adminServiceFee = order.ServiceFee;
            decimal sellerReceiveAmount = escrowTotalAmount - adminServiceFee;

            if (sellerReceiveAmount <= 0)
                throw new InvalidOperationException("Invalid seller payout amount.");


            // Thay đổi trạng thái Escrow gốc về 0 (Đã giải phóng hoàn toàn)
            string oldEscrowStatus = escrow.Status;
            escrow.Status = EscrowStatus.Released;
            escrow.Amount = 0;
            escrow.ResolvedAt = DateTime.UtcNow;
            escrow.Description = $"Fully dispersed: {sellerReceiveAmount:N0} VND to Seller, {adminServiceFee:N0} VND to Admin.";
            _escrowRepo.Update(escrow);

            // 3. Cập nhật số dư ví Seller & Ghi nhận lịch sử Escrow cho Seller
            decimal sellerBefore = sellerWallet.Balance;
            sellerWallet.Balance += sellerReceiveAmount;
            sellerWallet.UpdatedAt = DateTime.UtcNow;
            _walletRepo.Update(sellerWallet);

            // Lịch sử Escrow phần 1: Trích chi cho Seller
            await SaveEscrowStatusHistoryAsync(
                escrowSession: escrow,
                fromStatus: oldEscrowStatus,
                toStatus: EscrowStatus.PartiallyReleased,
                amountBefore: escrowTotalAmount,
                amountAfter: adminServiceFee,
                changedById: isSystemAction ? null : actorId,
                reason: isSystemAction
                    ? $"[Part 1/2 - Seller Payout] Auto release {sellerReceiveAmount:N0} VND to Seller (Wallet ID: {sellerWallet.WalletId}) due to system timeout."
                    : $"[Part 1/2 - Seller Payout] Buyer confirmed delivery. Released {sellerReceiveAmount:N0} VND to Seller (Wallet ID: {sellerWallet.WalletId}).");

            // 4. Cập nhật số dư ví Admin & Ghi nhận lịch sử Escrow cho Admin
            decimal adminBefore = adminWallet.Balance;
            if (adminServiceFee > 0)
            {
                adminWallet.Balance += adminServiceFee;
                adminWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(adminWallet);

                // Lịch sử Escrow phần 2: Trích chi nốt phần phí cho Admin
                await SaveEscrowStatusHistoryAsync(
                    escrowSession: escrow,
                    fromStatus: EscrowStatus.PartiallyReleased,
                    toStatus: EscrowStatus.Released,
                    amountBefore: adminServiceFee,
                    amountAfter: 0,
                    changedById: isSystemAction ? null : actorId,
                    reason: isSystemAction
                        ? $"[Part 2/2 - Admin Fee] Auto collect system fee {adminServiceFee:N0} VND to Admin Wallet."
                        : $"[Part 2/2 - Admin Fee] System fee {adminServiceFee:N0} VND collected upon buyer confirmation.");
            }

            // 5. Cập nhật trạng thái của Đơn hàng (Không gọi ghi log Order History ở đây nữa để tránh trùng lặp)
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepo.Update(order);

            // 6. Lưu Transaction Seller
            await _transactionRepo.AddAsync(new Transaction
            {
                WalletId = sellerWallet.WalletId,
                PaymentId = null,
                EscrowSessionId = escrow.EscrowSessionId,
                TransactionCode = GenerateTransactionCode("TRX"),
                Amount = sellerReceiveAmount,
                BalanceBefore = sellerBefore,
                BalanceAfter = sellerWallet.Balance,
                Type = TransactionType.Credit,
                ReferenceType = TransactionReferenceType.OrderPayment,
                ReferenceId = order.OrderId,
                Description = isSystemAction
                    ? $"Auto release payment for order {order.OrderCode}"
                    : $"Receive payment from order {order.OrderCode}",
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Success
            });

            // 7. Lưu Transaction - Admin
            if (adminServiceFee > 0)
            {
                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = adminWallet.WalletId,
                    PaymentId = null,
                    EscrowSessionId = escrow.EscrowSessionId,
                    TransactionCode = GenerateTransactionCode("TAX"),
                    Amount = adminServiceFee,
                    BalanceBefore = adminBefore,
                    BalanceAfter = adminWallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.OrderPayment,
                    ReferenceId = order.OrderId,
                    Description = $"Service fee collected from order #{order.OrderId}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });
            }
        }

        private async Task HandleCancelAsync(Order order, int currentUserId)
        {
            if (order.Status == OrderStatus.PendingPayment)
            {
                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (!detail.ItemVariantId.HasValue) continue;

                        var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                        if (variant != null)
                            _variantRepo.ReleaseReservedStock(variant, detail.Quantity);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                string actorType = currentUserId == order.BuyerId ? "Buyer" : currentUserId == order.SellerId ? "Seller" : "System";
                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.Cancelled,
                    actorType,
                    currentUserId,
                    $"Order cancelled prior to payment completion by {actorType}.");

                return;
            }

            if (order.Status == OrderStatus.Processing)
            {
                var buyerWallet = await _walletRepo.GetByAccountIdForUpdateAsync(order.BuyerId)
                    ?? throw new KeyNotFoundException("Buyer wallet not found.");

                var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
                if (escrow == null)
                    throw new KeyNotFoundException("Escrow session not found.");

                if (escrow.Status != EscrowStatus.Held)
                    throw new InvalidOperationException("Escrow is not in a valid held state.");

                decimal buyerBefore = buyerWallet.Balance;

                buyerWallet.Balance += order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                string oldEscrowStatus = escrow.Status;
                decimal escrowAmountBefore = escrow.Amount;

                escrow.Status = EscrowStatus.Refunded;
                escrow.ResolvedAt = DateTime.UtcNow;
                escrow.Description = $"Order Cancelled: Money released back to buyer. ActorId: {currentUserId}";
                _escrowRepo.Update(escrow);

                string actor = currentUserId == order.BuyerId ? "Buyer" : currentUserId == order.SellerId ? "Seller" : "System";
                await SaveEscrowStatusHistoryAsync(
                    escrowSession: escrow,
                    fromStatus: oldEscrowStatus,
                    toStatus: EscrowStatus.Refunded,
                    amountBefore: escrowAmountBefore,
                    amountAfter: 0,
                    changedById: currentUserId == 0 ? 1 : currentUserId,
                    reason: $"Order cancelled by {actor}. Money released back to Buyer wallet.");

                if (order.OrderDetails != null)
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        if (!detail.ItemVariantId.HasValue) continue;

                        var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                        if (variant != null)
                            _variantRepo.Restock(variant, detail.Quantity);
                    }
                }

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = buyerWallet.WalletId,
                    EscrowSessionId = escrow.EscrowSessionId,
                    TransactionCode = GenerateTransactionCode("REF"),
                    Amount = order.TotalAmount,
                    BalanceBefore = buyerBefore,
                    BalanceAfter = buyerWallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.OrderRefund,
                    ReferenceId = order.OrderId,
                    Description = $"Refund due to order #{order.OrderId} cancellation before shipping.",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await SaveStatusHistoryAsync(
                    order.OrderId,
                    OrderStatus.Cancelled,
                    actor,
                    currentUserId,
                    $"Order cancelled in processing state by {actor}. Full refund processed to Buyer wallet.");

                return;
            }

            throw new InvalidOperationException("This order can no longer be cancelled in its current state.");
        }

        private async Task CheckSpendingLimitAsync(Wallet wallet, decimal debitAmount)
        {
            if (wallet == null)
                throw new KeyNotFoundException("Wallet not found.");

            if (debitAmount <= 0)
                throw new ArgumentException("Invalid spending amount.");

            if (!wallet.MonthlySpendingLimit.HasValue || wallet.MonthlySpendingLimit.Value <= 0)
                return;

            var now = DateTime.UtcNow;

            decimal spentThisMonth = await _transactionRepo.GetMonthlyDebitTotalAsync(
                wallet.WalletId, now.Month, now.Year);

            decimal projectedSpent = spentThisMonth + debitAmount;
            decimal limitAmount = wallet.MonthlySpendingLimit.Value;

            if (wallet.IsHardSpendingLimit && projectedSpent > limitAmount)
            {
                throw new InvalidOperationException(
                    $"You have exceeded your monthly spending limit. " +
                    $"Spent this month: {spentThisMonth:N0} VND, " +
                    $"new transaction: {debitAmount:N0} VND, " +
                    $"limit: {limitAmount:N0} VND.");
            }
        }

        private OrderResponse MapToResponse(Order order, bool includeHistory = false)
        {
            var response = new OrderResponse
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                BuyerId = order.BuyerId,
                BuyerName = order.Buyer?.UserName ?? "Unknown",
                SellerId = order.SellerId,
                SellerName = order.Seller?.UserName ?? "Unknown",
                SubTotal = order.SubTotal,
                ServiceFee = order.ServiceFee,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                Note = order.Note,
                CancelReason = order.CancelReason,
                ShippingAddress = order.ShippingAddress,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderDetails = order.OrderDetails.Select(d => new OrderDetailResponse
                {
                    OrderDetailId = d.OrderDetailId,
                    OrderId = d.OrderId,
                    ItemId = d.ItemId,
                    ItemVariantId = d.ItemVariantId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.LineTotal,
                    ItemName = d.ItemNameSnapshot,
                    VariantSnapshot = d.VariantSnapshot,
                    SkuSnapshot = d.SkuSnapshot,
                    ImageUrl = !string.IsNullOrWhiteSpace(d.ImageUrlSnapshot)
                        ? d.ImageUrlSnapshot
                        : d.Item?.Images.OrderBy(i => i.CreatedAt).Select(i => i.ImageUrl).FirstOrDefault()
                }).ToList()
            };

            if (includeHistory && order.StatusHistories != null)
            {
                response.StatusHistories = order.StatusHistories
                    .OrderByDescending(h => h.ChangedAt)
                    .Select(h => new OrderStatusHistoryResponse
                    {
                        Id = h.Id,
                        Status = h.Status,
                        ChangedAt = h.ChangedAt,
                        ActorType = h.ActorType,
                        ChangedById = h.ChangedById,
                        Note = h.Note
                    }).ToList();
            }

            return response;
        }

        private static void ValidateRefundImage(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException("Only JPG, JPEG, PNG, and WEBP images are allowed.");

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
                throw new ArgumentException("Each proof image must be less than 5MB.");
        }

        private async Task NotifyOrder(OrderResponse response)
        {
            await _hubContext.Clients.Group($"User_{response.BuyerId}")
                .SendAsync("ReceiveNewOrder", response);

            await _hubContext.Clients.Group($"User_{response.SellerId}")
                .SendAsync("ReceiveNewOrder", response);
        }

        private static string BuildVariantSnapshot(ItemVariant variant)
        {
            string size = string.IsNullOrWhiteSpace(variant.SizeCode) ? "N/A" : variant.SizeCode;
            string color = string.IsNullOrWhiteSpace(variant.Color) ? "N/A" : variant.Color;
            return $"Size: {size}, Color: {color}";
        }

        private static string GenerateTransactionCode(string prefix)
            => $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        private static string GenerateOrderCode()
            => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        private static string? GetRefundItemImage(RefundRequest refundRequest)
        {
            var firstDetail = refundRequest.Order.OrderDetails.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstDetail?.ImageUrlSnapshot))
                return firstDetail.ImageUrlSnapshot;

            return firstDetail?.Item?.Images
                .OrderBy(i => i.CreatedAt)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();
        }

        private async Task NotifyOrderUserAsync(
            OrderResponse order,
            string type,
            int senderId,
            int targetUserId,
            string title,
            string content)
        {
            await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                SenderId = senderId,
                TargetUserId = targetUserId,
                Title = title,
                Content = content,
                Type = type,
                RelatedId = order.OrderId.ToString()
            });
        }

        private async Task NotifyOrderEventAsync(OrderResponse order, string type, int actorId)
        {
            switch (type)
            {
                case NotificationType.OrderCreated:
                    await NotifyOrderUserAsync(order, type, actorId, order.SellerId,
                        "New order received",
                        $"You have received a new order {order.OrderCode} from {order.BuyerName}.");
                    break;

                case NotificationType.OrderPaid:
                    await NotifyOrderUserAsync(order, type, actorId, order.SellerId,
                        "Order paid",
                        $"Order {order.OrderCode} has been paid by {order.BuyerName}.");
                    break;

                case NotificationType.OrderShipping:
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Order is shipping",
                        $"Your order {order.OrderCode} is now shipping.");
                    break;

                case NotificationType.OrderDelivered:
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Order delivered",
                        $"Your order {order.OrderCode} has been delivered. Please confirm it if everything is okay.");
                    break;

                case NotificationType.OrderCompleted:
                    await NotifyOrderUserAsync(order, type, actorId, order.SellerId,
                        "Order completed",
                        $"Order {order.OrderCode} has been completed. Payment has been released to your wallet.");
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Order completed",
                        $"Your order {order.OrderCode} has been completed.");
                    break;

                case NotificationType.OrderCancelled:
                    var targetId = actorId == order.BuyerId ? order.SellerId : order.BuyerId;
                    await NotifyOrderUserAsync(order, type, actorId, targetId,
                        "Order cancelled",
                        $"Order {order.OrderCode} has been cancelled.");
                    break;

                case NotificationType.RefundRequested:
                    await NotifyOrderUserAsync(order, type, actorId, order.SellerId,
                        "Refund requested",
                        $"{order.BuyerName} requested a refund for order {order.OrderCode}.");
                    break;

                case NotificationType.RefundApproved:
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Refund approved",
                        $"Your refund request for order {order.OrderCode} has been approved.");
                    await NotifyOrderUserAsync(order, type, actorId, order.SellerId,
                        "Order refunded",
                        $"Refund request for order {order.OrderCode} has been approved. Please wait for the returned item.");
                    break;

                case NotificationType.RefundRejected:
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Refund rejected",
                        $"Your refund request for order {order.OrderCode} has been rejected.");
                    break;

                case "OrderRefunded":
                    await NotifyOrderUserAsync(order, type, actorId, order.BuyerId,
                        "Order refunded",
                        $"The seller has received the returned item. Order {order.OrderCode} has been refunded to your wallet.");
                    break;
            }
        }
    }
}