using Domain.Contracts.Admin;

namespace Domain.Interfaces
{
    public interface IAdminSocialDashboardRepository
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
}