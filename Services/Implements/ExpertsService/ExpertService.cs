using Mapster;
using Microsoft.AspNetCore.Identity;
using Repositories.Entities;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.ExpertRequestRepos;
using Repositories.Repos.ReputationHistoryRepos;
using Repositories.UnitOfWork;
using Services.Implements.Auth;
using Services.Request.ExpertReq;
using Services.Response.ExpertResp;

namespace Services.Implements.Experts
{
    public class ExpertService : IExpertService
    {
        private readonly IExpertProfileRepository _profileRepo;
        private readonly IExpertRequestRepository _fileRepo;
        private readonly IReputationHistoryRepository _reputationHistory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly UserManager<Account> _userManager;

        public ExpertService(
            IExpertProfileRepository profileRepo,
            IExpertRequestRepository fileRepo,
            IUnitOfWork unitOfWork,
            IReputationHistoryRepository reputationHistory,
            ICurrentUserService currentUser,
            UserManager<Account> userManager)
        {
            _profileRepo = profileRepo;
            _fileRepo = fileRepo;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userManager = userManager;
            _reputationHistory = reputationHistory;
        }

        #region Expert Logic
        public async Task<bool> RegisterExpertAsync(ExpertRegistrationDto dto)
        {
            var userId = _currentUser.GetUserId();
            if (userId == null) throw new UnauthorizedAccessException("Cần đăng nhập.");

            try
            {
                var profile = await _profileRepo.GetByAccountIdAsync(userId.Value);
                bool isNewProfile = (profile == null);

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

                var newRequest = new Repositories.Entities.ExpertRequest
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

                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> GetCurrentApplicationStatusAsync()
        {
            var userId = _currentUser.GetUserId();
            if (userId == null) return "None";

            var profile = await _profileRepo.GetByAccountIdAsync(userId.Value);
            if (profile == null) return "None";

            var hasPending = await _fileRepo.AnyPendingRequestAsync(profile.ExpertProfileId);
            if (hasPending) return "Pending";

            if ((bool)profile.Verified) return "Approved";

            return "None";
        }
        #endregion

        #region Admin Logic

        public async Task<bool> ProcessApplicationAsync(int fileId, string status, string? reason)
        {
            var validStatuses = new[] { "Approved", "Rejected" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            var file = await _fileRepo.GetById(fileId);
            if (file == null) return false;

            if (file.Status != "Pending")
                throw new InvalidOperationException("Đơn đăng ký này đã được xử lý trước đó.");

            int? accountId = null;
            bool isApproved = status.Equals("Approved", StringComparison.OrdinalIgnoreCase);

            using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    file.Status = status;
                    file.Reason = reason;
                    file.ProcessedAt = DateTime.UtcNow;
                    _fileRepo.Update(file);

                    if (isApproved)
                    {
                        var profile = await _profileRepo.GetById(file.ExpertProfileId);
                        if (profile == null) throw new Exception("Không tìm thấy hồ sơ chuyên gia.");

                        accountId = profile.AccountId;
                        profile.Verified = true;
                        profile.UpdatedAt = DateTime.UtcNow;
                        profile.ReputationScore = 10;

                        var history = new ReputationHistory
                        {
                            ExpertProfileId = profile.ExpertProfileId,
                            PointChange = 10,
                            CurrentPoint = 10,
                            Reason = "Hệ thống cấp điểm uy tín khởi tạo khi duyệt hồ sơ Expert.",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _reputationHistory.AddAsync(history);
                        _profileRepo.Update(profile);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            if (isApproved && accountId.HasValue)
            {
                var account = await _userManager.FindByIdAsync(accountId.Value.ToString());
                if (account != null)
                {
                    if (!await _userManager.IsInRoleAsync(account, "Expert"))
                    {
                        await _userManager.RemoveFromRoleAsync(account, "User");
                        var addResult = await _userManager.AddToRoleAsync(account, "Expert");
                    }
                }
            }

            return true;
        }

        public async Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback)
        {
            string status = isApproved ? "Approved" : "Rejected";
            return await ProcessApplicationAsync(fileId, status, feedback);
        }

        public async Task<IEnumerable<ExpertManagementByAdminDto>> GetAllExpertsAsync()
        {
            var profiles = await _profileRepo.GetAllAsync();

            return profiles.Adapt<IEnumerable<ExpertManagementByAdminDto>>();
        }

        public async Task<IEnumerable<Repositories.Entities.ExpertRequest>> GetPendingApplicationsAsync()
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