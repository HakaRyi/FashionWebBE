using Domain.Constants;
using Domain.Contracts.Admin;
using Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AdminUserDashboardRepository : IAdminUserDashboardRepository
{
    private readonly FashionDbContext _db;

    public AdminUserDashboardRepository(FashionDbContext db)
    {
        _db = db;
    }

    public async Task<AdminUserDashboardDto> GetUserDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var accountsQuery = _db.Accounts
            .AsNoTracking()
            .AsQueryable();

        var totalUsers = await accountsQuery.CountAsync();

        var newUsersToday = await accountsQuery
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value.Date == today);

        var newUsersThisWeek = await accountsQuery
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= weekAgo);

        var newUsersThisMonth = await accountsQuery
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value >= monthAgo);

        var onlineUsers = await accountsQuery
            .CountAsync(x =>
                x.IsOnline == "ONLINE");

        var verifiedUsers = await accountsQuery
            .CountAsync(x =>
                x.EmailConfirmed);

        var completedOnboardingUsers = await accountsQuery
            .CountAsync(x =>
                x.HasCompletedOnboarding);

        var maleUsers = await accountsQuery
            .CountAsync(x =>
                x.Gender == GenderType.Male);

        var femaleUsers = await accountsQuery
            .CountAsync(x =>
                x.Gender == GenderType.Female);

        var otherGenderUsers = await accountsQuery
            .CountAsync(x =>
                x.Gender != GenderType.Male &&
                x.Gender != GenderType.Female);

        var totalPosts = await _db.Posts
            .AsNoTracking()
            .CountAsync();

        var postsToday = await _db.Posts
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value.Date == today);

        var totalFollows = await _db.Follows
            .AsNoTracking()
            .CountAsync();

        var followsToday = await _db.Follows
            .AsNoTracking()
            .CountAsync(x =>
                x.CreatedAt.HasValue &&
                x.CreatedAt.Value.Date == today);

        var totalReports = await _db.UserReports
            .AsNoTracking()
            .CountAsync();

        var pendingReports = await _db.UserReports
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == ReportStatus.Pending);

        var usersWithPhysicalProfile = await _db.PhysicalProfiles
            .AsNoTracking()
            .Select(x => x.AccountId)
            .Distinct()
            .CountAsync();

        var totalTryOnFeaturesUsed = await _db.TryOnHistories
            .AsNoTracking()
            .CountAsync();

        var activeExperts = await _db.Accounts
            .AsNoTracking()
            .CountAsync(x => x.ExpertProfile != null);

        var currentProfilesQuery = _db.PhysicalProfiles
            .AsNoTracking()
            .Where(x => x.IsCurrent);

        var totalCurrentProfiles = await currentProfilesQuery.CountAsync();

        var bodyShapeRaw = await currentProfilesQuery
            .Where(x => x.BodyShape != null && x.BodyShape != string.Empty)
            .GroupBy(x => x.BodyShape)
            .Select(g => new { Label = g.Key!, Count = g.Count() })
            .ToListAsync();

        var bodyShapeDistribution = bodyShapeRaw.Select(x => new FashionInsightDto
        {
            Label = x.Label,
            Count = x.Count,
            Percentage = totalCurrentProfiles > 0 ? Math.Round((double)x.Count / totalCurrentProfiles * 100, 2) : 0
        }).ToList();

        var skinToneRaw = await currentProfilesQuery
            .Where(x => x.SkinTone != null && x.SkinTone != string.Empty)
            .GroupBy(x => x.SkinTone)
            .Select(g => new { Label = g.Key!, Count = g.Count() })
            .ToListAsync();

        var skinToneDistribution = skinToneRaw.Select(x => new FashionInsightDto
        {
            Label = x.Label,
            Count = x.Count,
            Percentage = totalCurrentProfiles > 0 ? Math.Round((double)x.Count / totalCurrentProfiles * 100, 2) : 0
        }).ToList();

        // RECENT USERS
        var recentUsers = await _db.Accounts
            .AsNoTracking()
            .Include(x => x.Avatars)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .Select(x => new RecentUserDto
            {
                UserId = x.Id,
                Username = x.UserName ?? string.Empty,
                Email = x.Email,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                HasCompletedOnboarding = x.HasCompletedOnboarding,
                IsOnline = x.IsOnline == "ONLINE",

                AvatarUrl = x.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // TOP FOLLOWED USERS
        var topFollowedUsers = await _db.Accounts
            .AsNoTracking()
            .Include(x => x.Avatars)
            .OrderByDescending(x => x.CountFollower)
            .Take(10)
            .Select(x => new TopUserDto
            {
                UserId = x.Id,
                Username = x.UserName ?? string.Empty,
                Email = x.Email,
                Followers = x.CountFollower,
                Posts = x.CountPost,
                IsOnline = x.IsOnline == "ONLINE",
                CreatedAt = x.CreatedAt,

                AvatarUrl = x.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // TOP POSTING USERS
        var topPostingUsers = await _db.Accounts
            .AsNoTracking()
            .Include(x => x.Avatars)
            .OrderByDescending(x => x.CountPost)
            .Take(10)
            .Select(x => new TopUserDto
            {
                UserId = x.Id,
                Username = x.UserName ?? string.Empty,
                Email = x.Email,
                Followers = x.CountFollower,
                Posts = x.CountPost,
                IsOnline = x.IsOnline == "ONLINE",
                CreatedAt = x.CreatedAt,

                AvatarUrl = x.Avatars
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => a.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // USER GROWTH CHART
        var userGrowthRaw = await _db.Accounts
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

        var userGrowthChart = userGrowthRaw
            .Select(x => new UserGrowthChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

        // FOLLOW GROWTH CHART
        var followGrowthRaw = await _db.Follows
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

        var followGrowthChart = followGrowthRaw
            .Select(x => new ActivityChartDto
            {
                Label = x.Date.ToString("dd/MM"),
                Count = x.Count
            })
            .ToList();

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

        // REPORTED USERS
        var reportedUsers = await _db.UserReports
            .AsNoTracking()
            .GroupBy(x => new
            {
                x.Account.Id,
                x.Account.UserName
            })
            .Select(g => new ReportedUserDto
            {
                UserId = g.Key.Id,
                Username = g.Key.UserName ?? string.Empty,

                TotalReports = g.Count(),

                PendingReports = g.Count(x =>
                    x.Status == ReportStatus.Pending),

                AvatarUrl = _db.Images
                    .Where(img =>
                        img.AccountAvatarId == g.Key.Id)
                    .OrderByDescending(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.TotalReports)
            .Take(10)
            .ToListAsync();

        return new AdminUserDashboardDto
        {
            Overview = new UserOverviewDto
            {
                TotalUsers = totalUsers,
                NewUsersToday = newUsersToday,
                NewUsersThisWeek = newUsersThisWeek,
                NewUsersThisMonth = newUsersThisMonth,
                OnlineUsers = onlineUsers,
                VerifiedUsers = verifiedUsers,
                CompletedOnboardingUsers = completedOnboardingUsers,
                TotalPosts = totalPosts,
                PostsToday = postsToday,
                TotalFollows = totalFollows,
                FollowsToday = followsToday,
                TotalReports = totalReports,
                PendingReports = pendingReports,
                MaleUsers = maleUsers,
                FemaleUsers = femaleUsers,
                OtherGenderUsers = otherGenderUsers,

                UsersWithPhysicalProfile = usersWithPhysicalProfile,
                TotalTryOnFeaturesUsed = totalTryOnFeaturesUsed,
                ActiveExperts = activeExperts
            },

            UserGrowthChart = userGrowthChart,
            FollowGrowthChart = followGrowthChart,
            PostGrowthChart = postGrowthChart,
            RecentUsers = recentUsers,
            TopFollowedUsers = topFollowedUsers,
            TopPostingUsers = topPostingUsers,
            ReportedUsers = reportedUsers,

            BodyShapeDistribution = bodyShapeDistribution,
            SkinToneDistribution = skinToneDistribution
        };
    }
}