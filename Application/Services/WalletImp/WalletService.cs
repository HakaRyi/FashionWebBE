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

            if (request == null)
                throw new Exception("Dữ liệu nạp tiền không hợp lệ.");

            if (request.Amount <= 0)
                throw new Exception("Số tiền nạp phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(request.OrderCode))
                throw new Exception("Mã giao dịch không hợp lệ.");

            if (string.IsNullOrWhiteSpace(request.Provider))
                throw new Exception("Nhà cung cấp thanh toán không hợp lệ.");

            await _unitOfWork.BeginTransactionAsync();

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
                    Description = $"Nạp tiền qua {request.Provider}",
                    Status = TransactionStatus.Success,
                    CreatedAt = DateTime.UtcNow
                };

                await _transactionRepo.AddAsync(transaction);

                await _unitOfWork.CommitAsync();

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

            // 1. Gọi Repo lấy ví
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
            if (wallet == null) throw new KeyNotFoundException("The wallet doesn't exist.");

            // 2. Định nghĩa các loại cần lấy (Nạp/Rút)
            var walletTypes = new List<string> {
                TransactionType.Credit,
                TransactionType.Debit
            };

            // 3. Gọi Repo lấy giao dịch
            var transactions = await _walletRepo.GetWalletTransactionsAsync(wallet.WalletId, walletTypes);

            // 4. Map dữ liệu sang DTO
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