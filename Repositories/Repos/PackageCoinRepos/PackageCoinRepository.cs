using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.PackageCoinRepos
{
    public class PackageCoinRepository : IPackageCoinRepository
    {
        private readonly FashionDbContext _db;
        public PackageCoinRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreatePackage(Package package)
        {
            _db.Packages.Add(package);
            return await _db.SaveChangesAsync();
        }

        public async Task<Package> GetById(int id)
        {
            return await _db.Packages
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p=>p.PackageId==id);
        }

        public async Task<List<Package>> GetPackages()
        {
            return await _db.Packages
                .OrderBy(p=>p.PriceVnd)
                .ToListAsync();
        }
        public async Task<int> UpdatePackage(Package package)
        {
            _db.Packages.Update(package);
            return await _db.SaveChangesAsync();
        }
    }
}
