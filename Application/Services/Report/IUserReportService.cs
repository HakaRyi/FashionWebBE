using Domain.Dto.Common;
using Domain.Dto.Social.Report;

namespace Application.Services.Report
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