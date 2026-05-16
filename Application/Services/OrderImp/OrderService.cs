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
            ISystemSettingRepository settingRepo)
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
        }

        private async Task<decimal> GetServiceFeeAsync()
        => await _settingRepo.GetDecimalValueAsync("ORDER_SERVICE_FEE", 15000m);

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

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId)
                ?? throw new KeyNotFoundException("Buyer wallet not found.");

            if (buyerWallet.Balance < order.TotalAmount)
                throw new InvalidOperationException("Insufficient balance. Please top up your wallet and try again.");

            await CheckSpendingLimitAsync(buyerWallet, order.TotalAmount);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                buyerWallet = await _walletRepo.GetByAccountIdAsync(buyerId)
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
                order.PaidAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

                await _escrowRepo.AddAsync(new EscrowSession
                {
                    OrderId = order.OrderId,
                    SenderId = buyerId,
                    ReceiverId = order.SellerId,
                    Amount = order.TotalAmount,
                    ServiceFee = order.ServiceFee,
                    Status = EscrowStatus.Held,
                    Description = $"Escrow payment for order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow
                });

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
                    Description = $"Pay for order {order.OrderCode}",
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
                ?? throw new KeyNotFoundException("Order not found after payment.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.OrderPaid, buyerId);

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
                throw new UnauthorizedAccessException("You are not allowed to access this order.");

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

        public async Task<List<OrderResponse>> GetDeliveredOrdersAsync()
        {
            var orders = await _orderRepo.GetDeliveredOrdersAsync();
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
                switch (status)
                {
                    case OrderStatus.Shipping:
                        MarkShipping(order, currentUserId);
                        break;

                    case OrderStatus.Delivered:
                        throw new InvalidOperationException("Delivered status must be updated by shipper flow.");

                    case OrderStatus.Completed:
                        await CompleteOrderAndReleaseEscrowAsync(
                            order,
                            currentUserId,
                            isSystemAction: false);
                        break;

                    case OrderStatus.Cancelled:
                        await HandleCancelAsync(order, currentUserId);
                        break;

                    default:
                        throw new InvalidOperationException("This status update is not allowed in this flow.");
                }

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
            {
                await NotifyOrderEventAsync(response, notificationType, currentUserId);
            }

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
                switch (status)
                {
                    case OrderStatus.Shipping:
                        if (order.Status != OrderStatus.Processing)
                            throw new InvalidOperationException("Only processing orders can be marked as shipping.");

                        order.Status = OrderStatus.Shipping;
                        order.UpdatedAt = DateTime.UtcNow;
                        break;

                    case OrderStatus.Delivered:
                        if (order.Status != OrderStatus.Shipping)
                            throw new InvalidOperationException("Only shipping orders can be marked as delivered.");

                        order.Status = OrderStatus.Delivered;
                        order.DeliveredAt = DateTime.UtcNow;
                        order.UpdatedAt = DateTime.UtcNow;
                        break;

                    default:
                        throw new InvalidOperationException("Shipper can only update order to shipping or delivered.");
                }

                _orderRepo.Update(order);
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

            if (response.Status == OrderStatus.Shipping)
            {
                await NotifyOrderUserAsync(
                    response,
                    NotificationType.OrderShipping,
                    response.SellerId,
                    response.BuyerId,
                    "Order is shipping",
                    $"Your order {response.OrderCode} is now shipping.");
            }
            else if (response.Status == OrderStatus.Delivered)
            {
                await NotifyOrderUserAsync(
                    response,
                    NotificationType.OrderDelivered,
                    response.SellerId,
                    response.BuyerId,
                    "Order delivered",
                    $"Your order {response.OrderCode} has been delivered. Please confirm it if everything is okay.");
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
            {
                ValidateRefundImage(request.ProofImage2);
            }

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
            {
                proofImage2Url = await _cloudStorageService.UploadImageAsync(request.ProofImage2);
            }

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
                ItemImage = GetRefundItemImage(r),
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ProcessedAt = r.ProcessedAt
            }).ToList();
        }

        public async Task<OrderResponse> RejectRefundAsync(int orderId, string adminNote)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found.");

            var refundRequest = await _refundRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Refund request not found.");

            if (refundRequest.Status != "PENDING")
                throw new InvalidOperationException("Refund request already processed.");

            if (order.Status != OrderStatus.Refunding)
                throw new InvalidOperationException("Order is not in refunding status.");


            await _unitOfWork.BeginTransactionAsync();

            try
            {
                refundRequest.Status = "REJECTED";
                refundRequest.AdminNote = adminNote;
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                order.Status = OrderStatus.Delivered;

                await CompleteOrderAndReleaseEscrowAsync(
                    order,
                    order.BuyerId,
                    isSystemAction: true);

                _orderRepo.Update(order);

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

            var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                ?? throw new KeyNotFoundException("Buyer wallet not found.");

            var escrow = await _escrowRepo.GetByOrderIdAsync(orderId)
                ?? throw new KeyNotFoundException("Escrow session not found.");

            if (escrow.Status != EscrowStatus.Held)
                throw new InvalidOperationException("Escrow is not in a valid held state.");

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

                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ItemVariantId.HasValue)
                        continue;

                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                    if (variant != null)
                    {
                        _variantRepo.Restock(variant, detail.Quantity);
                    }
                }

                refundRequest.Status = "APPROVED";
                refundRequest.ProcessedAt = DateTime.UtcNow;
                _refundRepo.Update(refundRequest);

                order.Status = OrderStatus.Refunded;
                order.UpdatedAt = DateTime.UtcNow;
                _orderRepo.Update(order);

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
                    Description = $"Refund for returned order {order.OrderCode}",
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

            var updatedOrder = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new KeyNotFoundException("Order not found after refund approval.");

            var response = MapToResponse(updatedOrder);

            await NotifyOrder(response);
            await NotifyOrderEventAsync(response, NotificationType.RefundApproved, response.SellerId);

            return response;
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
                await CompleteOrderAndReleaseEscrowAsync(
                    order,
                    order.BuyerId,
                    isSystemAction: true);

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
                    if (!detail.ItemVariantId.HasValue)
                        continue;

                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                    if (variant != null)
                    {
                        _variantRepo.ReleaseReservedStock(variant, detail.Quantity);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                order.CancelReason = "Order was automatically cancelled because payment was not completed within 30 minutes.";

                _orderRepo.Update(order);

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

        public async Task<PagedResultDto<OrderResponse>> GetMyPurchasesFilteredAsync(
            int buyerId,
            OrderFilterRequest request)
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

            var result = await _orderRepo.GetOrdersByBuyerIdFilteredAsync(
                buyerId,
                page,
                pageSize,
                status,
                request.FromDate,
                request.ToDate,
                request.SellerName,
                request.OrderCode);

            return new PagedResultDto<OrderResponse>
            {
                Items = result.Orders.Select(MapToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                HasMore = page * pageSize < result.TotalCount
            };
        }

        public async Task<PagedResultDto<OrderResponse>> GetMySalesFilteredAsync(
            int sellerId,
            OrderFilterRequest request)
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

            var result = await _orderRepo.GetOrdersBySellerIdFilteredAsync(
                sellerId,
                page,
                pageSize,
                status,
                request.FromDate,
                request.ToDate,
                request.SellerName,
                request.OrderCode);

            return new PagedResultDto<OrderResponse>
            {
                Items = result.Orders.Select(MapToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = result.TotalCount,
                HasMore = page * pageSize < result.TotalCount
            };
        }

        private void MarkShipping(Order order, int currentUserId)
        {
            if (order.SellerId != currentUserId)
                throw new UnauthorizedAccessException("Only the seller can confirm shipment.");

            if (order.Status != OrderStatus.Processing)
                throw new InvalidOperationException("The order is not in a shippable state.");

            order.Status = OrderStatus.Shipping;
        }

        private async Task CompleteOrderAndReleaseEscrowAsync(
        Order order,
        int actorId,
        bool isSystemAction = false)
        {
            if (!isSystemAction && order.BuyerId != actorId)
                throw new UnauthorizedAccessException("Only the buyer can complete this order.");

            if (order.Status != OrderStatus.Delivered)
                throw new InvalidOperationException("Only delivered orders can be completed.");

            // 1. Lấy thông tin ví của các bên liên quan
            var sellerWallet = await _walletRepo.GetByAccountIdAsync(order.SellerId)
                ?? throw new KeyNotFoundException("Seller wallet not found.");

            var adminWallet = await _walletRepo.GetByAccountIdAsync(1) // Admin AccountId = 1
                ?? throw new KeyNotFoundException("Admin wallet not found.");

            var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
            if (escrow == null)
                throw new KeyNotFoundException("Escrow session not found for this order.");

            if (escrow.Status != EscrowStatus.Held)
                throw new InvalidOperationException("Escrow is not in a valid held state.");

            // 2. Tính toán dòng tiền
            decimal sellerBefore = sellerWallet.Balance;
            decimal sellerReceiveAmount = order.TotalAmount - order.ServiceFee;

            decimal adminBefore = adminWallet.Balance;
            decimal adminServiceFee = order.ServiceFee;

            if (sellerReceiveAmount <= 0)
                throw new InvalidOperationException("Invalid seller payout amount.");

            // 3. Cập nhật số dư ví Seller
            sellerWallet.Balance += sellerReceiveAmount;
            sellerWallet.UpdatedAt = DateTime.UtcNow;
            _walletRepo.Update(sellerWallet);

            // 4. Cập nhật số dư ví Admin (Thu phí hệ thống)
            if (adminServiceFee > 0)
            {
                adminWallet.Balance += adminServiceFee;
                adminWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(adminWallet);
            }

            // 5. Cập nhật trạng thái của Escrow
            escrow.Status = EscrowStatus.Released;
            escrow.ResolvedAt = DateTime.UtcNow;
            _escrowRepo.Update(escrow);

            // 6. Cập nhật trạng thái của Đơn hàng
            order.Status = OrderStatus.Completed;
            order.CompletedAt ??= DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            // 7. Lưu Transaction cho Seller
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
                Description = isSystemAction
                    ? $"Auto release payment for order {order.OrderCode}"
                    : $"Receive payment from order {order.OrderCode}",
                CreatedAt = DateTime.UtcNow,
                Status = TransactionStatus.Success
            });

            // 8. Lưu Transaction thu phí dịch vụ cho Admin (Id = 1)
            if (adminServiceFee > 0)
            {
                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = adminWallet.WalletId,
                    PaymentId = null,
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
            if (order.BuyerId != currentUserId && order.SellerId != currentUserId)
                throw new UnauthorizedAccessException("You are not allowed to cancel this order.");

            // TRƯỜNG HỢP 1: Hủy khi đơn chưa thanh toán
            if (order.Status == OrderStatus.PendingPayment)
            {
                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ItemVariantId.HasValue)
                        continue;

                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                    if (variant != null)
                    {
                        _variantRepo.ReleaseReservedStock(variant, detail.Quantity);
                    }
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                return;
            }

            // TRƯỜNG HỢP 2: Hủy khi đơn đã thanh toán & đang xử lý (Processing)
            if (order.Status == OrderStatus.Processing)
            {
                var buyerWallet = await _walletRepo.GetByAccountIdAsync(order.BuyerId)
                    ?? throw new KeyNotFoundException("Buyer wallet not found.");

                var escrow = order.EscrowSession ?? await _escrowRepo.GetByOrderIdAsync(order.OrderId);
                if (escrow == null)
                    throw new KeyNotFoundException("Escrow session not found.");

                if (escrow.Status != EscrowStatus.Held)
                    throw new InvalidOperationException("Escrow is not in a valid held state.");

                decimal buyerBefore = buyerWallet.Balance;

                // Hoàn tiền gốc + phí dịch vụ về lại cho người mua
                buyerWallet.Balance += order.TotalAmount;
                buyerWallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(buyerWallet);

                escrow.Status = EscrowStatus.Refunded;
                escrow.ResolvedAt = DateTime.UtcNow;
                _escrowRepo.Update(escrow);

                foreach (var detail in order.OrderDetails)
                {
                    if (!detail.ItemVariantId.HasValue)
                        continue;

                    var variant = await _variantRepo.GetByIdForUpdateAsync(detail.ItemVariantId.Value);
                    if (variant != null)
                    {
                        _variantRepo.Restock(variant, detail.Quantity);
                    }
                }

                // Lưu Transaction hoàn tiền cho Buyer
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
                    Description = $"Refund for order {order.OrderCode}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTime.UtcNow;
                return;
            }

            throw new InvalidOperationException("This order can no longer be cancelled.");
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
                wallet.WalletId,
                now.Month,
                now.Year);

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

        private OrderResponse MapToResponse(Order order)
        {
            return new OrderResponse
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
                PaidAt = order.PaidAt,
                DeliveredAt = order.DeliveredAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
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
                        : d.Item?.Images
                            .OrderBy(i => i.CreatedAt)
                            .Select(i => i.ImageUrl)
                            .FirstOrDefault()
                }).ToList()
            };
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
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        private static string GenerateOrderCode()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }

        private static string? GetRefundItemImage(RefundRequest refundRequest)
        {
            var firstDetail = refundRequest.Order.OrderDetails.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstDetail?.ImageUrlSnapshot))
            {
                return firstDetail.ImageUrlSnapshot;
            }

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

        private async Task NotifyOrderEventAsync(
            OrderResponse order,
            string type,
            int actorId)
        {
            switch (type)
            {
                case NotificationType.OrderCreated:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.SellerId,
                        "New order received",
                        $"You have received a new order {order.OrderCode} from {order.BuyerName}.");
                    break;

                case NotificationType.OrderPaid:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.SellerId,
                        "Order paid",
                        $"Order {order.OrderCode} has been paid by {order.BuyerName}.");
                    break;

                case NotificationType.OrderShipping:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.BuyerId,
                        "Order is shipping",
                        $"Your order {order.OrderCode} is now shipping.");
                    break;

                case NotificationType.OrderDelivered:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.BuyerId,
                        "Order delivered",
                        $"Your order {order.OrderCode} has been delivered. Please confirm it if everything is okay.");
                    break;

                case NotificationType.OrderCompleted:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.SellerId,
                        "Order completed",
                        $"Order {order.OrderCode} has been completed. Payment has been released to your wallet.");

                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.BuyerId,
                        "Order completed",
                        $"Your order {order.OrderCode} has been completed.");
                    break;

                case NotificationType.OrderCancelled:
                    var targetId = actorId == order.BuyerId
                        ? order.SellerId
                        : order.BuyerId;

                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        targetId,
                        "Order cancelled",
                        $"Order {order.OrderCode} has been cancelled.");
                    break;

                case NotificationType.RefundRequested:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.SellerId,
                        "Refund requested",
                        $"{order.BuyerName} requested a refund for order {order.OrderCode}.");
                    break;

                case NotificationType.RefundApproved:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.BuyerId,
                        "Refund approved",
                        $"Your refund request for order {order.OrderCode} has been approved.");

                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.SellerId,
                        "Order refunded",
                        $"Order {order.OrderCode} has been refunded to the buyer.");
                    break;

                case NotificationType.RefundRejected:
                    await NotifyOrderUserAsync(
                        order,
                        type,
                        actorId,
                        order.BuyerId,
                        "Refund rejected",
                        $"Your refund request for order {order.OrderCode} has been rejected.");
                    break;
            }
        }
    }
}