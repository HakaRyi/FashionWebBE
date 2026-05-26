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


        public TransactionService(
            ITransactionRepository transactionRepository,
            IEscrowSessionRepository escrowRepository,
            INotificationService notificationService,
            IWalletRepository walletRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _transactionRepository = transactionRepository;
            _escrowRepository = escrowRepository;
            _notificationService = notificationService;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
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
            var escrows = await _escrowRepository.Query()
                .Include(e => e.Sender)
                .Include(e => e.Event)
                .Include(e => e.Order)
                    .ThenInclude(o => o.Seller)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new EscrowResponse
                {
                    EscrowSessionId = e.EscrowSessionId,

                    EventId = e.EventId,
                    EventTitle = e.Event != null ? e.Event.Title : null,

                    OrderId = e.OrderId,
                    OrderCode = e.Order != null ? e.Order.OrderCode : null,

                    SenderId = e.SenderId,
                    SenderName = e.Sender != null
                        ? e.Sender.UserName ?? "Unknown"
                        : "Unknown",

                    ReceiverId = e.ReceiverId,
                    ReceiverName = e.Order != null && e.Order.Seller != null
                        ? e.Order.Seller.UserName ?? "Unknown"
                        : "System",

                    Amount = e.Amount,
                    ServiceFee = e.ServiceFee,
                    FinalAmount = e.FinalAmount > 0
                        ? e.FinalAmount
                        : e.Amount - e.ServiceFee,

                    Status = e.Status,
                    Description = e.Description,
                    CreatedAt = e.CreatedAt,
                    ResolvedAt = e.ResolvedAt
                })
                .ToListAsync();

            return escrows;
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
            // 1. Nạp giao dịch và Include rõ ràng từ cấp Wallet đến Account để không bị null dữ liệu liên kết
            var transactions = await _transactionRepository.GetTransactionsAsync(
                type,
                refType,
                refId,
                t => t.Wallet,
                t => t.Wallet!.Account!
            );
            var adminWalletCurBalance = await _walletRepository.GetByIdAsync(1);

            // 2. Thu thập các ID quy chiếu để tối ưu hóa truy vấn Batch
            var eventIds = transactions.Where(t => t.ReferenceType == "Event" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var orderIds = transactions.Where(t => t.ReferenceType == "OrderPayment" || t.ReferenceType == "OrderRefund").Where(t => t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var tryOnIds = transactions.Where(t => t.ReferenceType == "TryOn" && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var aiRecomIds = transactions.Where(t => (t.ReferenceType == "AIRecommendation" || t.ReferenceType == "AI_Recommendation") && t.ReferenceId.HasValue).Select(t => t.ReferenceId!.Value).Distinct().ToList();
            var allRefIds = eventIds.Concat(orderIds).Concat(tryOnIds).Concat(aiRecomIds).Distinct().ToList();

            // 3. Lấy danh sách các Escrow Sessions liên quan (Repo đã nạp kèm cả Sender, Receiver, Event, Order)
            var relevantEscrows = await _escrowRepository.GetEscrowsByBatchIdsAsync(eventIds, orderIds);
            var systemTransactions = await _transactionRepository.GetTransactionsAsync(type: null, refType: null, refId: null);
            var matchedSystemTxList = systemTransactions.Where(st => st.WalletId == 1 && st.ReferenceId.HasValue && allRefIds.Contains(st.ReferenceId.Value)).ToList();

            // Khởi tạo bộ theo dõi số dư lũy tiến riêng biệt cho từng EscrowSessionId
            var escrowStateCounters = relevantEscrows.ToDictionary(
                e => e.EscrowSessionId,
                e => new
                {
                    PrizeBalance = 0m,
                    RevenueBalance = 0m,
                    OrderBalance = e.OrderId.HasValue ? e.Amount : 0m
                }
            );

            // Sắp xếp xuôi theo thời gian (Tuyến tính) để cộng dồn số dư lũy tiến chính xác
            var chronologicalTransactions = transactions.OrderBy(t => t.CreatedAt).ToList();
            var computedResponsesMap = new Dictionary<int, TransactionResponseV2>();

            // 4. Duyệt tuyến tính dòng tiền
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

                EscrowSession? matchedEscrow = null;

                string typeLower = t.Type?.ToLower() ?? "";
                string codeLower = t.TransactionCode?.ToLower() ?? "";
                string descLower = t.Description?.ToLower() ?? "";

                // Biến đại diện cho luồng xử lý logic cuối cùng của switch-case
                string logicType = t.Type;

                // --- 1. PHÂN BỔ MATCHED ESCROW CHO LUỒNG EVENT HOẶC ORDER ---
                if (t.ReferenceType == "Event" && t.ReferenceId.HasValue)
                {
                    var eventEscrows = relevantEscrows.Where(e => e.EventId == t.ReferenceId.Value).ToList();
                    var prizePoolEscrow = eventEscrows.FirstOrDefault(e => e.Event != null && e.SenderId == e.Event.CreatorId);

                    if (typeLower == "credit" || typeLower == "debit")
                    {
                        if (codeLower.Contains("ref") || descLower.Contains("refund") || descLower.Contains("hoàn tiền"))
                            logicType = "Event_Refund";
                        else if (codeLower.Contains("pay") || descLower.Contains("fee") || descLower.Contains("lệ phí"))
                            logicType = "Event_Entry_Fee_Paid";
                        else if (codeLower.Contains("rel") || descLower.Contains("release") || descLower.Contains("giải ngân"))
                            logicType = "Event_Revenue_Released";
                        else if (codeLower.Contains("prz") || descLower.Contains("prize") || descLower.Contains("thưởng"))
                            logicType = "Prize_Reward";
                    }

                    switch (logicType)
                    {
                        case "Event_Entry_Fee_Paid":
                            var currentUserId = t.Wallet?.AccountId;
                            matchedEscrow = eventEscrows.FirstOrDefault(e => e.SenderId == currentUserId && e.EscrowSessionId != prizePoolEscrow?.EscrowSessionId);
                            if (matchedEscrow == null)
                            {
                                matchedEscrow = eventEscrows.FirstOrDefault(e => e.EscrowSessionId != prizePoolEscrow?.EscrowSessionId);
                            }
                            break;

                        case "Event_Revenue_Released":
                            matchedEscrow = eventEscrows.FirstOrDefault(e => e.EscrowSessionId != prizePoolEscrow?.EscrowSessionId);
                            break;

                        case "Escrow_Prize_Hold":
                        case "Prize_Reward":
                        case "Event_Refund":
                            matchedEscrow = prizePoolEscrow;
                            break;

                        default:
                            if (t.Type != "System_Fee_Payment" && t.Type != "System_Fee_Revenue")
                            {
                                matchedEscrow = prizePoolEscrow ?? eventEscrows.FirstOrDefault();
                            }
                            break;
                    }
                }
                else if ((t.ReferenceType == "OrderPayment" || t.ReferenceType == "OrderRefund") && t.ReferenceId.HasValue)
                {
                    // Tìm kiếm phiên Escrow khớp với mã đơn hàng
                    matchedEscrow = relevantEscrows.FirstOrDefault(e => e.OrderId == t.ReferenceId);

                    // Chuẩn hóa logicType cho Đơn hàng trong trường hợp DB chỉ lưu "Credit" / "Debit"
                    if (t.ReferenceType == "OrderRefund" || codeLower.StartsWith("ref-") || descLower.Contains("refund") || descLower.Contains("hoàn tiền"))
                    {
                        logicType = "OrderRefund";
                    }
                    else
                    {
                        logicType = "OrderPayment";
                    }
                }
                else
                {
                    matchedEscrow = null;
                }

                // --- 2. TÍNH TOÁN BIẾN ĐỘNG SỐ DƯ QUỸ (ESCROW CALCULATIONS) ---
                if (matchedEscrow != null)
                {
                    int sessionId = matchedEscrow.EscrowSessionId;
                    string action = "None";
                    decimal amountChanged = 0m;

                    var currentState = escrowStateCounters.ContainsKey(sessionId)
                        ? escrowStateCounters[sessionId]
                        : new { PrizeBalance = 0m, RevenueBalance = 0m, OrderBalance = matchedEscrow.Amount };

                    decimal escrowBefore = 0m;
                    decimal escrowAfter = 0m;

                    decimal newPrizeBalance = currentState.PrizeBalance;
                    decimal newRevenueBalance = currentState.RevenueBalance;
                    decimal newOrderBalance = currentState.OrderBalance;

                    switch (logicType)
                    {
                        // ====== LUỒNG TIỀN VÉ EVENT (ENTRY FEE) ======
                        case "Event_Entry_Fee_Paid":
                            action = "Entry_Fee_Held";
                            amountChanged = Math.Abs(t.Amount);
                            escrowBefore = currentState.RevenueBalance;
                            escrowAfter = escrowBefore + amountChanged;
                            newRevenueBalance = escrowAfter;
                            break;

                        case "Event_Revenue_Released":
                            action = "Revenue_Released";
                            amountChanged = -Math.Abs(t.Amount);
                            escrowBefore = currentState.RevenueBalance;
                            escrowAfter = escrowBefore + amountChanged;
                            newRevenueBalance = escrowAfter;
                            break;

                        // ====== LUỒNG QUỸ GIẢI THƯỞNG EVENT ======
                        case "Escrow_Hold":
                        case "Escrow_Prize_Hold":
                        case "Prize_Reward":

                            if (t.Amount < 0)
                            {
                                action = "Deposit_Held";
                                amountChanged = Math.Abs(t.Amount);
                                escrowBefore = currentState.PrizeBalance;
                                escrowAfter = escrowBefore + amountChanged;
                            }
                            else
                            {
                                action = "Prize_Released";
                                amountChanged = -Math.Abs(t.Amount);
                                escrowBefore = currentState.PrizeBalance;
                                escrowAfter = escrowBefore + amountChanged;
                            }
                            newPrizeBalance = escrowAfter;
                            break;

                        case "Event_Refund":
                            action = "Refund_Released";
                            amountChanged = -Math.Abs(t.Amount);
                            escrowBefore = currentState.PrizeBalance;
                            escrowAfter = escrowBefore + amountChanged;
                            newPrizeBalance = escrowAfter;
                            break;

                        // ====== LUỒNG ĐƠN HÀNG THƯƠNG MẠI (ORDER LOGIC) ======
                        case "OrderPayment":
                            // 1. LUỒNG GIẢI NGÂN (TIỀN CHẠY RA KHỎI QUỸ)
                            if (t.Amount > 0)
                            {
                                action = "Deposit_Released";
                                decimal releaseAmount = Math.Abs(t.Amount);

                                escrowBefore = currentState.OrderBalance > 0m ? currentState.OrderBalance : matchedEscrow.Amount;

                                amountChanged = -releaseAmount;

                                escrowAfter = escrowBefore - releaseAmount;
                                if (escrowAfter < 0) escrowAfter = 0m;

                                newOrderBalance = escrowAfter;
                            }

                            else
                            {
                                action = "Deposit_Held";
                                amountChanged = Math.Abs(t.Amount);
                                escrowBefore = 0m;
                                escrowAfter = amountChanged;
                                newOrderBalance = escrowAfter;
                            }
                            break;

                        case "OrderRefund":
                            action = "Refund_Released";
                            amountChanged = -Math.Abs(t.Amount); // Ví dụ: -30000

                            // Lấy trực tiếp số dư ký quỹ ban đầu từ bộ đếm (đã gán mặc định bằng matchedEscrow.Amount)
                            escrowBefore = currentState.OrderBalance > 0m ? currentState.OrderBalance : matchedEscrow.Amount;
                            escrowAfter = escrowBefore + amountChanged; // 30000 + (-30000) = 0
                            newOrderBalance = escrowAfter;
                            break;

                        default:
                            action = "None";
                            amountChanged = 0m;
                            escrowBefore = currentState.PrizeBalance;
                            escrowAfter = currentState.PrizeBalance;
                            break;
                    }

                    // Cập nhật lại bộ theo dõi số dư lũy tiến cho vòng lặp tiếp theo
                    escrowStateCounters[sessionId] = new { PrizeBalance = newPrizeBalance, RevenueBalance = newRevenueBalance, OrderBalance = newOrderBalance };

                    // Gán dữ liệu vào DTO Response
                    res.EscrowDetail = new EscrowBriefResponse
                    {
                        EscrowSessionId = sessionId,
                        Status = matchedEscrow.Status,
                        OriginalAmount = matchedEscrow.Amount,
                        SystemServiceFee = matchedEscrow.ServiceFee,
                        FinalPayoutAmount = matchedEscrow.FinalAmount,
                        EscrowAction = action,
                        EscrowAmountChanged = amountChanged,
                        EscrowBefore = escrowBefore,
                        EscrowAfter = escrowAfter,

                        SenderName = matchedEscrow.Sender?.UserName ?? (logicType == "Escrow_Prize_Hold" ? res.UserName : matchedEscrow.SenderId.ToString()),
                        ReceiverName = matchedEscrow.Receiver?.UserName ?? (logicType == "Prize_Reward" ? res.UserName : (matchedEscrow.ReceiverId?.ToString() ?? "System")),
                        LinkedTargetName = matchedEscrow.Event?.Title ?? (matchedEscrow.Order != null ? $"Order #{matchedEscrow.OrderId}" : "N/A")
                    };
                }

                // --- B. XỬ LÝ ĐỐI SOÁT VÍ ADMIN SƠ CẤP ---
                if (t.WalletId != 1)
                    {
                        Transaction? realSystemTx = matchedSystemTxList.FirstOrDefault(st =>
                            st.ReferenceType == t.ReferenceType && st.ReferenceId == t.ReferenceId &&
                            (t.Type == "System_Fee_Payment" ? st.Type == "System_Fee_Revenue" : st.Type == t.Type)
                        );

                        if (realSystemTx == null) realSystemTx = matchedSystemTxList.FirstOrDefault(st => st.ReferenceType == t.ReferenceType && st.ReferenceId == t.ReferenceId);

                        if (realSystemTx != null)
                        {
                            res.SystemWalletSnapshot = new SystemWalletSnapshotResponse { PlatformAmountChanged = realSystemTx.Amount, PlatformBalanceAfter = realSystemTx.BalanceAfter, DataMode = "Real" };
                        }
                        else if (t.Type == "Debit" || t.ReferenceType == "TryOn" || t.ReferenceType == "AIRecommendation" || t.ReferenceType == "AI_Recommendation")
                        {
                            res.SystemWalletSnapshot = new SystemWalletSnapshotResponse { PlatformAmountChanged = Math.Abs(t.Amount), PlatformBalanceAfter = adminWalletCurBalance?.Balance ?? 0, DataMode = "Fallback" };
                            if (string.IsNullOrEmpty(res.Description) || !res.Description.StartsWith("[")) res.Description = $"[Interpolation Reconciliation] {res.Description}";
                        }
                        else res.SystemWalletSnapshot = null;
                    }

                    // --- C. XỬ LÝ ĐỐI SOÁT NGƯỜI GỬI/NHẬN CHO VÍ ADMIN (WALLET ID == 1) ---
                    else if (t.WalletId == 1)
                    {
                        // Quét tìm bản ghi của User có giá trị tiền nghịch đảo âm-dương (-10k vs +10k), khớp cấu trúc tham chiếu và mốc thời gian trùng khớp
                        var userCounterpartTx = chronologicalTransactions.FirstOrDefault(ut =>
                            ut.WalletId != 1 &&
                            ut.ReferenceType == t.ReferenceType &&
                            ut.ReferenceId == t.ReferenceId &&
                            ut.Amount == -t.Amount &&
                            Math.Abs((ut.CreatedAt - t.CreatedAt).TotalSeconds) < 2 // Sai số tạo bản ghi lệch vài phần trăm giây trong DB
                        );

                        if (userCounterpartTx != null)
                        {
                            // Tìm ra đích danh thông tin tài khoản người dùng tương tác với hệ thống
                            res.CounterpartyUserId = userCounterpartTx.Wallet?.AccountId;
                            res.CounterpartyName = userCounterpartTx.Wallet?.Account?.UserName ?? "User";
                        }
                        else
                        {
                            // Fallback dự phòng nếu không tìm thấy giao dịch dòng tiền đối ứng trực tiếp
                            res.CounterpartyUserId = null;
                            res.CounterpartyName = "External Source / Network";
                        }
                    }

                    computedResponsesMap[t.TransactionId] = res;
            }

                // 5. TRẢ VỀ: Trả dữ liệu map theo đúng thứ tự sắp xếp gốc của thực thể `transactions` từ Database
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

            // 1. XỬ LÝ ĐIỀU KIỆN THỜI GIAN MẶC ĐỊNH KHÔNG NHẬP
            // Nếu không nhập, endDate mặc định là Hiện tại, startDate mặc định là 30 ngày trước
            var endDate = customEndDate ?? DateTime.Now;
            var startDate = customStartDate ?? endDate.AddDays(-30);

            // Tính toán độ dài khoảng thời gian đã chọn (ví dụ: 30 ngày, 7 ngày, 15 ngày...)
            var totalDaysSelected = (endDate - startDate).TotalDays;
            if (totalDaysSelected <= 0)
            {
                totalDaysSelected = 30; // Tránh lỗi chia cho 0 hoặc khoảng thời gian âm
                startDate = endDate.AddDays(-30);
            }

            // Kỳ trước (Previous Period) có cùng độ dài (X ngày) và nằm ngay trước kỳ này để so sánh Trend chính xác
            var preStartDate = startDate.AddDays(-totalDaysSelected);

            // 2. LẤY TOÀN BỘ DANH SÁCH GIAO DỊCH TRONG CẢ 2 KỲ (Kỳ trước + Kỳ này)
            var allTransactions = await _transactionRepository.GetTransactionsForDashboardAsync(preStartDate, endDate);

            // Phân tách dữ liệu thành 2 kỳ phục vụ tính toán xu hướng (Trends)
            var currentTx = allTransactions.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate).ToList();
            var previousTx = allTransactions.Where(t => t.CreatedAt >= preStartDate && t.CreatedAt < startDate).ToList();

            // 3. LOGIC SECTION 1: NHẬT KÝ GIAO DỊCH LIVE (Top 5 giao dịch mới nhất thuộc kỳ hiện tại)
            response.LiveTransactions = currentTx
                .Where(t => t.Wallet?.Account?.UserName != "admin")
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new LiveTransactionDto
                {
                    Id = t.TransactionId,
                    UserName = t.Wallet?.Account?.UserName ?? "Customer",
                    Item = !string.IsNullOrEmpty(t.Description) ? t.Description : $"{t.ReferenceType} - {t.Type}",
                    Amount = t.Amount,
                    CreatedAt = t.CreatedAt
                })
                .ToList();

            // Helper Hàm nhóm và tính doanh thu theo các tính năng thực tế từ DB
            List<FeatureRevenueDto> CalculateRevenueByFeature(List<Transaction> txList)
            {
                var revenueTx = txList.Where(t =>
                    (t.ReferenceType == "TryOn" && t.Type == "Debit") ||
                    (t.ReferenceType == "AIRecommendation" && t.Type == "Debit") ||
                    (t.Type == "System_Fee_Revenue") ||
                    (t.ReferenceType == "OrderPayment" && t.Type == "Credit")
                ).ToList();

                return revenueTx
                    .GroupBy(t => t.ReferenceType == "Event" || t.Type == "System_Fee_Revenue" ? "System_Fee_Revenue" : t.ReferenceType)
                    .Select(g => {
                        string featureName = g.Key switch
                        {
                            "OrderPayment" => "E-Commerce Marketplace",
                            "System_Fee_Revenue" => "Event Hosting Fees",
                            "TryOn" => "AI Try-On Fitting Room",
                            "AIRecommendation" => "Smart Outfit AI Assistant",
                            _ => "Other Services"
                        };

                        return new FeatureRevenueDto
                        {
                            Feature = featureName,
                            Revenue = g.Sum(x => Math.Abs(x.Amount)),
                            Cost = 0, // Hiện tại chưa có chi phí vận hành
                            Users = g.Select(x => x.WalletId).Distinct().Count(),
                            Growth = 0 // Sẽ cập nhật ở bước sau
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
            var totalCostCur = currentFeatures.Sum(f => f.Cost); // Bằng 0
            var activeUsersCur = currentTx.Select(t => t.WalletId).Distinct().Count();
            decimal marginCur = totalRevenueCur > 0 ? ((totalRevenueCur - totalCostCur) / totalRevenueCur) * 100 : 0;

            var totalRevenuePrev = previousFeatures.Sum(f => f.Revenue);
            var totalCostPrev = previousFeatures.Sum(f => f.Cost);
            var activeUsersPrev = previousTx.Select(t => t.WalletId).Distinct().Count();
            decimal marginPrev = totalRevenuePrev > 0 ? ((totalRevenuePrev - totalCostPrev) / totalRevenuePrev) * 100 : 0;

            // Hàm format chuỗi hiển thị trend
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

            // 5. LOGIC SECTION 3: VẬN TỐC TIÊU THỤ THEO KHUNG GIỜ (Tính gộp cho toàn bộ khoảng thời gian được chọn)
            // Thay vì chỉ lấy transactions của 1 ngày duy nhất (todayTransactions), ta lấy toàn bộ trong kỳ hiện tại (currentTx)
            var hourlyBlocks = new List<(string Label, int StartHour, int EndHour)>
{
            ("00:00", 0, 3), ("04:00", 4, 7), ("08:00", 8, 11),
            ("12:00", 12, 15), ("16:00", 16, 19), ("20:00", 20, 23)
};

            response.CreditVelocity = new List<CreditVelocityDto>();

            foreach (var block in hourlyBlocks)
            {
                // Lấy transactions rơi vào khung giờ này, không phân biệt ngày nào trong kỳ
                var txInBlock = currentTx
                    .Where(t => t.CreatedAt.Hour >= block.StartHour && t.CreatedAt.Hour <= block.EndHour)
                    .ToList();

                // Lọc các giao dịch tiêu tốn tiền dịch vụ AI/Platform (Loại trừ các giao dịch Credit nhận tiền)
                var apiTransactions = txInBlock.Where(t =>
                    (t.ReferenceType == "TryOn" && t.Type == "Debit") ||
                    (t.ReferenceType == "AIRecommendation" && t.Type == "Debit") ||
                    (t.ReferenceType == "OrderPayment" && t.Type == "Debit") ||
                    (t.Type == "System_Fee_Revenue") // Thu phí hệ thống
                );

                response.CreditVelocity.Add(new CreditVelocityDto
                {
                    Time = block.Label,

                    // Tính tổng tiền VND tiêu thụ tính năng phát sinh trong khung giờ này
                    ApiCalls = (int)apiTransactions.Sum(t => Math.Abs(t.Amount)),

                    // Tính tổng tiền VND nạp mới qua dòng TopUp trong khung giờ này
                    Spend = txInBlock.Where(t => t.ReferenceType == "TopUp").Sum(t => Math.Abs(t.Amount))
                });
            }

            return response;
        }

        public async Task<RankingDashboardResponse> GetRankingManagementDashboardAsync(DateTime previousFromDate, DateTime fromDate, DateTime toDate)
        {
            // 1. Quét DB mở rộng từ ngày bắt đầu kỳ trước cho đến ngày kết thúc kỳ này
            var transactions = await _transactionRepository.GetAllTransactionsAsync(previousFromDate, toDate);

            // Lọc các giao dịch thành công đem lại doanh thu cho Shop (Áp dụng cho toàn bộ tập dữ liệu lấy về)
            var allOrderPayments = transactions
                .Where(t => t.Status == "Success"
                         && t.ReferenceType == "OrderPayment"
                         && t.Type == "Credit"
                         && t.WalletId != 1)
                .ToList();

            // Tách tập dữ liệu thuộc Kỳ Hiện Tại (Dùng để tính Revenue, Share, Chart và các chỉ số hiển thị)
            var currentOrderPayments = allOrderPayments
                .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
                .ToList();

            decimal totalMarketRevenue = currentOrderPayments.Sum(t => t.Amount);

            // Group danh sách theo các Shop dựa trên data của KỲ HIỆN TẠI để hiển thị bảng xếp hạng công bằng
            var shopGroups = currentOrderPayments.GroupBy(t => t.WalletId).ToList();
            var shopList = new List<ShopRankingDto>();

            // 2. Chia khoảng thời gian kỳ hiện tại thành 6 cột mốc để vẽ biểu đồ
            var totalDays = (toDate - fromDate).TotalDays;
            var intervalDays = totalDays / 6;
            var timeIntervals = Enumerable.Range(0, 6)
                .Select(i => fromDate.AddDays(i * intervalDays))
                .ToList();

            foreach (var group in shopGroups)
            {
                var firstTx = group.First();
                // Lấy tên User hiển thị
                string shopName = !string.IsNullOrEmpty(firstTx.Wallet?.Account?.UserName)
                    ? firstTx.Wallet.Account.UserName
                    : $"Apex {firstTx.WalletId}";

                // Doanh thu và số đơn hàng của kỳ HIỆN TẠI
                decimal totalRevenue = group.Sum(t => t.Amount);
                int totalOrders = group.Select(t => t.ReferenceId).Distinct().Count();

                // --- LOGIC TĂNG TRƯỞNG (GROWTH) THEO CÁCH 1 ---
                // Tìm doanh thu của chính Shop này trong KỲ TRƯỚC (từ previousFromDate đến trước fromDate)
                decimal previousRevenue = allOrderPayments
                    .Where(t => t.WalletId == group.Key && t.CreatedAt >= previousFromDate && t.CreatedAt < fromDate)
                    .Sum(t => t.Amount);

                decimal growth = 0;
                if (previousRevenue > 0)
                {
                    growth = ((totalRevenue - previousRevenue) / previousRevenue) * 100;
                }
                else if (previousRevenue == 0 && totalRevenue > 0)
                {
                    growth = 100; // Kỳ trước không có doanh thu, kỳ này phát sinh -> Đạt mốc tăng trưởng ấn tượng 100%
                }

                // Tính xu hướng cho từng phân đoạn thời gian kỳ hiện tại (0 -> 100)
                var shopMonthlyData = timeIntervals.Select((time, index) => {
                    var nextTime = index == 5 ? toDate : timeIntervals[index + 1];
                    return group.Where(t => t.CreatedAt >= time && t.CreatedAt < nextTime).Sum(t => t.Amount);
                }).ToList();

                decimal maxShopVal = shopMonthlyData.Max();
                var trendIndexes = shopMonthlyData.Select(v => maxShopVal > 0 ? (int)((v / maxShopVal) * 100) : 0).ToList();

                // Phân hạng phân khúc dựa theo doanh thu trong kỳ hiện tại
                string status = "Stable";
                if (totalRevenue > 20000) status = "Elite";
                else if (growth > 15) status = "Rising";
                else if (totalOrders == 0) status = "Under Review";

                shopList.Add(new ShopRankingDto
                {
                    Id = group.Key,
                    Name = shopName,
                    Revenue = totalRevenue,
                    Orders = totalOrders,
                    Share = totalMarketRevenue > 0 ? Math.Round((totalRevenue / totalMarketRevenue) * 100, 1) : 0,
                    Growth = Math.Round(growth, 1),
                    Status = status,
                    MonthlyTrend = trendIndexes
                });
            }

            // Kiểm tra xem có shop nào kỳ trước có doanh thu nhưng kỳ này KHÔNG có đơn (không nằm trong shopGroups) 
            // Nếu muốn bổ sung các shop tụt hạng về doanh thu = 0 (Growth = -100%) bạn có thể tối ưu thêm tại đây.

            shopList = shopList.OrderByDescending(s => s.Revenue).ToList();

            // 3. Tính toán Global Trend (Chỉ số chung của toàn sàn trong các phân đoạn thuộc kỳ hiện tại)
            var globalData = timeIntervals.Select((time, index) => {
                var nextTime = index == 5 ? toDate : timeIntervals[index + 1];
                return currentOrderPayments.Where(t => t.CreatedAt >= time && t.CreatedAt < nextTime).Sum(t => t.Amount);
            }).ToList();

            decimal maxGlobal = globalData.Max();
            var globalTrendIndexes = globalData.Select(v => maxGlobal > 0 ? (int)((v / maxGlobal) * 100) : 0).ToList();

            // 4. Tính toán các Tile tổng quan (Chỉ tính dựa trên kỳ hiện tại đang chọn)
            int activeNodes = currentOrderPayments.Select(t => t.WalletId).Distinct().Count();
            string leaderboardAlpha = shopList.FirstOrDefault()?.Name ?? "No Sales";
            decimal avgTicketSize = currentOrderPayments.Any() ? currentOrderPayments.Average(t => t.Amount) : 0;

            return new RankingDashboardResponse
            {
                ActiveNodes = activeNodes,
                MarketReach = 100,
                LeaderboardAlpha = leaderboardAlpha,
                AvgTicketSize = Math.Round(avgTicketSize, 2),
                GlobalTrend = globalTrendIndexes.Count > 0 ? globalTrendIndexes : new List<int> { 0, 0, 0, 0, 0, 0 },
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