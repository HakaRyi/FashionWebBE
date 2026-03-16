using Repositories.Dto.Common;
using Repositories.Dto.Social.Report;
using Repositories.Entities;

namespace Repositories.Repos.Report
{
    public interface IUserReportRepository
    {
        Task<PostReportValidationInfoDto?> GetPostValidationInfoAsync(int postId);

        Task<ReportType?> GetReportTypeByIdAsync(int reportTypeId);

        Task<bool> IsAlreadyReportedAsync(int postId, int accountId);

        Task AddAsync(UserReport report);

        Task<List<ReportTypeDto>> GetReportTypesAsync();

        Task<UserReport?> GetByIdAsync(int userReportId);

        Task<AdminReportDetailDto?> GetAdminReportDetailAsync(int userReportId);

        Task<PagedResultDto<AdminReportListItemDto>> GetAdminReportsAsync(
            string? status,
            int pageNumber,
            int pageSize);

        void Update(UserReport report);
    }
}