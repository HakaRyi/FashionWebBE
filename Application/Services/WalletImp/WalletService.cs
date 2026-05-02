using Application.Interfaces;
using Application.Request.NotificationReq;
using Application.Request.WalletReq;
using Application.Response.TransactionResp;
using Application.Response.WalletResp;
using Application.Services.NotificationImp;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.WalletImp
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
                throw new Exception("User wallet not found.");

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
                throw new Exception("Wallet not found.");

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

            if (request == null)
                throw new Exception("Invalid top-up data.");

            if (request.Amount <= 0)
                throw new Exception("Top-up amount must be greater than 0.");

            if (string.IsNullOrWhiteSpace(request.OrderCode))
                throw new Exception("Invalid transaction code.");

            if (string.IsNullOrWhiteSpace(request.Provider))
                throw new Exception("Invalid payment provider.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
                if (wallet == null)
                    throw new Exception("Wallet not found.");

                var payment = new Payment
                {
                    AccountId = accountId,
                    OrderCode = request.OrderCode,
                    Provider = request.Provider,
                    Amount = request.Amount,
                    Status = PaymentStatus.Success,
                    CreatedAt = DateTime.UtcNow,
                    PaidAt = DateTime.UtcNow
                };

                await _paymentRepo.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                decimal balanceBefore = wallet.Balance;

                wallet.Balance += request.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = payment.PaymentId,
                    TransactionCode = GenerateTransactionCode("TOPUP"),
                    Amount = request.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.TopUp,
                    ReferenceId = payment.PaymentId,
                    Description = $"Top up via {request.Provider}",
                    Status = TransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(transaction);

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    SenderId = accountId,
                    TargetUserId = accountId,
                    Title = "Wallet top-up successful",
                    Content = $"You have successfully topped up {request.Amount:N0} VND to your wallet.",
                    Type = "WalletTopUp"
                });

                await _notificationService.SendWalletUpdatedAsync(accountId, new
                {
                    wallet.WalletId,
                    wallet.Balance,
                    wallet.UpdatedAt
                });

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<WalletDashboardResponse> GetWalletDashboardAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);

            if (wallet == null)
                throw new Exception("Wallet not found.");

            var transactions = await _transactionRepo.GetByWalletIdAsync(wallet.WalletId);

            decimal totalExpense = transactions
                .Where(t => t.Type == TransactionType.Debit
                         && t.Status == TransactionStatus.Success)
                .Sum(t => t.Amount);

            var response = new WalletDashboardResponse
            {
                Stats = new List<StatCardDto>
                {
                    new StatCardDto
                    {
                        Label = "Available Balance",
                        Value = wallet.Balance.ToString("N0"),
                        Sub = "Coins",
                        Icon = "Wallet"
                    },
                    new StatCardDto
                    {
                        Label = "Locked Balance",
                        Value = wallet.LockedBalance.ToString("N0"),
                        Sub = "Coins",
                        Icon = "Lock"
                    },
                    new StatCardDto
                    {
                        Label = "Total Spending",
                        Value = totalExpense.ToString("N0"),
                        Sub = "Coins",
                        Icon = "ArrowUpRight"
                    }
                },
                Transactions = transactions.Select(t =>
                {
                    bool isPositive = t.Type == TransactionType.Credit;

                    return new TransactionDto
                    {
                        Id = $"GD{t.TransactionId:D5}",
                        Detail = t.Description,
                        Date = t.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        Amount = t.Amount,
                        Type = isPositive ? "deposit" : "expense",
                        Status = t.Status
                    };
                }).ToList()
            };

            return response;
        }

        private static string GenerateTransactionCode(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}