using Repositories.Dto.Common;
using Repositories.Dto.Social.Report;

namespace Services.Implements.Report
{
    public interface IUserReportService
    {
        Task<PostReportDto> ReportPostAsync(int postId, int accountId, CreatePostReportDto request);

        Task<List<ReportTypeDto>> GetReportTypesAsync();

        Task<PagedResultDto<AdminReportListItemDto>> GetAdminReportsAsync(string? status, int pageNumber, int pageSize);

        Task<AdminReportDetailDto> GetAdminReportDetailAsync(int userReportId);

        Task<AdminReportDetailDto> UpdateReportStatusAsync(int userReportId, int adminAccountId, UpdateReportStatusDto request);
    }
}