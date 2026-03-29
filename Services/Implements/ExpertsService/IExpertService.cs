using Repositories.Entities;
using Services.Request.ExpertReq;
using Services.Response.ExpertResp;

namespace Services.Implements.Experts
{
    public interface IExpertService
    {
        Task<IEnumerable<ExpertManagementByAdminDto>> GetAllExpertsAsync();
        Task<bool> RegisterExpertAsync(ExpertRegistrationDto dto);
        Task<string> GetCurrentApplicationStatusAsync();
        Task<ExpertProfile?> GetProfileByAccountId(int accountId);
        Task<IEnumerable<Repositories.Entities.ExpertRequest>> GetPendingApplicationsAsync();
        Task<IEnumerable<ExpertManagementDto>> GetActiveExpertsForUserAsync();
        Task<ExpertManagementDto?> GetExpertPublicProfileAsync(int profileId);
        Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback);
        Task<bool> ProcessApplicationAsync(int fileId, string status, string? reason);
        Task<IEnumerable<ExpertProfile>> GetAllVerifiedExpertsAsync();
        Task<bool> DeleteExpertProfileAsync(int profileId);
    }
}
