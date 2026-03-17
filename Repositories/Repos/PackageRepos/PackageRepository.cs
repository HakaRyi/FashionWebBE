using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.PackageRepos
{
    public class PackageRepository : IPackageRepository
    {
        private readonly FashionDbContext _db;
        public PackageRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Package?> GetById(int id)
        {
            return await _db.Packages
                .Include(p => p.Account)
                .Include(p => p.PackageFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.PackageId == id);
        }

        public async Task<List<Package>> GetActivePackages()
        {
            return await _db.Packages
                .Where(p => p.IsActive == true)
                .Include(p => p.PackageFeatures)
                    .ThenInclude(pf => pf.Feature)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<int> CreatePackage(Package package)
        {
            await _db.Packages.AddAsync(package);
            return await _db.SaveChangesAsync();
        }

        public async Task<int> UpdatePackage(Package package)
        {
            _db.Packages.Update(package);
            return await _db.SaveChangesAsync();
        }

        public async Task<bool> DeletePackage(int id)
        {
            var package = await _db.Packages.FindAsync(id);
            if (package == null) return false;

            package.IsActive = false;
            _db.Packages.Update(package);

            return await _db.SaveChangesAsync() > 0;
        }
    }
}