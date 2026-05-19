using Domain.Contracts.Admin;

namespace Application.Services.AdminImp;

public interface IAdminSocialDashboardService
{
    Task<SocialDashboardOverviewResponse> GetOverviewAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardChartsResponse> GetChartsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<DashboardAnalyticsResponse> GetAnalyticsAsync(
        CancellationToken cancellationToken = default);

    Task<DashboardRecentResponse> GetRecentAsync(
        CancellationToken cancellationToken = default);
}