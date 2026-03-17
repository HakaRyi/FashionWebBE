using Repositories.Entities;

namespace Repositories.Repos.PackageRepos
{
    public interface IPackageRepository
    {
        Task<Package?> GetById(int id);
        Task<List<Package>> GetActivePackages();
        Task<int> CreatePackage(Package package);
        Task<int> UpdatePackage(Package package);
        Task<bool> DeletePackage(int id);
    }
}
