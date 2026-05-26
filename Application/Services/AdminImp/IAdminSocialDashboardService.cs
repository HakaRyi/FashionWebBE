using Domain.Contracts.Admin;

namespace Application.Services.AdminImp;

public interface IAdminSocialDashboardService
{
    Task<AdminUserDashboardDto> GetUserDashboardAsync();
    Task<AdminPostDashboardDto> GetPostDashboardAsync();
}