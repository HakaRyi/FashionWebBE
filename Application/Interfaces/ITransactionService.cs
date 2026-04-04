using Application.Response.TransactionResp;

namespace Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> GetById(int id);
        Task<List<TransactionResponse>> GetTransactions();
    }
}
