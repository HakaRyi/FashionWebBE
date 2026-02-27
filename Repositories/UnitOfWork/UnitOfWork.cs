using Repositories.Data;

namespace Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FashionDbContext _context;

        public UnitOfWork(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}