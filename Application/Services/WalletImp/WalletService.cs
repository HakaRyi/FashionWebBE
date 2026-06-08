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
                throw new KeyNotFoundException("User wallet not found.");

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
                throw new KeyNotFoundException("Wallet does not exist.");

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
            if (request == null)
                throw new ArgumentNullException(nameof(request), "Top-up data is invalid.");

            if (request.Amount <= 0)
                throw new ArgumentException("Top-up amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(request.OrderCode))
                throw new ArgumentException("Invalid transaction order code.");

            if (string.IsNullOrWhiteSpace(request.Provider))
                throw new ArgumentException("Invalid payment provider.");

            int accountId = _currentUserService.GetRequiredUserId();
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
                if (wallet == null)
                    throw new KeyNotFoundException("Wallet does not exist.");

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
                    Description = $"Top-up via {request.Provider}",
                    Status = TransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(transaction);
                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    SenderId = accountId,
                    TargetUserId = accountId,
                    Title = "Wallet Top-up Successful",
                    Content = $"You have successfully topped up {request.Amount:N0} {wallet.Currency} into your wallet.",
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
                throw new KeyNotFoundException("Wallet does not exist.");

            var walletTypes = new List<string>
            {
                TransactionType.Credit,
                TransactionType.Debit
            };

            var transactions = await _walletRepo.GetWalletTransactionsAsync(wallet.WalletId, walletTypes);

            return new WalletDashboardResponse
            {
                Wallet = new WalletSummaryDto
                {
                    Balance = wallet.Balance,
                    LockedBalance = wallet.LockedBalance,
                    Currency = wallet.Currency
                },
                Transactions = transactions.Select(t => new WalletTransactionDto
                {
                    TransactionId = t.TransactionId,
                    TransactionCode = t.TransactionCode,
                    Amount = t.Amount,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Type = t.Type,
                    ReferenceType = t.ReferenceType,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    Status = t.Status,
                    PaymentProvider = t.Payment?.Provider
                }).ToList()
            };
        }

        private static string GenerateTransactionCode(string prefix)
        {
            return $"{prefix}-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}