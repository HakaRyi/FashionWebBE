using Application.Interfaces;
using Application.Request.ExpertReq;
using Application.Response.ExpertResp;
using Application.Services.NotificationImp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace Application.Services
{
    public class ExpertService : IExpertService
    {
        private readonly IExpertProfileRepository _profileRepo;
        private readonly IExpertRequestRepository _fileRepo;
        private readonly IReputationHistoryRepository _reputationHistory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly UserManager<Account> _userManager;
        private readonly INotificationService _notificationService;


        public ExpertService(
            IExpertProfileRepository profileRepo,
            IExpertRequestRepository fileRepo,
            IUnitOfWork unitOfWork,
            IReputationHistoryRepository reputationHistory,
            ICurrentUserService currentUser,
            UserManager<Account> userManager,
            INotificationService notificationService)
        {
            _profileRepo = profileRepo;
            _fileRepo = fileRepo;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userManager = userManager;
            _reputationHistory = reputationHistory;
            _notificationService = notificationService;
        }

        #region Expert Logic
        public async Task<bool> RegisterExpertAsync(ExpertRegistrationDto dto)
        {
            var userId = _currentUser.GetUserId();
            if (userId == null) throw new UnauthorizedAccessException("Cần đăng nhập.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var profile = await _profileRepo.GetByAccountIdAsync(userId.Value);
                bool isNewProfile = profile == null;

                if (isNewProfile)
                {
                    profile = new ExpertProfile
                    {
                        AccountId = userId.Value,
                        CreatedAt = DateTime.UtcNow,
                        Verified = false
                    };
                }

                profile.ExpertiseField = dto.Style;
                profile.StyleAesthetic = dto.StyleAesthetic;
                profile.YearsOfExperience = dto.YearsOfExperience;
                profile.Bio = dto.Bio;
                profile.UpdatedAt = DateTime.UtcNow;

                if (isNewProfile) await _profileRepo.AddAsync(profile);
                else _profileRepo.Update(profile);

                await _unitOfWork.SaveChangesAsync();
                

                var existingRequests = await _fileRepo.GetByProfileIdAsync(profile.ExpertProfileId);

                var hasPending = await _fileRepo.AnyPendingRequestAsync(profile.ExpertProfileId);
                if (hasPending)
                {
                    throw new Exception("Bạn đã có một đơn đăng ký đang chờ duyệt. Vui lòng đợi phản hồi từ hệ thống.");
                }

                var newRequest = new Domain.Entities.ExpertRequest
                {
                    ExpertProfileId = profile.ExpertProfileId,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                    ExpertiseField = dto.Style,
                    StyleAesthetic = dto.StyleAesthetic,
                    YearsOfExperience = dto.YearsOfExperience,
                    Bio = dto.Bio,
                    Reason = null
                };

                if (dto.EvidenceType?.ToLower() == "pdf")
                {
                    newRequest.CertificateUrl = dto.PortfolioUrl;
                    newRequest.LicenseUrl = null;
                }
                else
                {
                    newRequest.LicenseUrl = dto.PortfolioUrl;
                    newRequest.CertificateUrl = null;
                }

                await _fileRepo.AddAsync(newRequest);

                var success = await _unitOfWork.SaveChangesAsync() > 0;

                await _unitOfWork.CommitAsync();
                return success;

            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new Exception(ex.Message);
            }
        }

        public async Task<ExpertApplicationStatusDto> GetCurrentApplicationStatusAsync()
        {
            var userId = _currentUser.GetUserId();
            if (userId == null) return new ExpertApplicationStatusDto { Status = "None" };

            var profile = await _profileRepo.GetByAccountIdAsync(userId.Value);
            if (profile == null) return new ExpertApplicationStatusDto { Status = "None" };

            var requests = await _fileRepo.GetListByProfileId(profile.ExpertProfileId);
            var latestRequest = requests.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            if (latestRequest != null)
            {
                return new ExpertApplicationStatusDto
                {
                    Status = latestRequest.Status ?? "Pending",
                    Reason = latestRequest.Reason,
                    ProcessedAt = latestRequest.ProcessedAt,
                    Style = latestRequest.ExpertiseField,
                    StyleAesthetic = latestRequest.StyleAesthetic,
                    YearsOfExperience = latestRequest.YearsOfExperience,
                    Bio = latestRequest.Bio,
                    PortfolioUrl = latestRequest.CertificateUrl ?? latestRequest.LicenseUrl
                };
            }

            if (profile.Verified == true)
            {
                return new ExpertApplicationStatusDto
                {
                    Status = "Approved",
                    Style = profile.ExpertiseField,
                    StyleAesthetic = profile.StyleAesthetic,
                    YearsOfExperience = profile.YearsOfExperience,
                    Bio = profile.Bio
                };
            }

            return new ExpertApplicationStatusDto { Status = "None" };
        }
        #endregion

        #region Admin Logic

        public async Task<bool> ProcessApplicationAsync(ExpertProcessDto dto)
        {
            var validStatuses = new[] { "Approved", "Rejected" };
            if (!validStatuses.Any(s => s.Equals(dto.Status, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Trạng thái không hợp lệ. Chỉ chấp nhận 'Approved' hoặc 'Rejected'.");


            var file = await _fileRepo.GetById(dto.FileId);
            if (file == null) return false;

            if (!file.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Đơn đăng ký này đã được xử lý trước đó.");

            var profile = await _profileRepo.GetById(file.ExpertProfileId);
            if (profile == null) throw new Exception("Không tìm thấy hồ sơ chuyên gia liên quan.");

            int accountId = profile.AccountId;
            bool isApproved = dto.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Detach(file);
                _unitOfWork.Detach(profile);

                file.Status = isApproved ? "Approved" : "Rejected";
                file.Reason = dto.Reason;
                file.ProcessedAt = DateTime.UtcNow;
                _fileRepo.Update(file);

                if (isApproved)
                {
                    profile.Verified = true;
                    profile.UpdatedAt = DateTime.UtcNow;
                    profile.ReputationScore = 100;
                    _profileRepo.Update(profile);

                    var history = new ReputationHistory
                    {
                        ExpertProfileId = profile.ExpertProfileId,
                        PointChange = 100,
                        CurrentPoint = 100,
                        Reason = "The reputation scoring system is initiated when reviewing Expert profiles.",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _reputationHistory.AddAsync(history);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account != null)
            {
                if (isApproved)
                {
                    if (!await _userManager.IsInRoleAsync(account, "Expert"))
                    {
                        await _userManager.RemoveFromRoleAsync(account, "User");
                        await _userManager.AddToRoleAsync(account, "Expert");
                    }
                }

                await _notificationService.SendNotificationAsync(new Application.Request.NotificationReq.SendNotificationRequest
                {
                    TargetUserId = accountId,
                    SenderId = _currentUser.GetRequiredUserId(),
                    Title = isApproved ? "Congratulations! You have become an Expert!" : "Expert Registration Results",
                    Content = isApproved
                        ? "Your expert profile has been successfully approved. Welcome to our team of experts!"
                        : $"Unfortunately, your expert profile has not been approved. Reason: {dto.Reason ?? "There is no specific reason."}",
                    Type = isApproved ? "Expert_Application_Approved" : "Expert_Application_Rejected",
                    RelatedId = file.ExpertProfileId.ToString()
                });
            }

            return true;
        }

        public async Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback)
        {
            string status = isApproved ? "Approved" : "Rejected";
            var dto = new ExpertProcessDto
            {
                FileId = fileId,
                Status = status,
                Reason = feedback
            };
            return await ProcessApplicationAsync(dto);
        }

        public async Task<IEnumerable<ExpertManagementByAdminDto>> GetAllExpertsAsync()
        {
            var profiles = await _profileRepo.GetAllAsync();

            return profiles.Adapt<IEnumerable<ExpertManagementByAdminDto>>();
        }

        public async Task<IEnumerable<Domain.Entities.ExpertRequest>> GetPendingApplicationsAsync()
        {
            return await _fileRepo.GetStatusApplicationsAsync("Pending");
        }

        #endregion

        #region General Retrieval

        public async Task<ExpertProfile?> GetProfileByAccountId(int accountId)
        {
            return await _profileRepo.GetByAccountIdAsync(accountId);
        }

        public async Task<IEnumerable<ExpertProfile>> GetAllVerifiedExpertsAsync()
        {
            var all = await _profileRepo.GetAllAsync();
            return all.Where(p => p.Verified == true);
        }

        public async Task<bool> DeleteExpertProfileAsync(int profileId)
        {
            await _profileRepo.DeleteAsync(profileId);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ExpertManagementDto>> GetActiveExpertsForUserAsync()
        {
            var profiles = await _profileRepo.GetAllAsync();

            return profiles
                .Where(p => p.Verified == true && p.Account?.Status == "Active")
                .Select(p => new ExpertManagementDto
                {
                    ExpertProfileId = p.ExpertProfileId,
                    AccountId = p.AccountId,
                    UserName = p.Account?.UserName,
                    ExpertiseField = p.ExpertiseField,
                    Bio = p.Bio,
                    StyleAesthetic = p.StyleAesthetic,
                    YearsOfExperience = p.YearsOfExperience,
                    RatingAvg = p.RatingAvg,
                    ReputationScore = p.ReputationScore,
                    Verified = p.Verified
                });
        }

        public async Task<ExpertManagementDto?> GetExpertPublicProfileAsync(int profileId)
        {
            var p = await _profileRepo.GetExpertDetailByIdAsync(profileId);

            if (p == null) return null;

            return new ExpertManagementDto
            {
                ExpertProfileId = p.ExpertProfileId,
                AccountId = p.AccountId,
                UserName = p.Account?.UserName,
                ExpertiseField = p.ExpertiseField,
                Bio = p.Bio,
                StyleAesthetic = p.StyleAesthetic,
                YearsOfExperience = p.YearsOfExperience,
                RatingAvg = p.RatingAvg,
                ReputationScore = p.ReputationScore,
                Verified = p.Verified,
                CreatedAt = p.CreatedAt
            };
        }

        #endregion
    }
}