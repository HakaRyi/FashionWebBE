using Repositories.Entities;
using Services.Request.WalletReq;
using Services.Response.WalletResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.WalletImp
{
    public interface IWalletService
    {
        Task<WalletResponse> GetMyWalletAsync();
        Task<IEnumerable<Transaction>> GetMyTransactionHistoryAsync();
        Task<bool> ProcessTopUpAsync(TopUpRequest request);
        Task<WalletDashboardResponse> GetWalletDashboardAsync();
    }
}
