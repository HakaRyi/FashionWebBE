using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Report;
using Repositories.Entities;

namespace Repositories.Repos.Report
{
    public class UserReportRepository : IUserReportRepository
    {
        private readonly FashionDbContext _context;

        public UserReportRepository(FashionDbContext context)
        {
            _context = context;
        }

        public async Task<PostReportValidationInfoDto?> GetPostValidationInfoAsync(int postId)
        {
            return await _context.Posts
                .AsNoTracking()
                .Where(x => x.PostId == postId)
                .Select(x => new PostReportValidationInfoDto
                {
                    PostId = x.PostId,
                    AccountId = x.AccountId,
                    Status = x.Status,
                    Visibility = x.Visibility
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ReportType?> GetReportTypeByIdAsync(int reportTypeId)
        {
            return await _context.ReportTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReportTypeId == reportTypeId);
        }

        public async Task<bool> IsAlreadyReportedAsync(int postId, int accountId)
        {
            return await _context.UserReports
                .AsNoTracking()
                .AnyAsync(x => x.PostId == postId && x.AccountId == accountId);
        }

        public async Task AddAsync(UserReport report)
        {
            await _context.UserReports.AddAsync(report);
        }

        public async Task<List<ReportTypeDto>> GetReportTypesAsync()
        {
            return await _context.ReportTypes
                .AsNoTracking()
                .OrderBy(x => x.ReportTypeId)
                .Select(x => new ReportTypeDto
                {
                    ReportTypeId = x.ReportTypeId,
                    TypeName = x.TypeName,
                    Description = x.Description
                })
                .ToListAsync();
        }

        public async Task<UserReport?> GetByIdAsync(int userReportId)
        {
            return await _context.UserReports
                .Include(x => x.Post)
                .Include(x => x.Account)
                .Include(x => x.ReportType)
                .FirstOrDefaultAsync(x => x.UserReportId == userReportId);
        }

        public async Task<AdminReportDetailDto?> GetAdminReportDetailAsync(int userReportId)
        {
            return await _context.UserReports
                .AsNoTracking()
                .Where(x => x.UserReportId == userReportId)
                .Select(x => new AdminReportDetailDto
                {
                    UserReportId = x.UserReportId,
                    PostId = x.PostId,
                    ReportedByAccountId = x.AccountId,
                    ReportedByUserName = x.Account.UserName ?? "",
                    PostOwnerAccountId = x.Post.AccountId,
                    PostOwnerUserName = x.Post.Account.UserName ?? "",
                    PostTitle = x.Post.Title,
                    PostContent = x.Post.Content,
                    PostStatus = x.Post.Status,
                    PostVisibility = x.Post.Visibility,
                    ReportTypeId = x.ReportTypeId,
                    ReportTypeName = x.ReportType.TypeName,
                    ReportTypeDescription = x.ReportType.Description,
                    Reason = x.Reason,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status,
                    ReviewedAt = x.ReviewedAt,
                    ReviewedBy = x.ReviewedBy,
                    AdminNote = x.AdminNote
                })
                .FirstOrDefaultAsync();
        }

        public async Task<PagedResultDto<AdminReportListItemDto>> GetAdminReportsAsync(
            string? status,
            int pageNumber,
            int pageSize)
        {
            var query = _context.UserReports
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminReportListItemDto
                {
                    UserReportId = x.UserReportId,
                    PostId = x.PostId,
                    ReportedByAccountId = x.AccountId,
                    ReportedByUserName = x.Account.UserName ?? "",
                    PostOwnerAccountId = x.Post.AccountId,
                    PostOwnerUserName = x.Post.Account.UserName ?? "",
                    PostStatus = x.Post.Status,
                    PostVisibility = x.Post.Visibility,
                    ReportTypeId = x.ReportTypeId,
                    ReportTypeName = x.ReportType.TypeName,
                    Reason = x.Reason,
                    CreatedAt = x.CreatedAt,
                    Status = x.Status,
                    ReviewedAt = x.ReviewedAt,
                    ReviewedBy = x.ReviewedBy,
                    AdminNote = x.AdminNote
                })
                .ToListAsync();

            return new PagedResultDto<AdminReportListItemDto>
            {
                Page = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items,
                HasMore = (pageNumber * pageSize) < totalCount
            };
        }

        public void Update(UserReport report)
        {
            _context.UserReports.Update(report);
        }
    }
}