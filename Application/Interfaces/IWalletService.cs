using Application.Request.WalletReq;
using Application.Response.TransactionResp;
using Application.Response.WalletResp;

namespace Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletResponse> GetMyWalletAsync();
        Task<List<TransactionHistoryResponse>> GetMyTransactionHistoryAsync();
        Task<bool> ProcessTopUpAsync(TopUpRequest request);
        Task<WalletDashboardResponse> GetWalletDashboardAsync();
    }
}