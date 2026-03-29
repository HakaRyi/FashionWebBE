using Microsoft.EntityFrameworkCore.Storage;

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