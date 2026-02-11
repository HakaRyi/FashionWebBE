using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.TransactionRepos
{
    public interface ITransactionRepository
    {
        Task<Transaction> GetById(int id);
        Task<List<Transaction>> GetTransactions();
    }
}
