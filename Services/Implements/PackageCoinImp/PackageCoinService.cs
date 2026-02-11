using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Repositories.Repos.PackageCoinRepos;
using Services.Request.PackageReq;
using Services.Response.PackageResp;

namespace Services.Implements.PackageCoinImp
{
    public class PackageCoinService : IPackageCoinService
    {
        private readonly IPackageCoinRepository packageCoinRepository;
        public PackageCoinService(IPackageCoinRepository packageCoinRepository)
        {
            this.packageCoinRepository = packageCoinRepository;
        }

        public async Task<int> CreateAsync(PackageRequest request, int accountCreatedId )
        {
            var package = new Package
            {
                Name = request.Name,
                CoinAmount = request.CoinAmount,
                PriceVnd = request.PriceVnd,
                IsActive = request.IsActive,
                AccountId = accountCreatedId, 
                CreatedAt = DateTime.UtcNow

            };
            return await packageCoinRepository.CreatePackage(package);
        }

        public async Task<List<CoinPackageResponse>> GetAll()
        {
            var packages = await packageCoinRepository.GetPackages();
            var response = packages.Select(p => new CoinPackageResponse
            {
                CoinPackageId = p.PackageId,
                PackageName = p.Name,
                CoinAmount = p.CoinAmount,
                Price = p.PriceVnd,
                CreatedAt = p.CreatedAt
            }).ToList();
            return response;
        }

        public async Task<CoinPackageDetail> GetById(int id)
        {
            var package = await packageCoinRepository.GetById(id);
            if (package == null)
            {
                return null;
            }
            var response = new CoinPackageDetail
            {
                PackageId = package.PackageId,
                Name =  package.Name,
                CoinAmount = package.CoinAmount,
                PriceVnd = package.PriceVnd,
                IsActive = package.IsActive,
                CreatedAt = package.CreatedAt,
                CreateBy = package.Account.Username,
                AccountId = package.AccountId
            };
            return response;

        }

        public async Task<int> UpdateAsync(PackageRequest request, int id)
        {
            var package = await packageCoinRepository.GetById(id);
            if (package == null)
            {
                return -1;
            }
            package.Name = request.Name;
            package.CoinAmount = request.CoinAmount;
            package.PriceVnd = request.PriceVnd;
            package.IsActive = request.IsActive;
            return await packageCoinRepository.UpdatePackage(package);
        }
        public async Task<string> DeleteAsync(int id)
        {
            var package = await packageCoinRepository.GetById(id);
            if (package == null)
            {
                return "ko thay";
            }
            package.IsActive = false;
            var result =  await packageCoinRepository.UpdatePackage(package);
            if (result > 0)
            {
                return "success";
            }
            else
            {
                return "fail";
            }
        }
    }
}
