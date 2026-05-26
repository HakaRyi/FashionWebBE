using Domain.Constants;
using Domain.Contracts.Admin;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminPostDashboardRepository : IAdminPostDashboardRepository
{
    private readonly FashionDbContext _db;

    public AdminPostDashboardRepository(FashionDbContext db)
    {
        _db = db;
    }

    public async Task<AdminPostDashboardDto> GetPostDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;

        var weekAgo = today.AddDays(-7);

        var monthAgo = today.AddDays(-30);

        // OVERVIEW
        var totalPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync();

        var postsToday = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value.Date == today);

        var postsThisWeek = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= weekAgo);

        var postsThisMonth = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= monthAgo);

        var draftPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == PostStatus.Draft);

        var publishedPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == PostStatus.Published);

        var hiddenPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.Visibility == PostVisibility.Hidden);

        var expertPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.IsExpertPost == true);

        var totalReactions = await _db.Posts
            .AsNoTracking()
            .SumAsync(x => x.LikeCount ?? 0);

        var reactionsToday = await _db.Posts
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value.Date == today)
            .SumAsync(x => x.LikeCount ?? 0);

        var totalComments = await _db.Comments
            .AsNoTracking()
            .CountAsync();

        var commentsToday = await _db.Comments
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.Date == today);

        var totalShares = await _db.Posts
            .AsNoTracking()
            .SumAsync(x => x.ShareCount ?? 0);

        var totalSavedPosts = await _db.PostSaves
            .AsNoTracking()
            .CountAsync();

        var totalReports = await _db.UserReports
            .AsNoTracking()
            .CountAsync();

        var pendingReports = await _db.UserReports
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == ReportStatus.Pending);

        var postsWithImages = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.Images.Any());

        // RECENT POSTS
        var recentPosts = await _db.Posts
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Account)
            .Include(x => x.PostHashtags)
                .ThenInclude(x => x.Hashtag)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new RecentPostDto
            {
                PostId = x.PostId,
                Title = x.Title,

                ThumbnailUrl = x.Images
                    .OrderBy(i => i.ImageId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                AuthorId = x.AccountId,

                AuthorName = x.Account.UserName ?? string.Empty,

                IsExpertPost = x.IsExpertPost ?? false,

                Status = x.Status,

                Visibility = x.Visibility,

                Likes = x.LikeCount ?? 0,

                Comments = x.CommentCount ?? 0,

                Shares = x.ShareCount ?? 0,

                CreatedAt = x.CreatedAt,

                Hashtags = x.PostHashtags
                    .Select(h => new PostHashtagDto
                    {
                        HashtagId = h.HashtagId,
                        Name = h.Hashtag.Name
                    })
                .ToList()
            })
            .ToListAsync();

        // TOP LIKED POSTS
        var topLikedPosts = await _db.Posts
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Account)
            .Include(x => x.PostHashtags)
                .ThenInclude(x => x.Hashtag)
            .OrderByDescending(x => x.LikeCount)
            .Take(10)
            .Select(x => new TopPostDto
            {
                PostId = x.PostId,
                Title = x.Title,

                ThumbnailUrl = x.Images
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                AuthorId = x.AccountId,

                AuthorName = x.Account.UserName ?? string.Empty,

                Likes = x.LikeCount ?? 0,

                Comments = x.CommentCount ?? 0,

                Shares = x.ShareCount ?? 0,

                CreatedAt = x.CreatedAt,

                Hashtags = x.PostHashtags
                    .Select(h => new PostHashtagDto
                    {
                        HashtagId = h.HashtagId,
                        Name = h.Hashtag.Name
                    })
                .ToList()
            })
            .ToListAsync();
       
        // MOST DISCUSSED POSTS
        var mostDiscussedPosts = await _db.Posts
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Account)
            .Include(x => x.PostHashtags)
                .ThenInclude(x => x.Hashtag)
            .OrderByDescending(x => x.CommentCount)
            .Take(10)
            .Select(x => new TopPostDto
            {
                PostId = x.PostId,
                Title = x.Title,

                ThumbnailUrl = x.Images
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                AuthorId = x.AccountId,

                AuthorName = x.Account.UserName ?? string.Empty,

                Likes = x.LikeCount ?? 0,

                Comments = x.CommentCount ?? 0,

                Shares = x.ShareCount ?? 0,

                CreatedAt = x.CreatedAt,

                Hashtags = x.PostHashtags
                    .Select(h => new PostHashtagDto
                    {
                        HashtagId = h.HashtagId,
                        Name = h.Hashtag.Name
                    })
                .ToList()
            })
            .ToListAsync();

        // POST GROWTH CHART
        var postGrowthRaw = await _db.Posts
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= monthAgo)
            .GroupBy(x => x.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var postGrowthChart = postGrowthRaw
            .Select(x => new ActivityChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

        // REACTION GROWTH CHART
        var reactionGrowthRaw = await _db.Posts
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= monthAgo)
            .GroupBy(x => x.CreatedAt!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Sum(x => x.LikeCount ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var reactionGrowthChart = reactionGrowthRaw
            .Select(x => new ActivityChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

        // COMMENT GROWTH CHART
        var commentGrowthRaw = await _db.Comments
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt >= monthAgo)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var commentGrowthChart = commentGrowthRaw
            .Select(x => new ActivityChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

        // REPORT GROWTH CHART
        var reportGrowthRaw = await _db.UserReports
            .AsNoTracking()
            .Where(x =>
                x.CreatedAt >= monthAgo)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var reportGrowthChart = reportGrowthRaw
            .Select(x => new ActivityChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

        // REPORTED POSTS
        var reportedPosts = await _db.UserReports
            .AsNoTracking()
            .GroupBy(x => new
            {
                x.PostId,
                x.Post.Title,
                x.Post.Account.UserName
            })
            .Select(g => new ReportedPostDto
            {
                PostId = g.Key.PostId,

                Title = g.Key.Title,

                AuthorName = g.Key.UserName ?? string.Empty,

                TotalReports = g.Count(),

                PendingReports = g.Count(x =>
                    x.Status == ReportStatus.Pending),

                LastReportAt = g.Max(x => x.CreatedAt),

                ThumbnailUrl = _db.Images
                    .Where(i => i.PostId == g.Key.PostId)
                    .OrderBy(i => i.ImageId)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.TotalReports)
            .Take(10)
            .ToListAsync();


        var topTrendingPosts = await _db.PostTrends
            .AsNoTracking()
            .Include(x => x.Post)
                .ThenInclude(p => p.Account)
            .Include(x => x.Post.Images)
            .Include(x => x.Post.PostHashtags)
                .ThenInclude(x => x.Hashtag)
            .Where(x =>
                x.Post.Status == PostStatus.Published &&
                x.Post.Visibility == PostVisibility.Visible)
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => new TopPostDto
            {
                PostId = x.PostId,
                Title = x.Post.Title,

                ThumbnailUrl = x.Post.Images
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                AuthorId = x.Post.AccountId,

                AuthorName = x.Post.Account.UserName ?? string.Empty,

                Likes = x.Post.LikeCount ?? 0,
                Comments = x.Post.CommentCount ?? 0,
                Shares = x.Post.ShareCount ?? 0,

                CreatedAt = x.Post.CreatedAt,
                Hashtags = x.Post.PostHashtags
                .Select(h => new PostHashtagDto
                {
                    HashtagId = h.HashtagId,
                    Name = h.Hashtag.Name
                })
                .ToList()
            })
            .ToListAsync();

        // TRENDING TOPICS
        var topTrendingHashtags = await _db.TrendingTopics
            .AsNoTracking()
            .Include(x => x.Hashtag)
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => new TrendingTopicDto
            {
                HashtagId = x.HashtagId,
                Name = x.Hashtag.Name,

                Score = x.Score,

                TotalPosts = x.TotalPosts,

                TotalEngagement = x.TotalEngagement,

                CalculatedAt = x.CalculatedAt
            })
            .ToListAsync();

        // RETURN
        return new AdminPostDashboardDto
        {
            Overview = new PostOverviewDto
            {
                TotalPosts = totalPosts,
                PostsToday = postsToday,
                PostsThisWeek = postsThisWeek,
                PostsThisMonth = postsThisMonth,

                DraftPosts = draftPosts,
                PublishedPosts = publishedPosts,
                HiddenPosts = hiddenPosts,
                ExpertPosts = expertPosts,

                TotalReactions = totalReactions,
                ReactionsToday = reactionsToday,

                TotalComments = totalComments,
                CommentsToday = commentsToday,

                TotalShares = totalShares,

                TotalSavedPosts = totalSavedPosts,

                TotalReports = totalReports,
                PendingReports = pendingReports,

                PostsWithImages = postsWithImages
            },

            PostGrowthChart = postGrowthChart,

            ReactionGrowthChart = reactionGrowthChart,

            CommentGrowthChart = commentGrowthChart,

            ReportGrowthChart = reportGrowthChart,

            TopLikedPosts = topLikedPosts,

            MostDiscussedPosts = mostDiscussedPosts,

            TopTrendingPosts = topTrendingPosts,

            TopTrendingHashtags = topTrendingHashtags,

            RecentPosts = recentPosts,

            ReportedPosts = reportedPosts
        };
    }
}