using Repositories.Entities;
using Repositories.Repos.AccountSubscriptionRepos;
using Repositories.Repos.Payments;
using Repositories.Repos.TransactionRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.PaymentReq;
using Services.Response.PaymentResp;

namespace Services.Implements.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IAccountSubscriptionRepository _subscriptionRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public PaymentService(
            IPaymentRepository paymentRepo,
            ITransactionRepository transactionRepo,
            IAccountSubscriptionRepository subscriptionRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _subscriptionRepo = subscriptionRepo;
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
        }

        public async Task<PaymentResponse?> CreatePackagePaymentAsync(PaymentRequest request)
        {
            int accountId = _currentUserService.GetRequiredUserId();

            var package = (await _paymentRepo.GetActivePackagesAsync())
                .FirstOrDefault(p => p.PackageId == request.PackageId);

            if (package == null)
                throw new Exception("Gói dịch vụ không khả dụng.");

            var orderCode = $"PKG-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var payment = new Payment
            {
                AccountId = accountId,
                PackageId = request.PackageId,
                Amount = package.Price,
                Provider = "VnPay",
                OrderCode = orderCode,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResponse
            {
                OrderCode = orderCode,
                Amount = payment.Amount,
                Description = $"Mua gói: {package.Name}",
                Status = payment.Status
            };
        }

        public async Task<PaymentResponse?> CreateTopUpPaymentAsync(decimal amount)
        {
            if (amount < 10000) throw new Exception("Số tiền tối thiểu là 10,000đ.");

            int accountId = _currentUserService.GetRequiredUserId();
            var orderCode = $"TOP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var payment = new Payment
            {
                AccountId = accountId,
                PackageId = null,
                Amount = amount,
                Provider = "VnPay",
                OrderCode = orderCode,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResponse
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = "Nạp tiền vào ví",
                Status = payment.Status
            };
        }

        public async Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess)
        {
            using var transactionScope = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var payment = await _paymentRepo.GetPaymentWithWalletAsync(orderCode);
                if (payment == null || payment.Status != "Pending") return false;

                if (isSuccess)
                {
                    payment.Status = "Success";
                    payment.PaidAt = DateTime.UtcNow;

                    var wallet = payment.Account?.Wallet;
                    if (wallet == null) throw new Exception("Ví không tồn tại.");

                    decimal balanceBefore = wallet.Balance;
                    wallet.Balance += payment.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;

                    // 1. Tạo Transaction Log
                    var transactionEntry = new Transaction
                    {
                        WalletId = wallet.WalletId,
                        PaymentId = payment.PaymentId,
                        Amount = payment.Amount,
                        BalanceBefore = balanceBefore,
                        BalanceAfter = wallet.Balance,
                        Type = "Deposit",
                        ReferenceType = payment.PackageId.HasValue ? "Subscription" : "TopUp",
                        ReferenceId = payment.PaymentId,
                        Description = payment.PackageId.HasValue
                            ? $"Thanh toán gói {payment.Package?.Name}"
                            : "Nạp tiền vào ví qua VnPay",
                        CreatedAt = DateTime.UtcNow,
                        Status = "Success"
                    };
                    await _transactionRepo.AddAsync(transactionEntry);

                    // 2. Logic Gia hạn/Kích hoạt Gói hội viên
                    if (payment.PackageId.HasValue && payment.Package != null)
                    {
                        // Kiểm tra xem khách có đang trong thời hạn gói nào không
                        var latestSub = await _subscriptionRepo.GetLatestActiveSubscriptionAsync(payment.AccountId);

                        // Nếu còn hạn thì bắt đầu từ lúc hết hạn cũ, nếu không thì bắt đầu từ bây giờ
                        DateTime startDate = (latestSub != null && latestSub.EndDate > DateTime.UtcNow)
                                            ? latestSub.EndDate
                                            : DateTime.UtcNow;

                        var newSub = new AccountSubscription
                        {
                            AccountId = payment.AccountId,
                            PackageId = payment.PackageId.Value,
                            StartDate = startDate,
                            EndDate = startDate.AddDays(payment.Package.DurationDays),
                            IsActive = true
                        };
                        await _subscriptionRepo.AddAsync(newSub);
                    }
                }
                else
                {
                    payment.Status = "Failed";
                }

                await _unitOfWork.SaveChangesAsync();
                await transactionScope.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transactionScope.RollbackAsync();
                return false;
            }
        }
    }
}