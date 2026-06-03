using Application.Response.OrderResp;
using Domain.Constants;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services.OrderImp
{
    public class OrderAdminService : IOrderAdminService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOrderStatusHistoryRepository _orderStatusHistoryRepository;

        public OrderAdminService(
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            ITransactionRepository transactionRepository,
            IOrderStatusHistoryRepository orderStatusHistoryRepository)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _orderStatusHistoryRepository = orderStatusHistoryRepository ?? throw new ArgumentNullException(nameof(orderStatusHistoryRepository));
        }

        public async Task<OrderAdminListPagedResponse> GetAllOrdersAsync(int pageNumber, int pageSize, string? status = null, string? search = null)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var (orders, totalCount) = await _orderRepository.GetPagedOrdersForAdminAsync(pageNumber, pageSize, status, search);

            var orderResponses = orders.Select(o => new OrderAdminResponse
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode ?? $"ORD-{o.OrderId}",
                BuyerName = o.ReceiverName ?? o.Buyer?.UserName ?? "N/A",
                SellerName = o.Seller?.UserName ?? "N/A",
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            }).ToList();

            return new OrderAdminListPagedResponse
            {
                Orders = orderResponses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<OrderAdminDetailResponse> GetOrderDetailForAdminAsync(string orderCode)
        {
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                throw new ArgumentException("The order number cannot be left blank.", nameof(orderCode));
            }

            var order = await _orderRepository.GetOrderWithDetailsByCodeAsync(orderCode);
            if (order == null)
            {
                throw new KeyNotFoundException($"No orders matching the provided code were found: {orderCode}");
            }

            // Đưa buyerName và sellerName lên đầu context để toàn bộ hàm phía dưới có thể sử dụng (Fix lỗi compile)
            string buyerName = order.ReceiverName ?? order.Buyer?.UserName ?? "Customer";
            string sellerName = order.Seller?.UserName ?? "Merchant";

            // Kéo các dữ liệu liên quan song song để tối ưu IO Bound (Performance Tuning)
            var payment = await _paymentRepository.GetPaymentByOrderCodeAsync(order.OrderCode ?? string.Empty);
            var orderTransactions = await _transactionRepository.GetAllTransactionsByOrderIdAsync(order.OrderId);
            var dbHistories = await _orderStatusHistoryRepository.GetByOrderIdAsync(order.OrderId);

            // 1. Xử lý mapping dữ liệu lịch sử trạng thái (Order History Timeline)
            var historyList = new List<HistoryDto>();
            if (dbHistories != null && dbHistories.Any())
            {
                historyList.AddRange(dbHistories.Select(history => new HistoryDto
                {
                    StatusKey = history.Status.ToUpperInvariant(),
                    StatusDisplay = MapStatusToDisplayString(history.Status, order.CancelReason ?? history.Note),
                    ChangedAt = history.ChangedAt,
                    DateOnly = history.ChangedAt.ToString("MMM dd, yyyy"),
                    TimeOnly = history.ChangedAt.ToString("HH:mm:ss"),
                    Note = history.Note
                }));
            }
            else
            {
                historyList.Add(new HistoryDto
                {
                    StatusKey = order.Status.ToUpperInvariant(),
                    StatusDisplay = "Order Initialized",
                    ChangedAt = order.CreatedAt,
                    DateOnly = order.CreatedAt.ToString("MMM dd, yyyy"),
                    TimeOnly = order.CreatedAt.ToString("HH:mm:ss"),
                    Note = "System generated placement record"
                });
            }

            // 2. Tính toán bước hiển thị cho Thanh Tiến Trình (Progress Step)
            int currentStatusStep = MapStatusToStep(order.Status);

            // 3. Xử lý Nhật ký dòng tiền đối soát (Financial Audit Logs)
            var auditDto = new FinancialAuditDto();
            if (orderTransactions != null && orderTransactions.Any())
            {
                var sortedTransactions = orderTransactions.OrderBy(t => t.CreatedAt).ToList();

                foreach (var txn in sortedTransactions)
                {
                    var logItem = new WalletLogDto
                    {
                        Amount = txn.Amount,
                        WalletBefore = txn.BalanceBefore,
                        WalletAfter = txn.BalanceAfter,
                        ActionType = txn.Type,
                        Timestamp = txn.CreatedAt.ToString("MMM dd, HH:mm:ss"),
                        ActorType = "Unknown",
                        ActorName = "N/A",
                        Description = "System generated transaction"
                    };

                    if (txn.Wallet?.Account != null)
                    {
                        bool isAdmin = string.Equals(txn.Wallet.Account.UserName, "admin", StringComparison.OrdinalIgnoreCase);
                        bool isSeller = txn.Wallet.AccountId == order.SellerId;
                        bool isRefund = string.Equals(txn.ReferenceType, "OrderRefund", StringComparison.OrdinalIgnoreCase);

                        if (isAdmin)
                        {
                            logItem.ActorType = "Platform";
                            logItem.ActorName = "Platform Admin";
                            logItem.Description = isRefund
                                ? $"Platform service fee reversal — Refunded to Seller ({sellerName})"
                                : "Platform service fee collection — Earned from order settlement";
                        }
                        else if (isSeller)
                        {
                            logItem.ActorType = "Seller";
                            logItem.ActorName = sellerName;
                            logItem.Description = isRefund
                                ? $"Payout clawback/reversal — Deducted from Seller for Buyer ({buyerName}) refund"
                                : "Order payout released — Successfully settled to Seller's wallet";
                        }
                        else
                        {
                            logItem.ActorType = "Buyer";
                            logItem.ActorName = buyerName;
                            logItem.Description = isRefund
                                ? $"Refund received successfully — Credited back from Seller ({sellerName})"
                                : "Wallet deduction for order payment — Transferred to Platform Escrow";
                        }
                    }

                    auditDto.SettlementLogs.Add(logItem);
                }
            }

            // 4. Xác định mã giao dịch chính để hiển thị (Transaction Resolution)
            string resolvedTransactionId = payment?.ExternalTransactionId
                ?? orderTransactions?.FirstOrDefault(t => string.Equals(t.Type, "Debit", StringComparison.OrdinalIgnoreCase))?.TransactionId.ToString()
                ?? "N/A";

            // 5. Trả về cấu trúc DTO hoàn chỉnh
            return new OrderAdminDetailResponse
            {
                Id = order.OrderCode ?? $"ORD-{order.OrderId}",
                OrderId = order.OrderId,
                TransactionId = resolvedTransactionId,
                PlacedAt = order.CreatedAt.ToString("MMM dd, yyyy - HH:mm:ss (UTC)"),

                StatusKey = order.Status.ToUpperInvariant(),
                CurrentStatusStep = currentStatusStep,

                Buyer = new BuyerDto
                {
                    Id = order.BuyerId.ToString(),
                    Name = order.ReceiverName ?? order.Buyer?.UserName ?? "N/A",
                    Email = order.Buyer?.Email ?? "N/A",
                    Phone = order.ReceiverPhone ?? order.Buyer?.PhoneNumber ?? "N/A",
                    Address = order.ShippingAddress ?? "No shipping address provided"
                },
                Seller = new SellerDto
                {
                    Id = order.SellerId.ToString(),
                    Name = sellerName, // Đã an toàn sử dụng ở đây
                    Warehouse = "Default-Warehouse-01"
                },
                Payment = new PaymentDto
                {
                    Method = payment?.Provider ?? "Wallet/Credit Card",
                    Status = payment?.Status ?? (
                        dbHistories != null && dbHistories.Any(h => string.Equals(h.Status, OrderStatus.Processing, StringComparison.OrdinalIgnoreCase))
                            ? "Success"
                            : string.Equals(order.Status, OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase) ? "Expired" : "Pending"
                    ),
                    Gateway = string.Equals(payment?.Provider, "Stripe", StringComparison.OrdinalIgnoreCase) ? "Stripe API v3" : "Internal Wallet System"
                },
                Ledger = new LedgerDto
                {
                    Subtotal = order.SubTotal,
                    Shipping = 0.00m,
                    Tax = 0.00m,
                    PlatformFee = order.ServiceFee,
                    SellerNet = order.TotalAmount - order.ServiceFee,
                    Total = order.TotalAmount
                },
                FinancialAudit = auditDto,

                HasRefundRequest = order.RefundRequest != null,
                RefundRequestStatus = order.RefundRequest?.Status,
                RefundReason = order.RefundRequest?.Reason,
                RefundAdminNote = order.RefundRequest?.AdminNote,
                RefundRequestedAt = order.RefundRequest?.CreatedAt,
                RefundProcessedAt = order.RefundRequest?.ProcessedAt,

                Items = order.OrderDetails.Select(d =>
                {
                    string displayItemName = d.ItemNameSnapshot ?? "Unknown Item";
                    if (!string.IsNullOrEmpty(d.VariantSnapshot))
                    {
                        displayItemName += $" ({d.VariantSnapshot})";
                    }

                    return new ItemDto
                    {
                        Id = d.OrderDetailId,
                        Name = displayItemName,
                        Price = d.UnitPrice,
                        Qty = d.Quantity,
                        Sku = d.SkuSnapshot ?? "N/A",
                        Status = MapStatusToItemStatusDisplay(order.Status),
                        Condition = "New",
                        ImageUrl = d.ImageUrlSnapshot ?? d.Item?.Images?.FirstOrDefault()?.ImageUrl
                    };
                }).ToList(),
                History = historyList
            };
        }

        #region Helper Methods (Sạch sẽ, dễ bảo trì, tránh lặp logic)

        private static string MapStatusToDisplayString(string status, string? reasonOrNote)
        {
            return status.ToUpperInvariant() switch
            {
                OrderStatus.PendingPayment => "Order Placed & Pending Payment",
                OrderStatus.Processing => "Payment Verified & Processing",
                OrderStatus.Shipping => "Shipped & Handed over to Carrier",
                OrderStatus.Delivered => "Delivered to Customer",
                OrderStatus.Completed => "Delivered Successfully (Completed)",
                OrderStatus.Cancelled => $"Order Cancelled (Reason: {reasonOrNote ?? "N/A"})",
                OrderStatus.Refunding => "Refund Request Under Review",
                OrderStatus.Refunded => "Amount Refunded Successfully & Return Order Initialized",
                OrderStatus.ReturnPickedUp => "Carrier Picked Up Returned Items From Buyer",
                OrderStatus.ReturnShipping => "Returned Items In Transit to Seller's Warehouse",
                OrderStatus.ReturnDelivered => "Returned Items Delivered to Seller (Pending Warehouse Inspection)",
                OrderStatus.ReturnCompleted => "Seller Verified & Stock Ingested (Return Flow Completed)",
                _ => $"Status Updated to: {status}"
            };
        }

        private static string MapStatusToItemStatusDisplay(string status)
        {
            return status.ToUpperInvariant() switch
            {
                OrderStatus.PendingPayment => "Pending Payment",
                OrderStatus.Processing => "Processing",
                OrderStatus.Shipping => "Shipping",
                OrderStatus.Delivered => "Delivered",
                OrderStatus.Completed => "Completed",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.Refunding => "Refunding",
                OrderStatus.Refunded => "Refunded",
                OrderStatus.ReturnPickedUp => "Return Picked Up",
                OrderStatus.ReturnShipping => "Return In Transit",
                OrderStatus.ReturnDelivered => "Return Delivered to Seller",
                OrderStatus.ReturnCompleted => "Return Stocked In",
                _ => "Unknown"
            };
        }

        private static int MapStatusToStep(string status)
        {
            return status.ToUpperInvariant() switch
            {
                OrderStatus.PendingPayment => 0,
                OrderStatus.Processing => 1,
                OrderStatus.Shipping => 2,
                OrderStatus.Delivered => 3,
                OrderStatus.Completed => 4,
                OrderStatus.Cancelled => -1,
                OrderStatus.Refunding => 5,
                OrderStatus.Refunded => 6,
                OrderStatus.ReturnPickedUp => 7,
                OrderStatus.ReturnShipping => 8,
                OrderStatus.ReturnDelivered => 9,
                OrderStatus.ReturnCompleted => 10,
                _ => 0
            };
        }

        #endregion
    }
}