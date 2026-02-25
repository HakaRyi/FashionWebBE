using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Request.PackageReq;
using Services.Response.PackageResp;

namespace Services.Implements.PackageCoinImp
{
    public interface IPackageCoinService
    {
        Task<CoinPackageDetail> GetById(int id);
        Task<List<CoinPackageResponse>> GetAll();
        Task<int> CreateAsync(PackageRequest request, int accountCreatedId);
        Task<int> UpdateAsync(PackageRequest request,int id);
        Task<string> DeleteAsync(int id);
    }
}
