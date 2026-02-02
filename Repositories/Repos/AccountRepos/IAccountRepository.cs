using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.AccountRepos
{
    public interface IAccountRepository
    {
        Task<int> SignUp(Account account);
        Task<Account?> SignIn(string email, string password);
        Task<Account?> GetAccountById(int accountId);
        Task<Account?> GetAccountByEmail(string email);
        Task<List<Account>> GetAllAccounts();
        Task<bool> UpdateAccount(Account account);
    }
}
