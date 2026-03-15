using Repositories.Entities;
using Services.Request.ExpertReq;
using Services.Response.ExpertResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Experts
{
    public interface IExpertService
    {
        Task<IEnumerable<ExpertProfile>> GetAllExpertsAsync();
        Task<bool> RegisterExpertAsync(ExpertRegistrationDto dto);
        Task<ExpertProfile?> GetProfileByAccountId(int accountId);
        Task<IEnumerable<Repositories.Entities.ExpertRequest>> GetPendingApplicationsAsync();
        Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback);
        Task<bool> ProcessApplicationAsync(int fileId, string status, string? reason);
        Task<IEnumerable<ExpertProfile>> GetAllVerifiedExpertsAsync();
        Task<bool> DeleteExpertProfileAsync(int profileId);
    }
}
