using Application.Interfaces;
using Application.Request.UserReportReq;
using Application.Response.UserReportResp;
using Domain.Constants;
using Domain.Contracts.UserReport;
using Domain.Dto.Common;
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
                throw new ArgumentException("PostId không hợp lệ.");
            }

            if (request.ReportTypeId <= 0)
            {
                throw new ArgumentException("ReportTypeId không hợp lệ.");
            }

            request.Reason = request.Reason?.Trim();

            if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Length > 1000)
            {
                throw new ArgumentException("Reason không được vượt quá 1000 ký tự.");
            }

            var postInfo = await _userReportRepository.GetPostValidationInfoAsync(request.PostId);
            if (postInfo == null)
            {
                throw new KeyNotFoundException("Bài viết không tồn tại.");
            }

            if (postInfo.AccountId == currentUserId)
            {
                throw new InvalidOperationException("Bạn không thể báo cáo bài viết của chính mình.");
            }

            if (postInfo.Status == PostStatus.Deleted || postInfo.Status == PostStatus.Banned)
            {
                throw new InvalidOperationException("Bài viết này hiện không thể báo cáo.");
            }

            var reportType = await _userReportRepository.GetReportTypeByIdAsync(request.ReportTypeId);
            if (reportType == null)
            {
                throw new KeyNotFoundException("Loại báo cáo không hợp lệ.");
            }

            var isAlreadyReported = await _userReportRepository.IsAlreadyReportedAsync(request.PostId, currentUserId);
            if (isAlreadyReported)
            {
                throw new InvalidOperationException("Bạn đã báo cáo bài viết này rồi.");
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
                Message = "Báo cáo bài viết thành công."
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
                throw new ArgumentException("Status không hợp lệ.");
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
                throw new ArgumentException("userReportId không hợp lệ.");
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
                throw new ArgumentException("userReportId không hợp lệ.");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.NewStatus))
            {
                throw new ArgumentException("NewStatus không được để trống.");
            }

            request.NewStatus = request.NewStatus.Trim();

            if (!ReportStatus.IsValid(request.NewStatus))
            {
                throw new ArgumentException("NewStatus không hợp lệ.");
            }

            if (request.NewStatus != ReportStatus.Resolved && request.NewStatus != ReportStatus.Rejected)
            {
                throw new ArgumentException("Admin chỉ được chuyển report sang Resolved hoặc Rejected.");
            }

            request.AdminNote = request.AdminNote?.Trim();

            if (!string.IsNullOrWhiteSpace(request.AdminNote) && request.AdminNote.Length > 1000)
            {
                throw new ArgumentException("AdminNote không được vượt quá 1000 ký tự.");
            }

            var report = await _userReportRepository.GetByIdAsync(userReportId);
            if (report == null)
            {
                throw new KeyNotFoundException("Không tìm thấy báo cáo.");
            }

            if (!ReportStatus.CanTransition(report.Status, request.NewStatus))
            {
                throw new InvalidOperationException(
                    $"Không thể chuyển trạng thái từ '{report.Status}' sang '{request.NewStatus}'.");
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
                        throw new ArgumentException("PostStatusToApply không hợp lệ.");
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
                    ? "Xử lý báo cáo thành công."
                    : "Đã từ chối báo cáo."
            };
        }
    }
}