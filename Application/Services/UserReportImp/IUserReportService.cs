using Application.Request.UserReportReq;
using Application.Response.UserReportResp;
using Domain.Contracts.Common;
using Domain.Contracts.UserReport;

namespace Application.Services.UserReportImp
{
    public interface IUserReportService
    {
        Task<CreateUserReportResponseDto> CreateReportAsync(CreateUserReportRequestDto request, int currentUserId);

        Task<List<ReportTypeDto>> GetReportTypesAsync();

        Task<PagedResultDto<AdminReportListItemDto>> GetAdminReportsAsync(
            string? status,
            int pageNumber,
            int pageSize);

        Task<AdminReportDetailDto?> GetAdminReportDetailAsync(int userReportId);

        Task<ReviewUserReportResponseDto> ReviewReportAsync(
            int userReportId,
            ReviewUserReportRequestDto request,
            int adminId);
    }
}