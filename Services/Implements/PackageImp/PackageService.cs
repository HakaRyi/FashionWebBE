using Repositories.Entities;
using Repositories.Repos.PackageRepos;
using Services.Request.PackageReq;
using Services.Response.FeatureResp;
using Services.Response.PackageResp;

namespace Services.Implements.PackageImp
{
    public class PackageService : IPackageService
    {
        private readonly IPackageRepository _packageRepository;

        public PackageService(IPackageRepository packageRepository)
        {
            _packageRepository = packageRepository;
        }

        public async Task<int> CreateAsync(PackageRequest request, int accountCreatedId)
        {
            var package = new Package
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                DurationDays = request.DurationDays,
                IsActive = request.IsActive,
                AccountId = accountCreatedId,
                CreatedAt = DateTime.UtcNow
            };

            return await _packageRepository.CreatePackage(package);
        }

        public async Task<List<PackageResponse>> GetAll()
        {
            var packages = await _packageRepository.GetActivePackages();
            return packages.Select(p => new PackageResponse
            {
                PackageId = p.PackageId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                DurationDays = p.DurationDays,
                CreatedAt = p.CreatedAt,
                Features = p.PackageFeatures.Select(f => new FeatureResponse
                {
                    Code = f.Feature.FeatureCode,
                    Value = f.Value
                }).ToList()
            }).ToList();
        }

        public async Task<PackageDetailResponse?> GetById(int id)
        {
            var package = await _packageRepository.GetById(id);
            if (package == null) return null;

            return new PackageDetailResponse
            {
                PackageId = package.PackageId,
                Name = package.Name,
                Description = package.Description,
                Price = package.Price,
                DurationDays = package.DurationDays,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt,
                CreatorName = package.Account?.UserName ?? "Admin",
                Features = package.PackageFeatures.Select(f => new FeatureResponse
                {
                    Code = f.Feature.FeatureCode,
                    Name = f.Feature.Name,
                    Value = f.Value
                }).ToList()
            };
        }

        public async Task<int> UpdateAsync(PackageRequest request, int id)
        {
            var package = await _packageRepository.GetById(id);
            if (package == null) return -1;

            package.Name = request.Name;
            package.Description = request.Description;
            package.Price = request.Price;
            package.DurationDays = request.DurationDays;
            package.IsActive = request.IsActive;

            return await _packageRepository.UpdatePackage(package);
        }

        public async Task<string> DeleteAsync(int id)
        {
            var result = await _packageRepository.DeletePackage(id);
            return result ? "success" : "fail hoặc không tìm thấy";
        }
    }
}