using Application.Interfaces;
using Application.Request.NotificationReq;
using Application.Response.EscrowSessionResp;
using Application.Response.TransactionResp;
using Application.Services.NotificationImp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Globalization;

namespace Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IEscrowSessionRepository _escrowRepository;
        private readonly INotificationService _notificationService;
        private readonly IWalletRepository _walletRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEscrowStatusHistoryRepository _escrowStatusHistoryRepository;
        private readonly IOrderRepository _orderRepository;


        public TransactionService(
            ITransactionRepository transactionRepository,
            IEscrowSessionRepository escrowRepository,
            INotificationService notificationService,
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IEscrowStatusHistoryRepository escrowStatusHistoryRepository,
            IOrderRepository orderRepository)
        {
            _transactionRepository = transactionRepository;
            _escrowRepository = escrowRepository;
            _notificationService = notificationService;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _escrowStatusHistoryRepository = escrowStatusHistoryRepository;
            _orderRepository = orderRepository;
        }

        public async Task AdminRequestFixLeakAsync(int escrowSessionId, string reason)
        {
            var escrow = await _escrowRepository.GetByIdAsync(escrowSessionId, e => e.Event!);
            if (escrow == null) throw new Exception("No Escrow session found.");

            escrow.Status = "PendingFix";
            escrow.Description = $"Admin Request Fix: {reason}";

            _escrowRepository.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                SenderId = 1, // System ID
                TargetUserId = escrow.SenderId,
                Title = "Request to Approve Cash Shortage Handling",
                Content = $"The transaction at the event '{escrow.Event?.Title}' is awaiting your approval for Admin to handle the technical issue.",
                Type = "ExpertApprovalNeeded",
                RelatedId = escrowSessionId.ToString()
            });
        }

        // HÀM 2: Expert duyệt yêu cầu của Admin
        public async Task ExpertApproveFixAsync(int escrowSessionId)
        {
            var escrow = await _escrowRepository.GetByIdAsync(escrowSessionId);
            if (escrow == null) throw new Exception("No holding session found");

            escrow.Status = "ExpertApproved";

            _escrowRepository.Update(escrow);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(new SendNotificationRequest
            {
                SenderId = escrow.SenderId,
                Title = "Expert đã phê duyệt",
                Content = $"Stuck transaction #{escrowSessionId} has been approved by the Expert for Admin processing.",
                Type = "AdminTaskNotify"
            });
        }

        // HÀM 3: Admin thực hiện cập nhật tiền sau khi đã có Expert duyệt
        public async Task AdminExecuteUpdateWalletAsync(int escrowSessionId)
        {
            var escrow = await _escrowRepository.GetByIdAsync(escrowSessionId);
            if (escrow == null || escrow.Status != "ExpertApproved")
                throw new Exception("The transaction has not been approved by the Expert or is invalid.");

            if (escrow.ReceiverId.HasValue)
            {
                var wallet = await _walletRepository.GetByAccountIdAsync(escrow.ReceiverId.Value);
                decimal oldBalance = wallet.Balance;
                wallet.Balance += escrow.FinalAmount;

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = escrow.FinalAmount,
                    BalanceBefore = oldBalance,
                    BalanceAfter = wallet.Balance,
                    Type = "Credit",
                    ReferenceType = "EventFix",
                    ReferenceId = escrow.EventId,
                    TransactionCode = "FIX_" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    Description = "Admin handles cash shortage after Expert approval.",
                    Status = "Success",
                    CreatedAt = DateTime.Now
                };

                await _transactionRepository.AddAsync(transaction);
                _walletRepository.Update(wallet);
                await _unitOfWork.SaveChangesAsync();

            }

            escrow.Status = "Completed";
            escrow.ResolvedAt = DateTime.Now;

            _escrowRepository.Update(escrow);
            await _unitOfWork.SaveChangesAsync();
        }

        // HÀM 4: Get thông tin bảng Escrow cho Admin quản lý
        public async Task<List<EscrowResponse>> AdminGetEscrowManagementAsync()
        {
            var rawEscrows = await _escrowRepository.Query()
                .Include(e => e.Sender)
                .Include(e => e.Receiver)
                .Include(e => e.Event)
                .Include(e => e.Order)
                    .ThenInclude(o => o.Seller)
                .Include(e => e.EscrowStatusHistories)
                    .ThenInclude(h => h.ChangedBy)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var response = rawEscrows.Select(e =>
            {
                decimal baseAmount = e.Amount;
                if (baseAmount <= 0 && e.EscrowStatusHistories != null && e.EscrowStatusHistories.Any())
                {
                    baseAmount = e.EscrowStatusHistories.Max(h => h.AmountAfter);
                }

                decimal mappedServiceFee = 0;
                if (e.ServiceFee > 0) mappedServiceFee = e.ServiceFee;
                else if (e.Order != null) mappedServiceFee = e.Order.ServiceFee;
                else if (e.Event != null) mappedServiceFee = e.Event.AppliedFee;

                if (mappedServiceFee >= baseAmount)
                {
                    mappedServiceFee = 0;
                }

                decimal finalAmount = Math.Max(0, baseAmount - mappedServiceFee);

                return new EscrowResponse
                {
                    EscrowSessionId = e.EscrowSessionId,
                    EventId = e.EventId,
                    EventTitle = e.Event?.Title,
                    OrderId = e.OrderId,
                    OrderCode = e.Order?.OrderCode,

                    SenderId = e.SenderId,
                    SenderName = e.Sender?.UserName ?? "Unknown",

                    ReceiverId = e.ReceiverId,
                    ReceiverName = e.Receiver?.UserName
                                 ?? e.Order?.Seller?.UserName
                                 ?? (e.EventId != null ? "Event Intermediary Fund" : "System"),

                    Amount = baseAmount,
                    ServiceFee = mappedServiceFee,
                    FinalAmount = finalAmount,

                    Status = e.Status,
                    Description = e.Description,
                    CreatedAt = e.CreatedAt,
                    ResolvedAt = e.ResolvedAt,

                    StatusHistories = e.EscrowStatusHistories?
                        .OrderByDescending(h => h.ChangedAt)
                        .Select(h => new EscrowHistoryDto
                        {
                            EscrowStatusHistoryId = h.EscrowStatusHistoryId,
                            FromStatus = h.FromStatus,
                            ToStatus = h.ToStatus,
                            AmountBefore = h.AmountBefore,
                            AmountAfter = h.AmountAfter,
                            Reason = h.Reason,
                            ChangedByName = h.ChangedBy?.UserName ?? "System",
                            ChangedAt = h.ChangedAt
                        }).ToList() ?? new List<EscrowHistoryDto>()
                };
            }).ToList();

            return response;
        }

        public async Task<List<EscrowResponse>> ExpertGetEscrowManagementAsync()
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null) throw new Exception("User not authenticated.");

            var escrows = await _escrowRepository.GetEscrowsByUserIdAsync(currentUserId.Value);

            return escrows.Adapt<List<EscrowResponse>>();
        }

        // HÀM 5: Get all giao dịch cho Expert (kèm ReferenceId)
        public async Task<List<TransactionResponse>> ExpertGetHistoryAsync()
        {
            var currentUserId = _currentUserService.GetUserId();

            if (currentUserId == null) throw new Exception("User not authenticated.");

            var wallet = await _walletRepository.GetByAccountIdAsync(currentUserId.Value);

            if (wallet == null) throw new Exception("No wallet found for the current user.");

            var transactions = await _transactionRepository.GetByWalletIdAsync(wallet.WalletId);
            return transactions.Adapt<List<TransactionResponse>>();
        }

        // HÀM 6: Get chi tiết giao dịch theo ReferenceId (EventId) để kiểm soát
        public async Task<List<TransactionResponse>> GetTransactionsByReferenceAsync(string refType, int refId)
        {
            var transactions = await _transactionRepository.GetByReferenceAsync(refType, refId);
            return transactions.Adapt<List<TransactionResponse>>();
        }

        // HÀM ADMIN: Get All giao dịch với filter linh hoạt
        public async Task<List<TransactionResponseV2>> AdminGetAllTransactionsAsync(
    string? type = null,
    string? refType = null,
    int? refId = null)
        {
            // 1. Nạp giao dịch và dữ liệu liên kết cơ bản
            var transactions = await _transactionRepository.GetTransactionsAsync(
                type,
                refType,
                refId,
                t => t.Wallet,
                t => t.Wallet!.Account!,
                t => t.EscrowSession!,
                t => t.EscrowSession!.Sender!,
                t => t.EscrowSession!.Receiver!,
                t => t.EscrowSession!.Event!,
                t => t.EscrowSession!.Order!
            );

            // 2. Lấy danh sách EscrowSessionId có xuất hiện trong các giao dịch
            var escrowSessionIds = transactions
                .Where(t => t.EscrowSessionId.HasValue)
                .Select(t => t.EscrowSessionId!.Value)
                .Distinct()
                .ToList();

            // SỬ DỤNG REPOSITORY ĐÃ ĐƯỢC CHUYỂN XUỐNG TẠI ĐÂY
            List<EscrowStatusHistory> escrowHistories = new();
            if (escrowSessionIds.Any())
            {
                var historiesResult = await _escrowStatusHistoryRepository.GetByEscrowSessionIdsAsync(escrowSessionIds);
                escrowHistories = historiesResult.ToList();
            }

            // 3. Thu thập các ID quy chiếu để phục vụ đối soát Ví Admin (WalletId == 1)
            var eventIds = transactions.Where(t => t.ReferenceType == "Event" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var orderIds = transactions.Where(t => t.ReferenceType == "OrderPayment" || t.ReferenceType == "OrderRefund").Where(t => t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var tryOnIds = transactions.Where(t => t.ReferenceType == "TryOn" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var aiRecomIds = transactions.Where(t => (t.ReferenceType == "AIRecommendation" || t.ReferenceType == "AI_Recommendation") && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var allRefIds = eventIds.Concat(orderIds).Concat(tryOnIds).Concat(aiRecomIds).Distinct().ToList();

            var systemTransactions = await _transactionRepository.GetTransactionsAsync(type: null, refType: null, refId: null);
            var matchedSystemTxList = systemTransactions.Where(st => st.WalletId == 1 && st.ReferenceId.HasValue && allRefIds.Contains(st.ReferenceId.Value)).ToList();

            var chronologicalTransactions = transactions.OrderBy(t => t.CreatedAt).ToList();
            var computedResponsesMap = new Dictionary<int, TransactionResponseV2>();

            // 4. Duyệt tuyến tính dòng tiền để map dữ liệu
            foreach (var t in chronologicalTransactions)
            {
                var res = new TransactionResponseV2
                {
                    TransactionId = t.TransactionId,
                    TransactionCode = t.TransactionCode,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Type = t.Type,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Status = t.Status,
                    WalletId = t.WalletId,
                    UserName = t.WalletId == 1 ? "System (Total Fund)" : (t.Wallet?.Account?.UserName ?? "N/A"),
                    ReferenceType = t.ReferenceType,
                    ReferenceId = t.ReferenceId,
                    PaymentId = t.PaymentId
                };

                var matchedEscrow = t.EscrowSession;

                // --- A. LẤY SỐ TIỀN TRỰC TIẾP TỪ ESCROW STATUS HISTORY (ĐÃ FIX PHÂN LOẠI SONG SONG) ---
                if (matchedEscrow != null)
                {
                    // Bước 1: Lọc ứng viên trùng ID phiên và trùng khớp thời gian (< 3 giây)
                    var candidateHistories = escrowHistories
                        .Where(h => h.EscrowSessionId == matchedEscrow.EscrowSessionId &&
                                    Math.Abs((h.ChangedAt - t.CreatedAt).TotalSeconds) < 3)
                        .ToList();

                    EscrowStatusHistory? matchedHistory = null;

                    if (candidateHistories.Count > 1)
                    {
                        // Nhận diện ngữ cảnh giao dịch qua Description của Transaction
                        string txDesc = t.Description?.ToLower() ?? "";
                        bool isServiceFeeTx = txDesc.Contains("service fee") || txDesc.Contains("admin fee") || txDesc.Contains("fee");

                        // Khớp chính xác lịch sử dựa trên từ khóa nghiệp vụ (Reason) và số tiền thay đổi
                        matchedHistory = candidateHistories.FirstOrDefault(h =>
                        {
                            decimal diff = h.AmountAfter - h.AmountBefore;
                            bool isAmountMatch = (diff == -t.Amount || diff == t.Amount);
                            if (!isAmountMatch) return false;

                            string reason = h.Reason?.ToLower() ?? "";

                            if (isServiceFeeTx)
                            {
                                // Nếu giao dịch ví là THU PHÍ -> Ưu tiên chọn lịch sử có từ khóa fee/admin
                                return reason.Contains("fee") || reason.Contains("admin");
                            }
                            else
                            {
                                // Nếu giao dịch ví là TRẢ TIỀN SELLER -> Loại trừ các lịch sử chứa chữ fee/admin
                                return !reason.Contains("fee") && !reason.Contains("admin");
                            }
                        });
                    }

                    // Fallback 1: Nếu lọc nâng cao dựa trên bối cảnh không ra, quay về khớp số tiền thuần túy
                    if (matchedHistory == null && candidateHistories.Any())
                    {
                        matchedHistory = candidateHistories.FirstOrDefault(h =>
                            (h.AmountAfter - h.AmountBefore) == -t.Amount ||
                            (h.AmountAfter - h.AmountBefore) == t.Amount
                        );
                    }

                    // Fallback 2: Lấy dòng đầu tiên phù hợp thời gian
                    if (matchedHistory == null)
                    {
                        matchedHistory = candidateHistories.FirstOrDefault();
                    }

                    // Fallback 3: Lấy bản ghi cuối cùng của Session đó
                    if (matchedHistory == null)
                    {
                        matchedHistory = escrowHistories
                            .Where(h => h.EscrowSessionId == matchedEscrow.EscrowSessionId)
                            .OrderByDescending(h => h.ChangedAt)
                            .FirstOrDefault();
                    }

                    decimal escrowBefore = matchedHistory?.AmountBefore ?? 0m;
                    decimal escrowAfter = matchedHistory?.AmountAfter ?? 0m;
                    decimal amountChanged = escrowAfter - escrowBefore;

                    string action = matchedHistory != null ? $"{matchedHistory.FromStatus} -> {matchedHistory.ToStatus}" : "Unknown";
                    if (matchedHistory != null && !string.IsNullOrEmpty(matchedHistory.Reason))
                    {
                        action += $" ({matchedHistory.Reason})";
                    }

                    res.EscrowDetail = new EscrowBriefResponse
                    {
                        EscrowSessionId = matchedEscrow.EscrowSessionId,
                        Status = matchedEscrow.Status,
                        OriginalAmount = matchedEscrow.Amount,
                        SystemServiceFee = matchedEscrow.ServiceFee,
                        FinalPayoutAmount = matchedEscrow.FinalAmount,
                        EscrowAction = action,
                        EscrowAmountChanged = amountChanged,
                        EscrowBefore = escrowBefore,
                        EscrowAfter = escrowAfter,
                        SenderName = matchedEscrow.Sender?.UserName ?? "User",
                        ReceiverName = matchedEscrow.Receiver?.UserName ?? (t.TransactionCode.StartsWith("RW-") ? res.UserName : "System"),
                        LinkedTargetName = matchedEscrow.Event?.Title ?? (matchedEscrow.Order != null ? $"Order #{matchedEscrow.OrderId}" : "N/A")
                    };
                }

                // --- B. ĐỐI SOÁT VÍ ADMIN SƠ CẤP (Sửa lỗi hiển thị trùng lặp Snapshot) ---
                if (t.WalletId != 1)
                {
                    // Tìm các giao dịch ví hệ thống trùng Reference và khớp thời gian nhất (sai lệch < 2 giây)
                    Transaction? realSystemTx = matchedSystemTxList.FirstOrDefault(st =>
                        st.ReferenceType == t.ReferenceType &&
                        st.ReferenceId == t.ReferenceId &&
                        Math.Abs((st.CreatedAt - t.CreatedAt).TotalSeconds) < 2
                    );

                    // Nếu không khớp thời gian hoàn hảo, mới dùng logic nghiệp vụ cũ làm Fallback
                    if (realSystemTx == null)
                    {
                        realSystemTx = matchedSystemTxList.FirstOrDefault(st =>
                            st.ReferenceType == t.ReferenceType && st.ReferenceId == t.ReferenceId &&
                            (t.TransactionCode.StartsWith("PAY_FEE_") ? st.TransactionCode.StartsWith("REV_FEE_") : st.Type == t.Type)
                        );
                    }

                    if (realSystemTx != null)
                    {
                        res.SystemWalletSnapshot = new SystemWalletSnapshotResponse
                        {
                            PlatformAmountChanged = realSystemTx.Amount,
                            PlatformBalanceAfter = realSystemTx.BalanceAfter,
                            DataMode = "Real"
                        };
                    }
                }
                // --- C. ĐỐI SOÁT VÍ ADMIN VỚI USER ---
                else if (t.WalletId == 1)
                {
                    var userCounterpartTx = chronologicalTransactions.FirstOrDefault(ut =>
                        ut.WalletId != 1 &&
                        ut.ReferenceType == t.ReferenceType &&
                        ut.ReferenceId == t.ReferenceId &&
                        Math.Abs(ut.Amount) == Math.Abs(t.Amount) &&
                        Math.Abs((ut.CreatedAt - t.CreatedAt).TotalSeconds) < 2
                    );

                    if (userCounterpartTx != null)
                    {
                        res.CounterpartyUserId = userCounterpartTx.Wallet?.AccountId;
                        res.CounterpartyName = userCounterpartTx.Wallet?.Account?.UserName ?? "User";
                    }
                    else
                    {
                        res.CounterpartyUserId = null;
                        res.CounterpartyName = "External Source / Network";
                    }
                }

                computedResponsesMap[t.TransactionId] = res;
            }

            return transactions
                .Select(t => computedResponsesMap.ContainsKey(t.TransactionId) ? computedResponsesMap[t.TransactionId] : null!)
                .Where(res => res != null)
                .ToList();
        }

        public async Task<TransactionResponse?> GetById(int id)
        {
            var t = await _transactionRepository.GetByIdAsync(id);
            if (t == null) return null;

            return MapToResponse(t);
        }

        public async Task<List<TransactionResponse>> GetTransactions()
        {
            //var transactions = await _transactionRepository.GetTransactionsAsync();
            string? type = null;
            string? refType = null;
            int? refId = null;
            var transactions = await _transactionRepository.GetTransactionsAsync(
               type,
               refType,
               refId,
               t => t.Wallet,
               t => t.Wallet!.Account!
           );
            return transactions.Select(t => MapToResponse(t)).ToList();
        }

        public async Task<FeatureIntelligenceResponse> GetFeatureIntelligenceDashboardAsync(DateTime? customStartDate = null, DateTime? customEndDate = null)
        {
            var response = new FeatureIntelligenceResponse();

            // 1. XỬ LÝ ĐIỀU KIỆN THỜI GIAN MẶC ĐỊNH
            var endDate = customEndDate ?? DateTime.Now;
            var startDate = customStartDate ?? endDate.AddDays(-30);

            var totalDaysSelected = (endDate - startDate).TotalDays;
            if (totalDaysSelected <= 0)
            {
                totalDaysSelected = 30;
                startDate = endDate.AddDays(-30);
            }

            var preStartDate = startDate.AddDays(-totalDaysSelected);

            // 2. LẤY TOÀN BỘ DANH SÁCH GIAO DỊCH TRONG CẢ 2 KỲ (Kỳ trước + Kỳ này)
            var allTransactions = await _transactionRepository.GetTransactionsForDashboardAsync(preStartDate, endDate);

            // Phân tách dữ liệu thành 2 kỳ phục vụ tính toán xu hướng (Trends)
            var currentTx = allTransactions.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate).ToList();
            var previousTx = allTransactions.Where(t => t.CreatedAt >= preStartDate && t.CreatedAt < startDate).ToList();

            // 3. LOGIC SECTION 1: NHẬT KÝ GIAO DỊCH LIVE (Top 5 giao dịch mới nhất thuộc kỳ hiện tại)
            response.LiveTransactions = currentTx
            .Where(t =>
                // Condition 1: Successful external TopUp from users
                (string.Equals(t.ReferenceType?.ToString(), "TopUp", StringComparison.OrdinalIgnoreCase) &&
                 string.Equals(t.Type?.ToString(), "Credit", StringComparison.OrdinalIgnoreCase))
                ||
                // OR Condition 2: Actual platform revenue flowing into Admin wallet (WalletId = 1)
                ((t.WalletId == 1 || string.Equals(t.Wallet?.Account?.UserName, "admin", StringComparison.OrdinalIgnoreCase)) &&
                 string.Equals(t.Type?.ToString(), "Credit", StringComparison.OrdinalIgnoreCase) &&
                 (string.Equals(t.ReferenceType?.ToString(), "TryOn", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(t.ReferenceType?.ToString(), "AIRecommendation", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(t.ReferenceType?.ToString(), "OrderPayment", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(t.ReferenceType?.ToString(), "Event", StringComparison.OrdinalIgnoreCase)))
            )
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t =>
            {
                string actionTarget = "System Service";
                bool isTopUp = string.Equals(t.ReferenceType?.ToString(), "TopUp", StringComparison.OrdinalIgnoreCase);

                if (isTopUp)
                {
                    actionTarget = "External Deposit (VnPay)";
                }
                else
                {
                    actionTarget = t.ReferenceType?.ToString() switch
                    {
                        "OrderPayment" => "Marketplace Service Fee Revenue",
                        "TryOn" => "AI Try-On Room Revenue",
                        "AIRecommendation" => "Smart AI Assistant Revenue",
                        "Event" => "Event Hosting Fee Revenue",
                        _ => "Platform Service Revenue"
                    };
                }

                return new LiveTransactionDto
                {
                    Id = t.TransactionId,
                    UserName = isTopUp ? (t.Wallet?.Account?.UserName ?? "Customer") : "Platform Fee Collection",
                    Item = !string.IsNullOrEmpty(t.Description) ? t.Description : actionTarget,
                    Amount = Math.Abs(t.Amount),
                    CreatedAt = t.CreatedAt
                };
            })
            .ToList();

            // Helper Hàm nhóm và tính doanh thu thuần của sàn dựa CHÍNH XÁC trên dòng tiền thu về của Admin (WalletId = 1)
            List<FeatureRevenueDto> CalculateRevenueByFeature(List<Transaction> txList)
            {
                // CHỈ LỌC CÁC GIAO DỊCH MÀ ADMIN NHẬN TIỀN (CREDIT) - ĐÂY CHÍNH LÀ DOANH THU THỰC
                var platformRevenueTx = txList.Where(t =>
                    (t.WalletId == 1 || t.Wallet?.Account?.UserName == "admin") &&
                    (t.Type == "Credit" || t.Type.ToString() == "Credit")
                ).ToList();

                return platformRevenueTx
                    .GroupBy(t => t.ReferenceType.ToString()) // Gom nhóm theo loại Enum/String của ReferenceType
                    .Select(g =>
                    {
                        string featureName = g.Key switch
                        {
                            "OrderPayment" => "E-Commerce Marketplace (Service Fees)",
                            "TryOn" => "AI Try-On Fitting Room",
                            "AIRecommendation" => "Smart Outfit AI Assistant",
                            "Event" => "Event Hosting Fees",
                            _ => "Other Platform Services"
                        };

                        return new FeatureRevenueDto
                        {
                            Feature = featureName,
                            Revenue = g.Sum(x => Math.Abs(x.Amount)),
                            Cost = 0,
                            Users = txList.Where(t => t.ReferenceType.ToString() == g.Key && t.WalletId != 1)
                                          .Select(t => t.WalletId)
                                          .Distinct()
                                          .Count(),
                            Growth = 0
                        };
                    }).ToList();
            }

            // Tính toán doanh thu chi tiết kỳ này và kỳ trước
            var currentFeatures = CalculateRevenueByFeature(currentTx);
            var previousFeatures = CalculateRevenueByFeature(previousTx);

            // Tính toán phần trăm tăng trưởng (Growth) cho từng Feature
            foreach (var cur in currentFeatures)
            {
                var prev = previousFeatures.FirstOrDefault(p => p.Feature == cur.Feature);
                if (prev != null && prev.Revenue > 0)
                {
                    cur.Growth = Math.Round(((cur.Revenue - prev.Revenue) / prev.Revenue) * 100, 1);
                }
                else
                {
                    cur.Growth = prev == null ? 100 : 0;
                }
            }
            response.FeatureRevenue = currentFeatures;

            // 4. LOGIC SECTION 2: TÍNH TOÁN KPI CARD TỔNG QUAN & XU HƯỚNG (TRENDS)
            var totalRevenueCur = currentFeatures.Sum(f => f.Revenue);
            var totalCostCur = currentFeatures.Sum(f => f.Cost);
            var activeUsersCur = currentTx.Where(t => t.WalletId != 1).Select(t => t.WalletId).Distinct().Count();
            decimal marginCur = totalRevenueCur > 0 ? ((totalRevenueCur - totalCostCur) / totalRevenueCur) * 100 : 0;

            var totalRevenuePrev = previousFeatures.Sum(f => f.Revenue);
            var totalCostPrev = previousFeatures.Sum(f => f.Cost);
            var activeUsersPrev = previousTx.Where(t => t.WalletId != 1).Select(t => t.WalletId).Distinct().Count();
            decimal marginPrev = totalRevenuePrev > 0 ? ((totalRevenuePrev - totalCostPrev) / totalRevenuePrev) * 100 : 0;

            string FormatTrend(decimal current, decimal previous)
            {
                if (previous == 0) return current > 0 ? "+100%" : "0%";
                var pct = ((current - previous) / previous) * 100;
                return pct >= 0 ? $"+{pct:F1}%" : $"{pct:F1}%";
            }

            response.Kpis = new KpiDashboard
            {
                GrossFeatureRevenue = totalRevenueCur,
                InfrastructureCost = totalCostCur,
                ActiveFeatureUsers = activeUsersCur,
                NetProfitMargin = Math.Round(marginCur, 1),
                Trends = new Dictionary<string, string>
        {
            { "grossRevenue", FormatTrend(totalRevenueCur, totalRevenuePrev) },
            { "infrastructureCost", FormatTrend(totalCostCur, totalCostPrev) },
            { "activeUsers", FormatTrend((decimal)activeUsersCur, (decimal)activeUsersPrev) },
            { "netProfitMargin", (marginCur - marginPrev) >= 0 ? $"+{(marginCur - marginPrev):F1}%" : $"{(marginCur - marginPrev):F1}%" }
        }
            };

            // 5. LOGIC SECTION 3: VẬN TỐC TIÊU THỤ THEO KHUNG GIỜ
            var hourlyBlocks = new List<(string Label, int StartHour, int EndHour)>
            {
                ("00:00", 0, 3), ("04:00", 4, 7), ("08:00", 8, 11),
                ("12:00", 12, 15), ("16:00", 16, 19), ("20:00", 20, 23)
            };

            response.CreditVelocity = new List<CreditVelocityDto>();

            foreach (var block in hourlyBlocks)
            {
                // Lọc các giao dịch phát sinh trong khung giờ đang xét
                var txInBlock = currentTx
                    .Where(t => t.CreatedAt.Hour >= block.StartHour && t.CreatedAt.Hour <= block.EndHour)
                    .ToList();

                var adminRevenueTxInBlock = txInBlock.Where(t =>
                    (t.WalletId == 1 || t.Wallet?.Account?.UserName == "admin") &&
                    (t.Type == "Credit" || t.Type.ToString() == "Credit") &&
                    (t.ReferenceType.ToString() == "TryOn" ||
                     t.ReferenceType.ToString() == "AIRecommendation" ||
                     t.ReferenceType.ToString() == "OrderPayment" ||
                     t.ReferenceType.ToString() == "Event")
                ).ToList();

                response.CreditVelocity.Add(new CreditVelocityDto
                {
                    Time = block.Label,

                    ApiCalls = adminRevenueTxInBlock.Count,

                    Spend = adminRevenueTxInBlock.Sum(t => Math.Abs(t.Amount))
                });
            }

            return response;
        }

        public async Task<RankingDashboardResponse> GetRankingManagementDashboardAsync(DateTime previousFromDate, DateTime fromDate, DateTime toDate)
        {
            var allOrders = await _orderRepository.GetCompletedOrdersForDashboardAsync(previousFromDate, toDate);

            // Phân tách danh sách đơn hàng thành Kỳ Hiện Tại và Kỳ Trước
            var currentOrders = allOrders.Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate).ToList();
            var previousOrders = allOrders.Where(o => o.CreatedAt >= previousFromDate && o.CreatedAt < fromDate).ToList();

            // TỔNG DOANH THU PHÍ TOÀN SÀN KỲ HIỆN TẠI (Tổng các khoản phí dịch vụ thu từ Order)
            decimal totalPlatformRevenueCur = currentOrders.Sum(o => o.ServiceFee);

            // Lấy danh sách ID của các Shop thực sự có đơn hàng thành công trong kỳ hiện tại
            var activeSellerIds = currentOrders.Select(o => o.SellerId).Distinct().ToList();
            var shopList = new List<ShopRankingDto>();

            // 2. Chia khoảng thời gian kỳ hiện tại thành 6 cột mốc để vẽ biểu đồ xu hướng
            double totalDays = (toDate - fromDate).TotalDays;
            double intervalDays = totalDays / 6;
            var timeIntervals = Enumerable.Range(0, 6)
                .Select(i => fromDate.AddDays(i * intervalDays))
                .ToList();

            var chartLabels = timeIntervals.Select(time => time.ToString("dd/MM")).ToList();

            // 3. DUYỆT QUA TỪNG SHOP ĐỂ PHÂN TÍCH HIỆU SUẤT
            foreach (var sellerId in activeSellerIds)
            {
                // Lọc toàn bộ đơn hàng của Shop này trong kỳ hiện tại và kỳ trước
                var shopOrdersCur = currentOrders.Where(o => o.SellerId == sellerId).ToList();
                var shopOrdersPrev = previousOrders.Where(o => o.SellerId == sellerId).ToList();

                // Lấy tên Shop hiển thị thông qua Navigation Property Seller (Account -> UserName)
                var anyOrder = shopOrdersCur.FirstOrDefault();
                string shopName = anyOrder?.Seller?.UserName ?? $"Shop {sellerId}";

                // DOANH THU SÀN THU ĐƯỢC TỪ SHOP NÀY (Tổng ServiceFee của các đơn hàng thuộc Shop đó)
                decimal shopPlatformRevenueCur = shopOrdersCur.Sum(o => o.ServiceFee);
                int totalOrdersCur = shopOrdersCur.Count;

                // DOANH THU SÀN THU ĐƯỢC TỪ SHOP NÀY Ở KỲ TRƯỚC
                decimal shopPlatformRevenuePrev = shopOrdersPrev.Sum(o => o.ServiceFee);

                // --- TÍNH TOÁN % TĂNG TRƯỞNG CHUẨN KẾ TOÁN ---
                decimal growth = 0;
                if (shopPlatformRevenuePrev > 0)
                {
                    growth = ((shopPlatformRevenueCur - shopPlatformRevenuePrev) / shopPlatformRevenuePrev) * 100;
                }
                else if (shopPlatformRevenuePrev == 0 && shopPlatformRevenueCur > 0)
                {
                    growth = 100;
                }

                // --- TÍNH XU HƯỚNG 6 MỐC THỜI GIAN THEO REVENUE CỦA SHOP ---
                var trendIndexes = new List<int> { 0, 0, 0, 0, 0, 0 };
                if (totalOrdersCur > 0)
                {
                    var shopPeriodData = timeIntervals.Select((time, index) =>
                    {
                        var nextTime = index == 5 ? toDate.AddDays(1) : timeIntervals[index + 1];
                        return shopOrdersCur.Where(o => o.CreatedAt >= time && o.CreatedAt < nextTime).Sum(o => o.ServiceFee);
                    }).ToList();

                    decimal maxShopVal = shopPeriodData.Max();
                    trendIndexes = shopPeriodData.Select(v => maxShopVal > 0 ? (int)((v / maxShopVal) * 100) : 0).ToList();
                }

                // --- PHÂN HẠNG TRẠNG THÁI CỦA SHOP ---
                string status = "Stable";
                if (shopPlatformRevenueCur > 5000) status = "Elite";
                else if (growth > 15) status = "Rising";

                shopList.Add(new ShopRankingDto
                {
                    Id = sellerId,
                    Name = shopName,
                    Revenue = shopPlatformRevenueCur,
                    Orders = totalOrdersCur,
                    Share = totalPlatformRevenueCur > 0 ? Math.Round((shopPlatformRevenueCur / totalPlatformRevenueCur) * 100, 1) : 0,
                    Growth = Math.Round(growth, 1),
                    Status = status,
                    MonthlyTrend = trendIndexes
                });
            }

            shopList = shopList.OrderByDescending(s => s.Revenue).ToList();

            var globalData = timeIntervals.Select((time, index) =>
            {
                var nextTime = index == 5 ? toDate.AddDays(1) : timeIntervals[index + 1];
                return currentOrders.Where(o => o.CreatedAt >= time && o.CreatedAt < nextTime).Sum(o => o.ServiceFee);
            }).ToList();

            decimal maxGlobal = globalData.Max();
            var globalTrendIndexes = globalData.Select(v => maxGlobal > 0 ? (int)((v / maxGlobal) * 100) : 0).ToList();

            decimal avgTicketSize = currentOrders.Any() ? currentOrders.Average(o => o.TotalAmount) : 0;

            int activeNodes = shopList.Count(s => s.Orders > 0);
            string leaderboardAlpha = shopList.FirstOrDefault()?.Name ?? "No Sales";

            return new RankingDashboardResponse
            {
                ActiveNodes = activeNodes,
                MarketReach = 100,
                LeaderboardAlpha = leaderboardAlpha,
                AvgTicketSize = Math.Round(avgTicketSize, 2),
                GlobalTrend = globalTrendIndexes,
                ChartLabels = chartLabels,
                Shops = shopList
            };
        }

        private TransactionResponse MapToResponse(Transaction t)
        {
            return new TransactionResponse
            {
                TransactionId = t.TransactionId,
                WalletId = t.WalletId,
                UserName = t.Wallet?.Account?.UserName ?? "Unknown",
                PaymentId = t.PaymentId,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Type = t.Type,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                Status = t.Status
            };
        }
    }
}