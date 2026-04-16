using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FashionDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null) return;
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();

                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
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

        public void Detach<T>(T entity) where T : class
        {
            var entry = _context.Entry(entity);
            var primaryKey = entry.Metadata.FindPrimaryKey();

            if (primaryKey != null)
            {
                var keyValues = primaryKey.Properties
                    .ToDictionary(p => p.Name, p => entry.Property(p.Name).CurrentValue);

                var alreadyTracked = _context.ChangeTracker.Entries<T>()
                    .FirstOrDefault(e =>
                        primaryKey.Properties.All(p =>
                            object.Equals(e.Property(p.Name).CurrentValue, keyValues[p.Name])
                        )
                    );

                if (alreadyTracked != null)
                {
                    alreadyTracked.State = EntityState.Detached;
                }
            }

            entry.State = EntityState.Detached;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}