using Domain.Contracts.Admin;
using Domain.Interfaces;

namespace Application.Services.AdminImp;

public class AdminSocialDashboardService
    : IAdminSocialDashboardService
{
    private readonly IAdminUserDashboardRepository _dashboardRepository;
    private readonly IAdminPostDashboardRepository _postDashboardRepository;

    public AdminSocialDashboardService(
        IAdminUserDashboardRepository dashboardRepository, IAdminPostDashboardRepository postDashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
        _postDashboardRepository = postDashboardRepository;
    }

    public async Task<AdminUserDashboardDto> GetUserDashboardAsync()
    {
        return await _dashboardRepository.GetUserDashboardAsync();
    }

    public async Task<AdminPostDashboardDto> GetPostDashboardAsync()
    {
        return await _postDashboardRepository.GetPostDashboardAsync();
    }
}