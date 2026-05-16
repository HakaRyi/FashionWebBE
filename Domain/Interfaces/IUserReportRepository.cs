using Domain.Contracts.Common;
using Domain.Contracts.UserReport;
using Domain.Entities;

namespace Domain.Interfaces
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