using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Data;
using Repositories.Repos.EscrowSessionRepos;
using Repositories.Repos.Events;
using Repositories.Repos.Payments;
using Repositories.Repos.PrizeEventRepos;
using Repositories.Repos.TransactionRepos;
using Repositories.Repos.WalletRepos;
using Repositories.Repos;

namespace Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FashionDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null) await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}