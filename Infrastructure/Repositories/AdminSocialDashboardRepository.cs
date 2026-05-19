using Domain.Constants;
using Domain.Contracts.Admin;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminSocialDashboardRepository
    : IAdminSocialDashboardRepository
{
    private readonly FashionDbContext _db;

    public AdminSocialDashboardRepository(
        FashionDbContext db)
    {
        _db = db;

        _db.ChangeTracker.QueryTrackingBehavior =
            QueryTrackingBehavior.NoTracking;
    }

    #region OVERVIEW

    public async Task<SocialDashboardOverviewResponse> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var response = new SocialDashboardOverviewResponse();

        await LoadOverviewStatisticsAsync(
            response,
            cancellationToken);

        await LoadTodayStatisticsAsync(
            response,
            cancellationToken);

        await LoadPeriodStatisticsAsync(
            response,
            cancellationToken);

        await LoadGrowthStatisticsAsync(
            response,
            cancellationToken);

        await LoadNotificationStatisticsAsync(
            response,
            cancellationToken);

        return response;
    }

    private async Task LoadOverviewStatisticsAsync(
        SocialDashboardOverviewResponse response,
        CancellationToken cancellationToken)
    {
        response.Overview.TotalUsers =
            await _db.Accounts
                .CountAsync(cancellationToken);

        response.Overview.TotalPosts =
            await _db.Posts
                .CountAsync(cancellationToken);

        response.Overview.TotalReports =
            await _db.UserReports
                .CountAsync(cancellationToken);

        response.Overview.TotalNotifications =
            await _db.Notifications
                .CountAsync(cancellationToken);

        var postStatistics = await _db.Posts
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalLikes =
                    g.Sum(x => x.LikeCount ?? 0),

                TotalComments =
                    g.Sum(x => x.CommentCount ?? 0),

                TotalShares =
                    g.Sum(x => x.ShareCount ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        response.Overview.TotalLikes =
            postStatistics?.TotalLikes ?? 0;

        response.Overview.TotalComments =
            postStatistics?.TotalComments ?? 0;

        response.Overview.TotalShares =
            postStatistics?.TotalShares ?? 0;

        response.Overview.TotalEngagement =
            response.Overview.TotalLikes +
            response.Overview.TotalComments +
            response.Overview.TotalShares;

        response.Overview.AverageEngagementPerPost =
            response.Overview.TotalPosts == 0
                ? 0
                : Math.Round(
                    (double)response.Overview.TotalEngagement /
                    response.Overview.TotalPosts,
                    2);

        response.Overview.VerifiedUsers =
            await _db.Accounts
                .CountAsync(
                    x => x.EmailConfirmed,
                    cancellationToken);

        response.Overview.UnverifiedUsers =
            await _db.Accounts
                .CountAsync(
                    x => !x.EmailConfirmed,
                    cancellationToken);

        response.Overview.OnlineUsers =
            await _db.Accounts
                .CountAsync(
                    x => x.IsOnline == "Online",
                    cancellationToken);

        response.Overview.ReportedPosts =
            await _db.UserReports
                .Select(x => x.PostId)
                .Distinct()
                .CountAsync(cancellationToken);
    }

    private async Task LoadTodayStatisticsAsync(
        SocialDashboardOverviewResponse response,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var tomorrow = today.AddDays(1);

        response.Today.NewUsers =
            await _db.Accounts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= today &&
                        x.CreatedAt.Value < tomorrow,
                    cancellationToken);

        response.Today.Posts =
            await _db.Posts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= today &&
                        x.CreatedAt.Value < tomorrow,
                    cancellationToken);

        response.Today.Reports =
            await _db.UserReports
                .CountAsync(
                    x =>
                        x.CreatedAt >= today &&
                        x.CreatedAt < tomorrow,
                    cancellationToken);

        response.Today.Notifications =
            await _db.Notifications
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= today &&
                        x.CreatedAt.Value < tomorrow,
                    cancellationToken);
    }

    private async Task LoadPeriodStatisticsAsync(
        SocialDashboardOverviewResponse response,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var last7Days = today.AddDays(-7);

        var last30Days = today.AddDays(-30);

        response.Period.Last7DaysUsers =
            await _db.Accounts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= last7Days,
                    cancellationToken);

        response.Period.Last30DaysUsers =
            await _db.Accounts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= last30Days,
                    cancellationToken);

        response.Period.Last7DaysPosts =
            await _db.Posts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= last7Days,
                    cancellationToken);

        response.Period.Last30DaysPosts =
            await _db.Posts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= last30Days,
                    cancellationToken);
    }

    private async Task LoadGrowthStatisticsAsync(
        SocialDashboardOverviewResponse response,
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var last7Days = today.AddDays(-7);

        var previous7Days = today.AddDays(-14);

        var last30Days = today.AddDays(-30);

        var previous30Days = today.AddDays(-60);

        var previous7DaysUsers =
            await _db.Accounts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= previous7Days &&
                        x.CreatedAt.Value < last7Days,
                    cancellationToken);

        var previous7DaysPosts =
            await _db.Posts
                .CountAsync(
                    x =>
                        x.CreatedAt.HasValue &&
                        x.CreatedAt.Value >= previous7Days &&
                        x.CreatedAt.Value < last7Days,
                    cancellationToken);

        var current30DaysEngagement =
            await _db.Posts
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= last30Days)
                .SumAsync(
                    x =>
                        (x.LikeCount ?? 0) +
                        (x.CommentCount ?? 0) +
                        (x.ShareCount ?? 0),
                    cancellationToken);

        var previous30DaysEngagement =
            await _db.Posts
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= previous30Days &&
                    x.CreatedAt.Value < last30Days)
                .SumAsync(
                    x =>
                        (x.LikeCount ?? 0) +
                        (x.CommentCount ?? 0) +
                        (x.ShareCount ?? 0),
                    cancellationToken);

        response.Growth.UserGrowthPercent =
            CalculateGrowthPercent(
                previous7DaysUsers,
                response.Period.Last7DaysUsers);

        response.Growth.PostGrowthPercent =
            CalculateGrowthPercent(
                previous7DaysPosts,
                response.Period.Last7DaysPosts);

        response.Growth.EngagementGrowthPercent =
            CalculateGrowthPercent(
                previous30DaysEngagement,
                current30DaysEngagement);
    }

    private async Task LoadNotificationStatisticsAsync(
        SocialDashboardOverviewResponse response,
        CancellationToken cancellationToken)
    {
        response.Notification.Unread =
            await _db.Notifications
                .CountAsync(
                    x => x.Status == "Unread",
                    cancellationToken);

        response.Notification.Read =
            await _db.Notifications
                .CountAsync(
                    x => x.Status == "Read",
                    cancellationToken);

        response.Notification.Failed =
            await _db.Notifications
                .CountAsync(
                    x => x.Status == "Failed",
                    cancellationToken);

        response.Notification.System =
            await _db.Notifications
                .CountAsync(
                    x =>
                        x.Type == NotificationType.OrderCreated ||
                        x.Type == NotificationType.OrderPaid ||
                        x.Type == NotificationType.OrderShipping ||
                        x.Type == NotificationType.OrderDelivered ||
                        x.Type == NotificationType.OrderCompleted ||
                        x.Type == NotificationType.OrderCancelled ||
                        x.Type == NotificationType.RefundRequested ||
                        x.Type == NotificationType.RefundApproved ||
                        x.Type == NotificationType.RefundRejected,
                    cancellationToken);

        response.Notification.User =
            await _db.Notifications
                .CountAsync(
                    x =>
                        x.Type == NotificationType.LikePost ||
                        x.Type == NotificationType.CommentPost ||
                        x.Type == NotificationType.MentionPost ||
                        x.Type == NotificationType.ReplyComment ||
                        x.Type == NotificationType.SharePost ||
                        x.Type == NotificationType.FollowUser ||
                        x.Type == NotificationType.SavePost,
                    cancellationToken);
    }

    #endregion

    #region CHARTS

    public async Task<DashboardChartsResponse> GetChartsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var userChart =
            await _db.Accounts
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= startDate &&
                    x.CreatedAt.Value <= endDate)
                .GroupBy(x => x.CreatedAt!.Value.Date)
                .Select(g => new DailyStatisticDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

        var postChart =
            await _db.Posts
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= startDate &&
                    x.CreatedAt.Value <= endDate)
                .GroupBy(x => x.CreatedAt!.Value.Date)
                .Select(g => new DailyStatisticDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

        var reportChart =
            await _db.UserReports
                .Where(x =>
                    x.CreatedAt >= startDate &&
                    x.CreatedAt <= endDate)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new DailyStatisticDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

        var notificationChart =
            await _db.Notifications
                .Where(x =>
                    x.CreatedAt.HasValue &&
                    x.CreatedAt.Value >= startDate &&
                    x.CreatedAt.Value <= endDate)
                .GroupBy(x => x.CreatedAt!.Value.Date)
                .Select(g => new DailyStatisticDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

        return new DashboardChartsResponse
        {
            UserGrowthChart =
                FillMissingDates(
                    startDate,
                    endDate,
                    userChart),

            PostGrowthChart =
                FillMissingDates(
                    startDate,
                    endDate,
                    postChart),

            ReportGrowthChart =
                FillMissingDates(
                    startDate,
                    endDate,
                    reportChart),

            NotificationGrowthChart =
                FillMissingDates(
                    startDate,
                    endDate,
                    notificationChart)
        };
    }

    #endregion

    #region ANALYTICS

    public async Task<DashboardAnalyticsResponse> GetAnalyticsAsync(
        CancellationToken cancellationToken = default)
    {
        var userStatuses =
            await _db.Accounts
                .GroupBy(x => x.Status ?? "Unknown")
                .Select(g => new StatusStatisticDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var postStatuses =
            await _db.Posts
                .GroupBy(x => x.Status ?? "Unknown")
                .Select(g => new StatusStatisticDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var reportStatuses =
            await _db.UserReports
                .GroupBy(x => x.Status ?? "Unknown")
                .Select(g => new StatusStatisticDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var reportTypes =
            await _db.UserReports
                .GroupBy(x => x.ReportType.TypeName)
                .Select(g => new TypeStatisticDto
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var notificationTypes =
            await _db.Notifications
                .GroupBy(x => x.Type ?? "Unknown")
                .Select(g => new TypeStatisticDto
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var notificationStatuses =
            await _db.Notifications
                .GroupBy(x => x.Status ?? "Unknown")
                .Select(g => new StatusStatisticDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

        var topActiveUsers =
            await _db.Accounts
                .Select(x => new TopActiveUserDto
                {
                    AccountId = x.Id,

                    Username = x.UserName ?? "Unknown",

                    TotalPosts = x.Posts.Count,

                    TotalEngagement =
                        x.Posts.Sum(p =>
                            (p.LikeCount ?? 0) +
                            (p.CommentCount ?? 0) +
                            (p.ShareCount ?? 0))
                })
                .OrderByDescending(x => x.TotalEngagement)
                .Take(10)
                .ToListAsync(cancellationToken);

        var topReportedPosts =
            await _db.UserReports
                .GroupBy(x => x.PostId)
                .Select(g => new TopReportedPostDto
                {
                    PostId = g.Key,
                    ReportCount = g.Count()
                })
                .OrderByDescending(x => x.ReportCount)
                .Take(10)
                .ToListAsync(cancellationToken);

        return new DashboardAnalyticsResponse
        {
            UserStatuses = userStatuses,
            PostStatuses = postStatuses,
            ReportStatuses = reportStatuses,
            ReportTypes = reportTypes,
            NotificationTypes = notificationTypes,
            NotificationStatuses = notificationStatuses,
            TopActiveUsers = topActiveUsers,
            TopReportedPosts = topReportedPosts
        };
    }

    #endregion

    #region RECENT

    public async Task<DashboardRecentResponse> GetRecentAsync(
        CancellationToken cancellationToken = default)
    {
        var recentNotifications =
            await _db.Notifications
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .Select(x => new RecentNotificationDto
                {
                    NotificationId = x.NotificationId,
                    Type = x.Type ?? "Unknown",
                    Status = x.Status ?? "Unknown",
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(cancellationToken);

        return new DashboardRecentResponse
        {
            RecentNotifications = recentNotifications
        };
    }

    #endregion

    #region PRIVATE METHODS

    private static double CalculateGrowthPercent(
        long previous,
        long current)
    {
        if (previous == 0)
        {
            return 0;
        }

        return Math.Round(
            ((double)(current - previous) / previous) * 100,
            2);
    }

    private static List<DailyStatisticDto> FillMissingDates(
        DateTime startDate,
        DateTime endDate,
        List<DailyStatisticDto> source)
    {
        var result = new List<DailyStatisticDto>();

        var lookup = source.ToDictionary(
            x => x.Date.Date,
            x => x.Count);

        for (
            var date = startDate.Date;
            date <= endDate.Date;
            date = date.AddDays(1))
        {
            result.Add(new DailyStatisticDto
            {
                Date = date,
                Count = lookup.GetValueOrDefault(date, 0)
            });
        }

        return result;
    }

    #endregion
}