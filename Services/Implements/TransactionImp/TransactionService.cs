using Repositories.Repos.TransactionRepos;
using Services.Response.TransactionResp;
using Repositories.Entities;

namespace Services.Implements.TransactionImp
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<TransactionResponse?> GetById(int id)
        {
            var t = await _transactionRepository.GetById(id);
            if (t == null) return null;

            return MapToResponse(t);
        }

        public async Task<List<TransactionResponse>> GetTransactions()
        {
            var transactions = await _transactionRepository.GetTransactions();
            return transactions.Select(t => MapToResponse(t)).ToList();
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