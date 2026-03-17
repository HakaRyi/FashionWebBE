using Repositories.Entities;
using Repositories.Repos.WalletRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.Payments;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Response.WalletResp;
using Services.Request.WalletReq;

namespace Services.Implements.WalletImp
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletRepository _walletRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ICurrentUserService _currentUserService;

        public WalletService(
            IUnitOfWork unitOfWork,
            IWalletRepository walletRepo,
            ITransactionRepository transactionRepo,
            IPaymentRepository paymentRepo,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _walletRepo = walletRepo;
            _transactionRepo = transactionRepo;
            _paymentRepo = paymentRepo;
            _currentUserService = currentUserService;
        }

        public async Task<WalletResponse> GetMyWalletAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);

            if (wallet == null)
                throw new Exception("Không tìm thấy ví người dùng.");

            // Mapping từ Entity sang DTO tại Service
            return new WalletResponse
            {
                WalletId = wallet.WalletId,
                Balance = wallet.Balance,
                Currency = wallet.Currency,
                UpdatedAt = wallet.UpdatedAt
            };
        }

        public async Task<IEnumerable<Transaction>> GetMyTransactionHistoryAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
            if (wallet == null) throw new Exception("Ví không tồn tại.");

            return await _transactionRepo.GetByWalletIdAsync(wallet.WalletId);
        }

        public async Task<bool> ProcessTopUpAsync(TopUpRequest request)
        {
            int accountId = _currentUserService.GetRequiredUserId();

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var wallet = await _walletRepo.GetByAccountIdAsync(accountId);
                if (wallet == null) throw new Exception("Ví không tồn tại.");

                // 1. Mapping DTO sang Entity Payment
                var payment = new Payment
                {
                    AccountId = accountId,
                    OrderCode = request.OrderCode,
                    Provider = request.Provider,
                    Status = "Completed",
                    CreatedAt = DateTime.Now,
                    PaidAt = DateTime.Now
                };
                await _paymentRepo.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync(); // Để lấy PaymentId

                // 2. Cập nhật số dư ví
                decimal balanceBefore = wallet.Balance;
                wallet.Balance += request.Amount;
                wallet.UpdatedAt = DateTime.Now;
                _walletRepo.Update(wallet);

                // 3. Tạo Transaction log (Ghi vết biến động số dư)
                var trans = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = payment.PaymentId,
                    Amount = request.Amount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = "Deposit",
                    ReferenceType = "Payment",
                    ReferenceId = payment.PaymentId,
                    Description = $"Nạp tiền qua {request.Provider}",
                    Status = "Success",
                    CreatedAt = DateTime.Now
                };
                await _transactionRepo.AddAsync(trans);

                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception($"Lỗi nạp tiền: {ex.Message}");
            }
        }
    }
}