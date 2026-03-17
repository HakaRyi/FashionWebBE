using Services.Request.PackageReq;
using Services.Response.PackageResp;

namespace Services.Implements.PackageImp
{
    public interface IPackageService
    {
        Task<PackageDetailResponse> GetById(int id);
        Task<List<PackageResponse>> GetAll();
        Task<int> CreateAsync(PackageRequest request, int accountCreatedId);
        Task<int> UpdateAsync(PackageRequest request, int id);
        Task<string> DeleteAsync(int id);
    }
}
