using Application.Interfaces;
using Domain.Contracts.Admin;
using Domain.Interfaces;

namespace Application.Services.AdminImp;

public class AdminSocialDashboardService
    : IAdminSocialDashboardService
{
    private readonly IAdminSocialDashboardRepository
        _dashboardRepository;

    public AdminSocialDashboardService(
        IAdminSocialDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    #region OVERVIEW

    public async Task<SocialDashboardOverviewResponse>
        GetOverviewAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository
            .GetOverviewAsync(cancellationToken);
    }

    #endregion

    #region CHARTS

    public async Task<DashboardChartsResponse>
        GetChartsAsync(
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository
            .GetChartsAsync(
                startDate,
                endDate,
                cancellationToken);
    }

    #endregion

    #region ANALYTICS

    public async Task<DashboardAnalyticsResponse>
        GetAnalyticsAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository
            .GetAnalyticsAsync(cancellationToken);
    }

    #endregion

    #region RECENT

    public async Task<DashboardRecentResponse>
        GetRecentAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dashboardRepository
            .GetRecentAsync(cancellationToken);
    }

    #endregion
}