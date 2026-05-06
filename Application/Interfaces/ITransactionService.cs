using Application.Response.EscrowSessionResp;
using Application.Response.TransactionResp;

namespace Application.Interfaces
{
    public interface ITransactionService
    {

        Task<TransactionResponse> GetById(int id);
        Task<List<TransactionResponse>> GetTransactions();
        Task<List<TransactionResponse>> GetTransactionsByReferenceAsync(string refType, int refId);
        Task<List<TransactionResponse>> AdminGetAllTransactionsAsync(string? type = null, string? refType = null, int? refId = null);
        Task<List<TransactionResponse>> ExpertGetHistoryAsync();
        Task<List<EscrowResponse>> AdminGetEscrowManagementAsync();
        Task<List<EscrowResponse>> ExpertGetEscrowManagementAsync();
        Task AdminRequestFixLeakAsync(int escrowSessionId, string reason);
        Task ExpertApproveFixAsync(int escrowSessionId);
        Task AdminExecuteUpdateWalletAsync(int escrowSessionId);
    }
}
