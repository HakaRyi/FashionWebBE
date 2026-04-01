using Repositories.Constants;
using Repositories.Entities;
using Repositories.Repos.PaymentsRespo;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.UnitOfWork;
using Services.Helpers;
using Services.Implements.NotificationImp;
using Services.Request.NotificationReq;

namespace Services.Implements.PaymentService
{
    public class TopUpPaymentProcessor : ITopUpPaymentProcessor
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public TopUpPaymentProcessor(
            IPaymentRepository paymentRepo,
            ITransactionRepository transactionRepo,
            IWalletRepository walletRepo,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _walletRepo = walletRepo;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<bool> ProcessAsync(string orderCode, bool isSuccess)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _paymentRepo.GetPaymentWithWalletAsync(orderCode);
                if (payment == null) return false;
                if (payment.Status != PaymentStatus.Pending) return false;

                if (!isSuccess)
                {
                    payment.Status = PaymentStatus.Failed;
                    _paymentRepo.Update(payment);
                    await _unitOfWork.CommitAsync();
                    return false;
                }

                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                _paymentRepo.Update(payment);

                var wallet = payment.Account?.Wallet;
                if (wallet == null)
                    throw new Exception("Ví không tồn tại.");

                decimal balanceBefore = wallet.Balance;
                wallet.Balance += payment.Amount;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                await _transactionRepo.AddAsync(new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = payment.PaymentId,
                    TransactionCode = PaymentCodeGenerator.GenerateTransactionCode("TRX"),
                    Amount = payment.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Credit,
                    ReferenceType = TransactionReferenceType.TopUp,
                    ReferenceId = payment.PaymentId,
                    Description = $"Nạp tiền qua {payment.Provider} - Order {payment.OrderCode}",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                });

                await _unitOfWork.CommitAsync();

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    SenderId = payment.AccountId,
                    TargetUserId = payment.AccountId,
                    Title = "Nạp ví thành công",
                    Content = $"Bạn đã nạp thành công {payment.Amount:N0} VND vào ví.",
                    Type = "WalletTopUp"
                });

                await _notificationService.SendWalletUpdatedAsync(payment.AccountId, new
                {
                    wallet.WalletId,
                    wallet.Balance,
                    wallet.LockedBalance,
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
    }
}