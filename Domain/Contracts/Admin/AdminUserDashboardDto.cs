namespace Domain.Contracts.Admin;

public class AdminUserDashboardDto
{
    public UserOverviewDto Overview { get; set; } = new();

    public List<UserGrowthChartDto> UserGrowthChart { get; set; } = new();

    public List<ActivityChartDto> FollowGrowthChart { get; set; } = new();

    public List<ActivityChartDto> PostGrowthChart { get; set; } = new();

    public List<TopUserDto> TopFollowedUsers { get; set; } = new();

    public List<TopUserDto> TopPostingUsers { get; set; } = new();

    public List<RecentUserDto> RecentUsers { get; set; } = new();

    public List<ReportedUserDto> ReportedUsers { get; set; } = new();
}

public class UserOverviewDto
{
    public int TotalUsers { get; set; }

    public int NewUsersToday { get; set; }

    public int NewUsersThisWeek { get; set; }

    public int NewUsersThisMonth { get; set; }

    public int OnlineUsers { get; set; }

    public int VerifiedUsers { get; set; }

    public int CompletedOnboardingUsers { get; set; }

    public int TotalPosts { get; set; }

    public int PostsToday { get; set; }

    public int TotalFollows { get; set; }

    public int FollowsToday { get; set; }

    public int TotalReports { get; set; }

    public int PendingReports { get; set; }

    public int MaleUsers { get; set; }

    public int FemaleUsers { get; set; }

    public int OtherGenderUsers { get; set; }
}

public class UserGrowthChartDto
{
    public string Label { get; set; } = default!;

    public int Count { get; set; }
}

public class ActivityChartDto
{
    public string Label { get; set; } = default!;

    public int Count { get; set; }
}

public class TopUserDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = default!;

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    public int Followers { get; set; }

    public int Posts { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class RecentUserDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = default!;

    public string? Email { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Status { get; set; }

    public bool HasCompletedOnboarding { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class ReportedUserDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = default!;

    public string? AvatarUrl { get; set; }

    public int TotalReports { get; set; }

    public int PendingReports { get; set; }
}