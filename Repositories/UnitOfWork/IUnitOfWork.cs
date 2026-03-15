using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.Events;
using Repositories.Repos.Payments;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;

namespace Repositories.UnitOfWork
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();

        Task<IDbContextTransaction> BeginTransactionAsync();

        Task CommitAsync();
        Task RollbackAsync();
    }
}