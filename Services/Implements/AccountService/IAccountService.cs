using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Services.Request.AccountReq;
using Services.Response.AccountRep;

namespace Services.Implements.AccountService
{
    public interface IAccountService
    {
        Task<List<AccountResponse>> GetListAccount();
        Task<List<FashionExpertResponse>> GetFashionExpert();
        Task<FashionExpertDetail> GetFashionExpertDetail(int id);
        Task<AccountResponse?> GetAccountById(int accountId);
        Task<string> updateAccountRequest(int accountId, UpdateAccountRequest request);
        Task<int> CountAccount();
        Task<int> CountExpert();
    }
}
