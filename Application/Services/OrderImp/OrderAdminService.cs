using Application.Response.OrderResp;
using Domain.Constants;
using Domain.Interfaces;
using Google.Apis.Drive.v3.Data;
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

        // Dependency Injection nhận các Repository thay vì DbContext
        public OrderAdminService(
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            ITransactionRepository transactionRepository)
        {
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<OrderAdminListPagedResponse> GetAllOrdersAsync(int pageNumber, int pageSize, string? status = null, string? search = null)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

            // Gọi xuống tầng Repository và truyền thêm biến search
            var (orders, totalCount) = await _orderRepository.GetPagedOrdersForAdminAsync(pageNumber, pageSize, status, search);

            // Mapping sang DTO tại tầng Service
            var orderResponses = orders.Select(o => new OrderAdminResponse
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode ?? $"ORD-{o.OrderId}",
                BuyerName = o.ReceiverName ?? o.Buyer.UserName ?? "N/A",
                SellerName = o.Seller.UserName ?? "N/A",
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
            // 1. Lấy thông tin đơn hàng qua Repository
            var order = await _orderRepository.GetOrderWithDetailsByCodeAsync(orderCode);
            if (order == null)
            {
                throw new Exception($"Không tìm thấy đơn hàng tương ứng với mã cung cấp: {orderCode}");
            }

            // 2. Lấy thông tin thanh toán & giao dịch qua các Repository tương ứng
            var payment = await _paymentRepository.GetPaymentByOrderCodeAsync(order.OrderCode ?? string.Empty);
            var orderTransactions = await _transactionRepository.GetAllTransactionsByOrderIdAsync(order.OrderId);

            // 3. Xử lý logic dòng thời gian (History)
            var historyList = new List<HistoryDto>
            {
                new HistoryDto { Status = "Order Placed", Time = order.CreatedAt.ToString("MMM dd, HH:mm") }
            };

            if (order.PaidAt.HasValue)
                historyList.Add(new HistoryDto { Status = "Payment Verified", Time = order.PaidAt.Value.ToString("MMM dd, HH:mm") });

            if (order.DeliveredAt.HasValue)
                historyList.Add(new HistoryDto { Status = "Shipped & Handed over to Carrier", Time = order.DeliveredAt.Value.ToString("MMM dd, HH:mm") });

            if (order.CompletedAt.HasValue)
                historyList.Add(new HistoryDto { Status = "Delivered Successfully (Completed)", Time = order.CompletedAt.Value.ToString("MMM dd, HH:mm") });
            else if (order.CancelledAt.HasValue)
                historyList.Add(new HistoryDto { Status = $"Order Cancelled (Reason: {order.CancelReason ?? "N/A"})", Time = order.CancelledAt.Value.ToString("MMM dd, HH:mm") });

            // 4. Map Stepper UI
            int currentStatusStep = order.Status.ToUpperInvariant() switch
            {
                OrderStatus.PendingPayment => 0,

                OrderStatus.Processing => 1,

                OrderStatus.Shipping => 2,

                OrderStatus.Delivered => 3,
                OrderStatus.Completed => 4,

                OrderStatus.Cancelled => -1,

                OrderStatus.Refunding => 5,
                OrderStatus.Refunded => 6,

                _ => 0
            };

            // 5. Kiểm toán tài chính biến động số dư
            // 5. Kiểm toán tài chính biến động số dư (Dữ liệu đã đầy đủ quan hệ Wallet -> Account)
            var auditDto = new FinancialAuditDto();

            if (orderTransactions != null && orderTransactions.Any())
            {
                // Sắp xếp theo thời gian: Giao dịch nào xảy ra trước (trả tiền) hiện trước, chia tiền hiện sau
                var sortedTransactions = orderTransactions.OrderBy(t => t.CreatedAt).ToList();

                foreach (var txn in sortedTransactions)
                {
                    var logItem = new WalletLogDto
                    {
                        Amount = txn.Amount,
                        WalletBefore = txn.BalanceBefore,
                        WalletAfter = txn.BalanceAfter,
                        ActionType = txn.Type, // "Debit" hoặc "Credit"
                        Timestamp = txn.CreatedAt.ToString("MMM dd, HH:mm:ss")
                    };

                    if (txn.Wallet?.Account != null)
                    {
                        bool isAdmin = string.Equals(txn.Wallet.Account.UserName, "admin", StringComparison.OrdinalIgnoreCase);
                        bool isSeller = txn.Wallet.AccountId == order.SellerId;

                        string buyerName = order.ReceiverName ?? order.Buyer?.UserName ?? "Customer";
                        string sellerName = order.Seller?.UserName ?? "Merchant";

                        // Lấy mã code rút gọn của transaction để hiển thị cho đẹp
                        string txnCode = txn.TransactionCode ?? "N/A";

                        if (isAdmin)
                        {
                            logItem.ActorType = "Platform";
                            logItem.ActorName = "Platform Admin";

                            if (string.Equals(txn.ReferenceType, "OrderRefund", StringComparison.OrdinalIgnoreCase))
                            {
                                logItem.Description = $"Platform service fee reversal — Refunded to Seller ({sellerName})";
                            }
                            else // Trường hợp thực tế: "OrderPayment" và Admin nhận "Credit"
                            {
                                logItem.Description = $"Platform service fee collection — Earned from order settlement";
                            }
                        }
                        else if (isSeller)
                        {
                            logItem.ActorType = "Seller";
                            logItem.ActorName = sellerName;

                            if (string.Equals(txn.ReferenceType, "OrderRefund", StringComparison.OrdinalIgnoreCase))
                            {
                                logItem.Description = $"Payout clawback/reversal — Deducted from Seller for Buyer ({buyerName}) refund";
                            }
                            else // Trường hợp thực tế: "OrderPayment" và Seller nhận "Credit" (Tiền đã về ví công nhận)
                            {
                                logItem.Description = $"Order payout released — Successfully settled to Seller's wallet";
                            }
                        }
                        else // Đối tượng BUYER (Người mua)
                        {
                            logItem.ActorType = "Buyer";
                            logItem.ActorName = buyerName;

                            if (string.Equals(txn.ReferenceType, "OrderRefund", StringComparison.OrdinalIgnoreCase))
                            {
                                logItem.Description = $"Refund received successfully — Credited back from Seller ({sellerName})";
                            }
                            else // Trường hợp thực tế: "OrderPayment" và Buyer bị "Debit"
                            {
                                logItem.Description = $"Wallet deduction for order payment — Transferred to Platform Escrow";
                            }
                        }
                    }
                    else
                    {
                        logItem.ActorType = "Unknown";
                        logItem.ActorName = "N/A";
                        logItem.Description = "System generated transaction";
                    }

                    auditDto.SettlementLogs.Add(logItem);
                }
            }

            // 6. Trả về cấu trúc DTO thành phẩm
            return new OrderAdminDetailResponse
            {
                Id = order.OrderCode ?? $"ORD-{order.OrderId}",
                TransactionId = payment?.ExternalTransactionId
                ?? orderTransactions?.FirstOrDefault(t => string.Equals(t.Type, Domain.Constants.TransactionType.Debit, StringComparison.OrdinalIgnoreCase))?.TransactionId.ToString()
                ?? "N/A",
                PlacedAt = order.CreatedAt.ToString("MMM dd, yyyy - HH:mm:ss (UTC)"),
                CurrentStatus = currentStatusStep,
                Buyer = new BuyerDto
                {
                    Id = $"{order.BuyerId}",
                    Name = order.ReceiverName ?? order.Buyer.UserName ?? "N/A",
                    Email = order.Buyer.Email ?? "N/A",
                    Phone = order.ReceiverPhone ?? order.Buyer.PhoneNumber ?? "N/A",
                    Address = order.ShippingAddress ?? "No shipping address provided"
                },
                Seller = new SellerDto
                {
                    Id = $"{order.SellerId}",
                    Name = order.Seller.UserName ?? "TechGear Store",
                    Warehouse = "California-WH04"
                },
                Payment = new PaymentDto
                {
                    Method = payment?.Provider ?? "Wallet/Credit Card",
                    Status = payment?.Status ?? (
                        order.PaidAt.HasValue
                            ? "Success"
                            : order.Status.Equals(OrderStatus.Cancelled, StringComparison.OrdinalIgnoreCase)
                                ? "Expired"
                                : "Pending"
                    ),
                    Gateway = payment?.Provider == "Stripe" ? "Stripe API v3" : "Internal Wallet System"
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
                    string displayItemName = d.ItemNameSnapshot;
                    if (!string.IsNullOrEmpty(d.VariantSnapshot))
                    {
                        displayItemName += $" ({d.VariantSnapshot})";
                    }
                    else if (d.ItemVariant != null)
                    {
                        var attrs = new List<string>();
                        if (!string.IsNullOrEmpty(d.ItemVariant.SizeCode)) attrs.Add($"Size: {d.ItemVariant.SizeCode}");
                        if (!string.IsNullOrEmpty(d.ItemVariant.Color)) attrs.Add($"Màu: {d.ItemVariant.Color}");
                        if (attrs.Count > 0) displayItemName += $" ({string.Join(", ", attrs)})";
                    }

                    string? finalImageUrl = d.ImageUrlSnapshot;

                    if (string.IsNullOrEmpty(finalImageUrl) && d.Item?.Images != null)
                    {
                        var firstImage = d.Item.Images.FirstOrDefault();
                        if (firstImage != null)
                        {
                            finalImageUrl = firstImage.ImageUrl;
                        }
                    }

                    var statusItem = order.Status.ToUpperInvariant() switch
                    {
                        OrderStatus.Completed => "Completed",
                        OrderStatus.Delivered => "Delivered",

                        OrderStatus.Shipping => "Shipping",

                        OrderStatus.Cancelled => "Cancelled",

                        OrderStatus.Processing => "Processing",

                        OrderStatus.PendingPayment => "Pending Payment",

                        OrderStatus.Refunding => "Refunding",

                        OrderStatus.Refunded => "Refunded",

                        _ => "Unknown"
                    };

                    return new ItemDto
                    {
                        Id = d.OrderDetailId,
                        Name = displayItemName,
                        Price = d.UnitPrice,
                        Qty = d.Quantity,
                        Sku = d.SkuSnapshot ?? d.ItemVariant?.Sku ?? "N/A",
                        Status = statusItem,
                        Condition = d.Item?.Condition ?? "New",
                        ImageUrl = finalImageUrl
                    };
                }).ToList(),
                History = historyList
            };
        }
    }
}