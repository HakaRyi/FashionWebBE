using Repositories.Entities;
using Repositories.Repos.WalletRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.Payments;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Implements.NotificationImp;
using Services.Request.NotificationReq;
using Services.Request.WalletReq;
using Services.Response.TransactionResp;
using Services.Response.WalletResp;

namespace Services.Implements.WalletImp
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public WalletService(
            IUnitOfWork unitOfWork,
            IWalletRepository walletRepo,
            ITransactionRepository transactionRepo,
            IPaymentRepository paymentRepo,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _paymentRepo = paymentRepo;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<WalletResponse> GetMyWalletAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);

            if (wallet == null)
                throw new Exception("Không tìm thấy ví người dùng.");

            return new WalletResponse
            {
                WalletId = wallet.WalletId,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public async Task<List<TransactionHistoryResponse>> GetMyTransactionHistoryAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);

            if (wallet == null)
                throw new Exception("Ví không tồn tại.");

            var transactions = await _transactionRepo.GetByWalletIdAsync(wallet.WalletId);

            return transactions
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TransactionHistoryResponse
                {
                    TransactionId = t.TransactionId,
                    WalletId = t.WalletId,
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
                })
                .ToList();
        }

        public async Task<bool> ProcessTopUpAsync(TopUpRequest request)
        {
            int accountId = _currentUserService.GetRequiredUserId();

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
                if (wallet == null)
                    throw new Exception("Ví không tồn tại.");

                var payment = new Payment
                {
                    AccountId = accountId,
                    OrderCode = request.OrderCode,
                    Provider = request.Provider,
                    Amount = request.Amount,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                };

                await _paymentRepo.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                decimal balanceBefore = wallet.Balance;
                wallet.Balance += request.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                var trans = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = payment.PaymentId,
                    Amount = request.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Credit",
                    ReferenceType = "TopUp",
                    ReferenceId = payment.PaymentId,
                    Description = $"Nạp tiền qua {request.Provider}",
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(trans);
                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    SenderId = accountId,
                    TargetUserId = accountId,
                    Title = "Nạp ví thành công",
                    Content = $"Bạn đã nạp thành công {request.Amount:N0} VND vào ví.",
                    Type = "WalletTopUp"
                });

                await _notificationService.SendWalletUpdatedAsync(accountId, new
                {
                    WalletId = wallet.WalletId,
                    Balance = wallet.Balance,
                    UpdatedAt = wallet.UpdatedAt
                });

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Lỗi nạp tiền: {ex.Message}");
            }
        }

        public async Task<WalletDashboardResponse> GetWalletDashboardAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
            if (wallet == null) throw new Exception("Ví không tồn tại.");

            var transactions = await _transactionRepo.GetByWalletIdAsync(wallet.WalletId);

            var response = new WalletDashboardResponse();

            // 1. Map Stats (Dữ liệu cho WalletStats.js)
            response.Stats = new List<StatCardDto>
    {
        new StatCardDto {
            Label = "Số dư khả dụng",
            Value = wallet.Balance.ToString("N0"),
            Sub = "Coins",
            Icon = "Wallet"
        },
        new StatCardDto {
            Label = "Số dư đóng băng",
            Value = wallet.LockedBalance.ToString("N0"),
            Sub = "Coins",
            Icon = "Lock"
        },
        new StatCardDto {
            Label = "Tổng chi tiêu",
            Value = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount).ToString("N0"),
            Sub = "Coins",
            Icon = "ArrowUpRight"
        }
    };

            // 2. Map Transactions (Dữ liệu cho WalletTransactionTable.js)
            response.Transactions = transactions.Select(t => new TransactionDto
            {
                Id = $"GD{t.TransactionId:D5}",
                Detail = t.Description,
                Date = t.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                Amount = t.Amount,
                Type = t.Type?.ToLower() == "deposit" ? "deposit" : "expense",
                Status = t.Status // FE sẽ dùng .toLowerCase() để map class CSS
            }).ToList();

            return response;
        }
    }
}