using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.AccountRepos
{
    public class AccountRepository : IAccountRepository
    {

        public Task<Account?> GetAccountByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public Task<Account?> GetAccountById(int accountId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Account>> GetAllAccounts()
        {
            throw new NotImplementedException();
        }

        public Task<Account?> SignIn(string email, string password)
        {
            throw new NotImplementedException();
        }

        public Task<int> SignUp(Account account)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAccount(Account account)
        {
            throw new NotImplementedException();
        }
    }
}
