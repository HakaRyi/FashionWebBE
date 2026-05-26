namespace Domain.Contracts.Admin;

public class SocialDashboardOverviewResponse
{
    public OverviewStatisticDto Overview { get; set; } = new();

    public TodayStatisticDto Today { get; set; } = new();

    public PeriodStatisticDto Period { get; set; } = new();

    public GrowthStatisticDto Growth { get; set; } = new();

    public NotificationStatisticDto Notification { get; set; } = new();
}

public class OverviewStatisticDto
{
    public int TotalUsers { get; set; }

    public int TotalPosts { get; set; }

    public int TotalReports { get; set; }

    public int TotalNotifications { get; set; }

    public long TotalLikes { get; set; }

    public long TotalComments { get; set; }

    public long TotalShares { get; set; }

    public long TotalEngagement { get; set; }

    public double AverageEngagementPerPost { get; set; }

    public int VerifiedUsers { get; set; }

    public int UnverifiedUsers { get; set; }

    public int OnlineUsers { get; set; }

    public int ReportedPosts { get; set; }
}

public class TodayStatisticDto
{
    public int NewUsers { get; set; }

    public int Posts { get; set; }

    public int Reports { get; set; }

    public int Notifications { get; set; }
}

public class PeriodStatisticDto
{
    public int Last7DaysUsers { get; set; }

    public int Last30DaysUsers { get; set; }

    public int Last7DaysPosts { get; set; }

    public int Last30DaysPosts { get; set; }
}

public class GrowthStatisticDto
{
    public double UserGrowthPercent { get; set; }

    public double PostGrowthPercent { get; set; }

    public double EngagementGrowthPercent { get; set; }
}

public class NotificationStatisticDto
{
    public int Unread { get; set; }

    public int Read { get; set; }

    public int Failed { get; set; }

    public int System { get; set; }

    public int User { get; set; }
}

public class StatusStatisticDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class TypeStatisticDto
{
    public string Type { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class DailyStatisticDto
{
    public DateTime Date { get; set; }

    public int Count { get; set; }
}

public class RecentNotificationDto
{
    public int NotificationId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
}

public class TopActiveUserDto
{
    public int AccountId { get; set; }

    public string Username { get; set; } = string.Empty;

    public int TotalPosts { get; set; }

    public long TotalEngagement { get; set; }
}

public class TopReportedPostDto
{
    public int PostId { get; set; }

    public int ReportCount { get; set; }
}

public class DashboardChartsResponse
{
    public List<DailyStatisticDto> UserGrowthChart { get; set; } = [];

    public List<DailyStatisticDto> PostGrowthChart { get; set; } = [];

    public List<DailyStatisticDto> ReportGrowthChart { get; set; } = [];

    public List<DailyStatisticDto> NotificationGrowthChart { get; set; } = [];
}

public class DashboardAnalyticsResponse
{
    public List<StatusStatisticDto> UserStatuses { get; set; } = [];

    public List<StatusStatisticDto> PostStatuses { get; set; } = [];

    public List<StatusStatisticDto> ReportStatuses { get; set; } = [];

    public List<TypeStatisticDto> ReportTypes { get; set; } = [];

    public List<TypeStatisticDto> NotificationTypes { get; set; } = [];

    public List<StatusStatisticDto> NotificationStatuses { get; set; } = [];

    public List<TopActiveUserDto> TopActiveUsers { get; set; } = [];

    public List<TopReportedPostDto> TopReportedPosts { get; set; } = [];
}

public class DashboardRecentResponse
{
    public List<RecentNotificationDto> RecentNotifications { get; set; } = [];
}