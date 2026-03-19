using Microsoft.EntityFrameworkCore;
using Repositories.Constants;
using Repositories.Dto.Common;
using Repositories.Dto.Social.Report;
using Repositories.Repos.Report;
using Repositories.UnitOfWork;

namespace Services.Implements.Report
{
    public class UserReportService : IUserReportService
    {
        private readonly IUserReportRepository _userReportRepository;
        private readonly IUnitOfWork _uow;

        public UserReportService(
            IUserReportRepository userReportRepository,
            IUnitOfWork uow)
        {
            _userReportRepository = userReportRepository;
            _uow = uow;
        }

        public async Task<PostReportDto> ReportPostAsync(
            int postId,
            int accountId,
            CreatePostReportDto request)
        {
            if (request == null)
                throw new ArgumentException("Request cannot be null.");

            if (request.ReportTypeId <= 0)
                throw new ArgumentException("Invalid report type.");

            if (!string.IsNullOrWhiteSpace(request.Reason) &&
                request.Reason.Trim().Length > 1000)
            {
                throw new ArgumentException("Reason must not exceed 1000 characters.");
            }

            var post = await _userReportRepository.GetPostValidationInfoAsync(postId);
            if (post == null)
                throw new KeyNotFoundException("Post not found.");

            if (post.Status != PostStatus.Published ||
                post.Visibility != PostVisibility.Visible)
            {
                throw new InvalidOperationException("Only visible published posts can be reported.");
            }

            if (post.AccountId == accountId)
                throw new InvalidOperationException("You cannot report your own post.");

            var reportType = await _userReportRepository.GetReportTypeByIdAsync(request.ReportTypeId);
            if (reportType == null)
                throw new KeyNotFoundException("Report type not found.");

            if (await _userReportRepository.IsAlreadyReportedAsync(postId, accountId))
                throw new InvalidOperationException("You already reported this post.");

            var entity = new Repositories.Entities.UserReport
            {
                PostId = postId,
                AccountId = accountId,
                ReportTypeId = request.ReportTypeId,
                Reason = string.IsNullOrWhiteSpace(request.Reason)
                    ? null
                    : request.Reason.Trim(),
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            try
            {
                await _userReportRepository.AddAsync(entity);
                await _uow.SaveChangesAsync();

                return new PostReportDto
                {
                    UserReportId = entity.UserReportId,
                    PostId = entity.PostId,
                    AccountId = entity.AccountId,
                    ReportTypeId = entity.ReportTypeId,
                    ReportTypeName = reportType.TypeName,
                    Reason = entity.Reason,
                    CreatedAt = entity.CreatedAt,
                    Status = entity.Status
                };
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("You already reported this post.");
            }
        }

        public async Task<List<ReportTypeDto>> GetReportTypesAsync()
        {
            return await _userReportRepository.GetReportTypesAsync();
        }

        public async Task<PagedResultDto<AdminReportListItemDto>> GetAdminReportsAsync(
            string? status,
            int pageNumber,
            int pageSize)
        {
            if (!string.IsNullOrWhiteSpace(status) && !ReportStatus.IsValid(status))
                throw new ArgumentException("Invalid report status.");

            if (pageNumber <= 0)
                pageNumber = 1;

            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 100)
                pageSize = 100;

            return await _userReportRepository.GetAdminReportsAsync(
                status,
                pageNumber,
                pageSize);
        }

        public async Task<AdminReportDetailDto> GetAdminReportDetailAsync(int userReportId)
        {
            if (userReportId <= 0)
                throw new ArgumentException("Invalid report id.");

            var report = await _userReportRepository.GetAdminReportDetailAsync(userReportId);
            if (report == null)
                throw new KeyNotFoundException("Report not found.");

            return report;
        }

        public async Task<AdminReportDetailDto> UpdateReportStatusAsync(
            int userReportId,
            int adminAccountId,
            UpdateReportStatusDto request)
        {
            if (userReportId <= 0)
                throw new ArgumentException("Invalid report id.");

            if (request == null)
                throw new ArgumentException("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.Status))
                throw new ArgumentException("Status is required.");

            var newStatus = request.Status.Trim();

            if (!ReportStatus.IsValid(newStatus))
                throw new ArgumentException("Invalid report status.");

            if (!string.IsNullOrWhiteSpace(request.AdminNote) &&
                request.AdminNote.Trim().Length > 1000)
            {
                throw new ArgumentException("Admin note must not exceed 1000 characters.");
            }

            var report = await _userReportRepository.GetByIdAsync(userReportId);
            if (report == null)
                throw new KeyNotFoundException("Report not found.");

            if (!ReportStatus.CanTransition(report.Status, newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot change report status from '{report.Status}' to '{newStatus}'.");
            }

            report.Status = newStatus;
            report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
                ? null
                : request.AdminNote.Trim();
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedBy = adminAccountId;

            if (newStatus == ReportStatus.Resolved && report.Post != null)
            {
                //report.Post.Status = PostStatus.BlockedByAdmin;
                report.Post.UpdatedAt = DateTime.UtcNow;
            }

            _userReportRepository.Update(report);
            await _uow.SaveChangesAsync();

            var detail = await _userReportRepository.GetAdminReportDetailAsync(userReportId);
            if (detail == null)
                throw new KeyNotFoundException("Report not found after update.");

            return detail;
        }
    }
}