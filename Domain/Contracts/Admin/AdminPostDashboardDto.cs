namespace Domain.Contracts.Admin;

public class AdminPostDashboardDto
{
    public PostOverviewDto Overview { get; set; } = new();

    public List<ActivityChartDto> PostGrowthChart { get; set; } = new();

    public List<ActivityChartDto> ReactionGrowthChart { get; set; } = new();

    public List<ActivityChartDto> CommentGrowthChart { get; set; } = new();

    public List<ActivityChartDto> ReportGrowthChart { get; set; } = new();

    public List<TopPostDto> TopLikedPosts { get; set; } = new();

    public List<TopPostDto> MostDiscussedPosts { get; set; } = new();

    public List<TopPostDto> TopTrendingPosts { get; set; } = new();

    public List<TrendingTopicDto> TopTrendingHashtags { get; set; } = new();

    public List<RecentPostDto> RecentPosts { get; set; } = new();

    public List<ReportedPostDto> ReportedPosts { get; set; } = new();
}

public class PostOverviewDto
{
    public int TotalPosts { get; set; }

    public int PostsToday { get; set; }

    public int PostsThisWeek { get; set; }

    public int PostsThisMonth { get; set; }

    public int DraftPosts { get; set; }

    public int PublishedPosts { get; set; }

    public int HiddenPosts { get; set; }

    public int ExpertPosts { get; set; }

    public int TotalReactions { get; set; }

    public int ReactionsToday { get; set; }

    public int TotalComments { get; set; }

    public int CommentsToday { get; set; }

    public int TotalShares { get; set; }

    public int TotalSavedPosts { get; set; }

    public int TotalReports { get; set; }

    public int PendingReports { get; set; }

    public int PostsWithImages { get; set; }
}

public class TopPostDto
{
    public int PostId { get; set; }

    public string? Title { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int AuthorId { get; set; }

    public string AuthorName { get; set; } = default!;

    public int Likes { get; set; }

    public int Comments { get; set; }

    public int Shares { get; set; }

    public DateTime? CreatedAt { get; set; }

    public List<PostHashtagDto> Hashtags { get; set; } = new();
}

public class RecentPostDto
{
    public int PostId { get; set; }

    public string? Title { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int AuthorId { get; set; }

    public string AuthorName { get; set; } = default!;

    public bool IsExpertPost { get; set; }

    public string? Status { get; set; }

    public string Visibility { get; set; } = default!;

    public int Likes { get; set; }

    public int Comments { get; set; }

    public int Shares { get; set; }

    public DateTime? CreatedAt { get; set; }

    public List<PostHashtagDto> Hashtags { get; set; } = new();
}

public class ReportedPostDto
{
    public int PostId { get; set; }

    public string? Title { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int TotalReports { get; set; }

    public int PendingReports { get; set; }

    public DateTime? LastReportAt { get; set; }

    public string AuthorName { get; set; } = default!;

    public List<PostHashtagDto> Hashtags { get; set; } = new();
}

public class TrendingTopicDto
{
    public int HashtagId { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Score { get; set; }

    public int TotalPosts { get; set; }

    public int TotalEngagement { get; set; }

    public DateTime CalculatedAt { get; set; }
}

public class PostHashtagDto
{
    public int HashtagId { get; set; }

    public string Name { get; set; } = string.Empty;
}