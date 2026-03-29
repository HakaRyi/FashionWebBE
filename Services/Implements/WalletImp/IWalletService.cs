using Services.Request.WalletReq;
using Services.Response.TransactionResp;
using Services.Response.WalletResp;

namespace Services.Implements.WalletImp
{
    public interface IWalletService
    {
        Task<WalletResponse> GetMyWalletAsync();
        Task<List<TransactionHistoryResponse>> GetMyTransactionHistoryAsync();
        Task<bool> ProcessTopUpAsync(TopUpRequest request);
        Task<WalletDashboardResponse> GetWalletDashboardAsync();
    }
}