using Repositories.Repos.TransactionRepos;
using Services.Response.TransactionResp;

namespace Services.Implements.TransactionImp
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<TransactionResponse> GetById(int id)
        {
            var transaction = await _transactionRepository.GetById(id);
            if (transaction == null)
            {
                return null;
            }

            return new TransactionResponse
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId,
                AccountName = transaction.Account?.UserName,
                PaymentId = transaction.PaymentId,
                AmountCoin = transaction.AmountCoin,
                Type = transaction.Type,
                ReferenceType = transaction.ReferenceType,
                ReferenceId = transaction.ReferenceId,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status
            };
        }

        public async Task<List<TransactionResponse>> GetTransactions()
        {
            var transactions = await _transactionRepository.GetTransactions();

            return transactions.Select(transaction => new TransactionResponse
            {
                TransactionId = transaction.TransactionId,
                AccountId = transaction.AccountId,
                AccountName = transaction.Account?.UserName,
                PaymentId = transaction.PaymentId,
                AmountCoin = transaction.AmountCoin,
                Type = transaction.Type,
                ReferenceType = transaction.ReferenceType,
                ReferenceId = transaction.ReferenceId,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status
            }).ToList();
        }
    }
}