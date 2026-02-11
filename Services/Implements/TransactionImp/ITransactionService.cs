using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Response.TransactionResp;

namespace Services.Implements.TransactionImp
{
    public interface ITransactionService
    {
        Task<TransactionResponse> GetById(int id);
        Task<List<TransactionResponse>> GetTransactions();
    }
}
