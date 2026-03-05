using Repositories.Data;
using Repositories.Entities;
using Repositories.Repos.Payments;
using Repositories.Repos.TransactionRepos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PaymentService
{
    public class PaymentService: IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly ITransactionRepository _transactionRepo;
        private readonly FashionDbContext _db;

        public PaymentService(IPaymentRepository paymentRepo, ITransactionRepository transactionRepo, FashionDbContext db)
        {
            _paymentRepo = paymentRepo;
            _transactionRepo = transactionRepo;
            _db = db;
        }

        public async Task<string> CreatePaymentAsync(int accountId, int packageId)
        {
            var package = (await _paymentRepo.GetActivePackagesAsync()).FirstOrDefault(p => p.PackageId == packageId);
            if (package == null) return null;

            var orderCode = $"EXP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var payment = new Payment
            {
                AccountId = accountId,
                PackageId = packageId,
                Provider = "VnPay",
                OrderCode = orderCode,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepo.AddPaymentAsync(payment);
            await _paymentRepo.SaveChangesAsync();
            return orderCode;
        }

        public async Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess)
        {
            var payment = await _paymentRepo.GetByOrderCodeAsync(orderCode);
            if (payment == null || payment.Status != "Pending") return false;

            if (isSuccess)
            {
                payment.Status = "Success";
                payment.PaidAt = DateTime.UtcNow;

                var currentBalance = await _transactionRepo.GetCurrentBalanceAsync(payment.AccountId);
                int coinAmount = payment.Package?.CoinAmount ?? 0;

                var transaction = new Transaction
                {
                    AccountId = payment.AccountId,
                    PaymentId = payment.PaymentId,
                    AmountCoin = coinAmount,
                    Type = "Deposit",
                    BalanceAfter = currentBalance + coinAmount,
                    Description = $"Nạp Coin từ gói {payment.Package?.Name}",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Success"
                };

                await _transactionRepo.AddAsync(transaction);
            }
            else { payment.Status = "Failed"; }

            return await _paymentRepo.SaveChangesAsync();
        }
    }
}
