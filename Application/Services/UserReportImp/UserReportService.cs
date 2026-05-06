using Application.Interfaces;
using Application.Request.UserReportReq;
using Application.Response.UserReportResp;
using Domain.Constants;
using Domain.Contracts.Common;
using Domain.Contracts.UserReport;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.UserReportImp
{
    public class UserReportService : IUserReportService
    {
        private readonly IUserReportRepository _userReportRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserReportService(
            IUserReportRepository userReportRepository,
            IUnitOfWork unitOfWork)
        {
            _userReportRepository = userReportRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateUserReportResponseDto> CreateReportAsync(
            CreateUserReportRequestDto request,
            int currentUserId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.PostId <= 0)
            {
                throw new ArgumentException("Invalid PostId.");
            }

            if (request.ReportTypeId <= 0)
            {
                throw new ArgumentException("Invalid ReportTypeId.");
            }

            request.Reason = request.Reason?.Trim();

            if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Length > 1000)
            {
                throw new ArgumentException("Reason must not exceed 1000 characters.");
            }

            var postInfo = await _userReportRepository.GetPostValidationInfoAsync(request.PostId);
            if (postInfo == null)
            {
                throw new KeyNotFoundException("Post not found.");
            }

            if (postInfo.AccountId == currentUserId)
            {
                throw new InvalidOperationException("You cannot report your own post.");
            }

            if (postInfo.Status == PostStatus.Deleted || postInfo.Status == PostStatus.Banned)
            {
                throw new InvalidOperationException("This post cannot be reported at the moment.");
            }

            var reportType = await _userReportRepository.GetReportTypeByIdAsync(request.ReportTypeId);
            if (reportType == null)
            {
                throw new KeyNotFoundException("Invalid report type.");
            }

            var isAlreadyReported = await _userReportRepository.IsAlreadyReportedAsync(request.PostId, currentUserId);
            if (isAlreadyReported)
            {
                throw new InvalidOperationException("You have already reported this post.");
            }

            var report = new UserReport
            {
                PostId = request.PostId,
                AccountId = currentUserId,
                ReportTypeId = request.ReportTypeId,
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending
            };

            await _userReportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            return new CreateUserReportResponseDto
            {
                UserReportId = report.UserReportId,
                PostId = report.PostId,
                AccountId = report.AccountId,
                ReportTypeId = report.ReportTypeId,
                ReportTypeName = reportType.TypeName,
                Reason = report.Reason,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                Message = "Post reported successfully."
            };
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
            {
                throw new ArgumentException("Invalid status.");
            }

            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            return await _userReportRepository.GetAdminReportsAsync(status, pageNumber, pageSize);
        }

        public async Task<AdminReportDetailDto?> GetAdminReportDetailAsync(int userReportId)
        {
            if (userReportId <= 0)
            {
                throw new ArgumentException("Invalid userReportId.");
            }

            return await _userReportRepository.GetAdminReportDetailAsync(userReportId);
        }

        public async Task<ReviewUserReportResponseDto> ReviewReportAsync(
            int userReportId,
            ReviewUserReportRequestDto request,
            int adminId)
        {
            if (userReportId <= 0)
            {
                throw new ArgumentException("Invalid userReportId.");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.NewStatus))
            {
                throw new ArgumentException("NewStatus must not be empty.");
            }

            request.NewStatus = request.NewStatus.Trim();

            if (!ReportStatus.IsValid(request.NewStatus))
            {
                throw new ArgumentException("Invalid NewStatus.");
            }

            if (request.NewStatus != ReportStatus.Resolved && request.NewStatus != ReportStatus.Rejected)
            {
                throw new ArgumentException("Admin can only change the report status to Resolved or Rejected.");
            }

            request.AdminNote = request.AdminNote?.Trim();

            if (!string.IsNullOrWhiteSpace(request.AdminNote) && request.AdminNote.Length > 1000)
            {
                throw new ArgumentException("AdminNote must not exceed 1000 characters.");
            }

            var report = await _userReportRepository.GetByIdAsync(userReportId);
            if (report == null)
            {
                throw new KeyNotFoundException("Report not found.");
            }

            if (!ReportStatus.CanTransition(report.Status, request.NewStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot change status from '{report.Status}' to '{request.NewStatus}'.");
            }

            report.Status = request.NewStatus;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedBy = adminId;
            report.AdminNote = request.AdminNote;

            if (request.NewStatus == ReportStatus.Resolved)
            {
                if (request.HidePostWhenResolved)
                {
                    report.Post.Visibility = PostVisibility.Hidden;
                    report.Post.UpdatedAt = DateTime.UtcNow;
                }

                if (!string.IsNullOrWhiteSpace(request.PostStatusToApply))
                {
                    if (!PostStatus.IsValid(request.PostStatusToApply))
                    {
                        throw new ArgumentException("Invalid PostStatusToApply.");
                    }

                    report.Post.Status = request.PostStatusToApply;
                    report.Post.UpdatedAt = DateTime.UtcNow;
                }
            }

            _userReportRepository.Update(report);
            await _unitOfWork.SaveChangesAsync();

            return new ReviewUserReportResponseDto
            {
                UserReportId = report.UserReportId,
                PostId = report.PostId,
                Status = report.Status,
                ReviewedAt = report.ReviewedAt,
                ReviewedBy = report.ReviewedBy,
                AdminNote = report.AdminNote,
                PostStatus = report.Post.Status,
                PostVisibility = report.Post.Visibility,
                Message = request.NewStatus == ReportStatus.Resolved
                    ? "Report resolved successfully."
                    : "Report rejected."
            };
        }
    }
}