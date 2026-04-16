

namespace Application.Interfaces
{
    public interface IUnitOfWork: IDisposable
    {
        Task<int> SaveChangesAsync();

        Task BeginTransactionAsync();

        Task CommitAsync();

        Task RollbackAsync();

        void Detach<T>(T entity) where T : class;
    }
}