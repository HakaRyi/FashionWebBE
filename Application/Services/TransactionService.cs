using Application.Interfaces;
using Application.Request.NotificationReq;
using Application.Response.EscrowSessionResp;
using Application.Response.TransactionResp;
using Application.Services.NotificationImp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;

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
        public async Task<List<TransactionResponse>> AdminGetAllTransactionsAsync(string? type = null, string? refType = null, int? refId = null)
        {
            var transactions = await _transactionRepository.GetTransactionsAsync(type, refType, refId,
            t => t.Wallet!.Account!);
            return transactions.Adapt<List<TransactionResponse>>();
        }

        public async Task<TransactionResponse?> GetById(int id)
        {
            var t = await _transactionRepository.GetByIdAsync(id);
            if (t == null) return null;

            return MapToResponse(t);
        }

        public async Task<List<TransactionResponse>> GetTransactions()
        {
            var transactions = await _transactionRepository.GetTransactionsAsync();
            return transactions.Select(t => MapToResponse(t)).ToList();
        }

        public async Task<FeatureIntelligenceResponse> GetFeatureIntelligenceDashboardAsync()
        {
            var response = new FeatureIntelligenceResponse();

            // Thiết lập mốc thời gian lọc (Mặc định quét dữ liệu 30 ngày gần nhất)
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-30);

            // 1. Lấy danh sách giao dịch thô đã thành công từ Repository
            var rawTransactions = await _transactionRepository.GetTransactionsForDashboardAsync(startDate, endDate);

            // 2. LOGIC SECTION 1: NHẬT KÝ GIAO DỊCH LIVE (Top 5 giao dịch mới nhất)
            response.LiveTransactions = rawTransactions
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

            // 3. LOGIC SECTION 2: DOANH THU THEO TÍNH NĂNG HỆ THỐNG (Revenue by Feature)
            // Lọc các ReferenceType đóng vai trò trực tiếp tạo ra nguồn tiền dòng thu Hybrid
            var revenueTransactions = rawTransactions
                .Where(t => t.ReferenceType == "TryOn" ||
                            t.ReferenceType == "AIRecommendation" ||
                            t.ReferenceType == "System_Fee_Revenue" ||
                            (t.ReferenceType == "OrderPayment" && t.Type == "Credit"))
                .ToList();

            response.FeatureRevenue = revenueTransactions
                .GroupBy(t => t.ReferenceType)
                .Select(g => {
                    var totalRev = g.Sum(x => Math.Abs(x.Amount));
                    string featureName = "Other Services";
                    decimal estimatedCost = 0;

                    switch (g.Key)
                    {
                        case "OrderPayment":
                            featureName = "E-Commerce Marketplace";
                            estimatedCost = totalRev * 0.05m; // Chi phí hạ tầng thanh toán/vận hành (5%)
                            break;
                        case "System_Fee_Revenue":
                            featureName = "Event Hosting Fees";
                            estimatedCost = totalRev * 0.10m; // Phí nhân sự duyệt Event (10%)
                            break;
                        case "TryOn":
                            featureName = "AI Try-On Fitting Room";
                            estimatedCost = totalRev * 0.25m; // Chi phí GPU máy chủ AI cao (25%)
                            break;
                        case "AIRecommendation":
                            featureName = "Smart Outfit AI Assistant";
                            estimatedCost = totalRev * 0.08m; // Phí token API LLM (8%)
                            break;
                    }

                    return new FeatureRevenueDto
                    {
                        Feature = featureName,
                        Revenue = totalRev,
                        Cost = Math.Round(estimatedCost, 2),
                        Users = g.Select(x => x.WalletId).Distinct().Count(),
                        Growth = g.Key == "OrderPayment" ? 14.5m : g.Key == "System_Fee_Revenue" ? 22.8m : 11.2m
                    };
                })
                .ToList();

            // 4. LOGIC SECTION 3: TÍNH TOÁN KPI CARD TỔNG QUAN
            var totalRevenue = response.FeatureRevenue.Sum(f => f.Revenue);
            var totalCost = response.FeatureRevenue.Sum(f => f.Cost);
            var activeUsersCount = rawTransactions.Select(t => t.WalletId).Distinct().Count();
            decimal margin = totalRevenue > 0 ? ((totalRevenue - totalCost) / totalRevenue) * 100 : 0;

            response.Kpis = new KpiDashboard
            {
                GrossFeatureRevenue = totalRevenue,
                InfrastructureCost = totalCost,
                ActiveFeatureUsers = activeUsersCount,
                NetProfitMargin = Math.Round(margin, 1),
                Trends = new Dictionary<string, string>
                {
                    { "grossRevenue", "+16.2%" },
                    { "infrastructureCost", "+4.1%" },
                    { "activeUsers", "+9.5%" },
                    { "netProfitMargin", "+1.1%" }
                }
            };

            // 5. LOGIC SECTION 4: VẬN TỐC TIÊU THỤ THEO KHUNG GIỜ (Credit Velocity) trong ngày hôm nay
            var todayTransactions = rawTransactions
                .Where(t => t.CreatedAt.Date == DateTime.Today)
                .ToList();

            var hourlyBlocks = new List<(string Label, int StartHour, int EndHour)>
            {
                ("00:00", 0, 3), ("04:00", 4, 7), ("08:00", 8, 11),
                ("12:00", 12, 15), ("16:00", 16, 19), ("20:00", 20, 23)
            };

            response.CreditVelocity = new List<CreditVelocityDto>();

            foreach (var block in hourlyBlocks)
            {
                var txInBlock = todayTransactions
                    .Where(t => t.CreatedAt.Hour >= block.StartHour && t.CreatedAt.Hour <= block.EndHour)
                    .ToList();

                response.CreditVelocity.Add(new CreditVelocityDto
                {
                    Time = block.Label,
                    // ApiCalls: Đếm mọi hành động tương tác thanh toán/sử dụng dịch vụ của user
                    ApiCalls = txInBlock.Count(t => t.ReferenceType == "TryOn" ||
                                                   t.ReferenceType == "AIRecommendation" ||
                                                   t.ReferenceType == "OrderPayment" ||
                                                   t.ReferenceType == "System_Fee_Revenue"),
                    // Spend: Tổng lượng nạp tiền thực qua cổng TopUp VNPAY để lấy dòng tiền chảy vào hệ thống
                    Spend = txInBlock.Where(t => t.ReferenceType == "TopUp").Sum(t => Math.Abs(t.Amount))
                });
            }

            return response;
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