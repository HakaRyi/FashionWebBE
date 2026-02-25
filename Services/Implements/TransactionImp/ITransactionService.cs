using Services.Response.TransactionResp;

namespace Services.Implements.TransactionImp
{
    public interface ITransactionService
    {
        Task<TransactionResponse> GetById(int id);
        Task<List<TransactionResponse>> GetTransactions();
    }
}
