using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.PackageCoinRepos
{
    public interface IPackageCoinRepository
    {
        Task<int> CreatePackage(Package package);
        Task<int> UpdatePackage(Package package);
        Task<Package> GetById(int id);
        Task<List<Package>> GetPackages();

    }
}
