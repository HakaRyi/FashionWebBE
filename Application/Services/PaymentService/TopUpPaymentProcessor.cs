using Application.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Application.Helpers;
using Application.Request.NotificationReq;
using Application.Services.NotificationImp;
using Domain.Interfaces;

namespace Application.Services.PaymentService
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
            if (string.IsNullOrWhiteSpace(orderCode))
                return false;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var payment = await _paymentRepo.GetPaymentWithWalletAsync(orderCode);
                if (payment == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }

                // Idempotent:
                // - Nếu payment đã success trước đó => coi như xử lý thành công
                // - Nếu payment đã failed/cancelled => không xử lý lại
                if (payment.Status == PaymentStatus.Success)
                {
                    await _unitOfWork.RollbackAsync();
                    return true;
                }

                if (payment.Status == PaymentStatus.Failed ||
                    payment.Status == PaymentStatus.Cancelled)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }

                if (payment.Status != PaymentStatus.Pending)
                {
                    await _unitOfWork.RollbackAsync();
                    return false;
                }

                if (!isSuccess)
                {
                    payment.Status = PaymentStatus.Failed;
                    _paymentRepo.Update(payment);

                    await _unitOfWork.CommitAsync();
                    return false;
                }

                var wallet = payment.Account?.Wallet;
                if (wallet == null)
                    throw new Exception("Ví không tồn tại.");

                decimal balanceBefore = wallet.Balance;

                payment.Status = PaymentStatus.Success;
                payment.PaidAt = DateTime.UtcNow;
                _paymentRepo.Update(payment);

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