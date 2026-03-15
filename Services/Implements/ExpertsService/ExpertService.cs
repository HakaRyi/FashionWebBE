using Microsoft.AspNetCore.Identity;
using Repositories.Entities;
using Repositories.Repos.ExpertRequestRepos;
using Repositories.Repos.ExpertProfileRepos;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly UserManager<Account> _userManager;

        public ExpertService(
            IExpertProfileRepository profileRepo,
            IExpertRequestRepository fileRepo,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            UserManager<Account> userManager)
        {
            _profileRepo = profileRepo;
            _fileRepo = fileRepo;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _userManager = userManager;
        }

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
                        CreatedAt = DateTime.UtcNow
                    };
                }

                profile.ExpertiseField = dto.Style;
                profile.StyleAesthetic = dto.StyleAesthetic;
                profile.YearsOfExperience = dto.YearsOfExperience;
                profile.Bio = dto.Bio;
                profile.Verified = false;
                profile.UpdatedAt = DateTime.UtcNow;

                if (isNewProfile) await _profileRepo.AddAsync(profile);
                else _profileRepo.Update(profile);

                await _unitOfWork.SaveChangesAsync();

                var file = await _fileRepo.GetByProfileIdAsync(profile.ExpertProfileId);
                bool isNewFile = (file == null);

                if (isNewFile)
                {
                    file = new Repositories.Entities.ExpertRequest
                    {
                        ExpertProfileId = profile.ExpertProfileId,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                file.Status = "Pending";

                if (dto.EvidenceType?.ToLower() == "pdf")
                {
                    file.CertificateUrl = dto.PortfolioUrl;
                    file.LicenseUrl = null;
                }
                else
                {
                    file.LicenseUrl = dto.PortfolioUrl;
                    file.CertificateUrl = null;
                }

                if (isNewFile) await _fileRepo.AddAsync(file);
                else _fileRepo.Update(file);

                return await _unitOfWork.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi đăng ký Expert: {ex.Message}");
            }
        }

        #region Admin Logic

        public async Task<bool> ProcessApplicationAsync(int fileId, string status, string? reason)
        {
            var file = await _fileRepo.GetById(fileId);
            if (file == null) return false;

            using (var transaction = await _unitOfWork.BeginTransactionAsync())
            {
                try
                {
                    file.Status = status;
                    _fileRepo.Update(file);

                    if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        var profile = await _profileRepo.GetById(file.ExpertProfileId);
                        if (profile != null)
                        {
                            profile.Verified = true;
                            profile.UpdatedAt = DateTime.UtcNow;
                            _profileRepo.Update(profile);

                            var account = await _userManager.FindByIdAsync(profile.AccountId.ToString());
                            if (account != null)
                            {
                                if (!await _userManager.IsInRoleAsync(account, "Expert"))
                                {
                                    var removeResult = await _userManager.RemoveFromRoleAsync(account, "User");
                                    var addResult = await _userManager.AddToRoleAsync(account, "Expert");

                                    if (!addResult.Succeeded)
                                    {
                                        throw new Exception("Không thể cập nhật quyền Expert cho người dùng.");
                                    }
                                }
                            }
                        }
                    }

                    var saveResult = await _unitOfWork.SaveChangesAsync() > 0;

                    await transaction.CommitAsync();
                    return saveResult;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback)
        {
            string status = isApproved ? "Approved" : "Rejected";
            return await ProcessApplicationAsync(fileId, status, feedback);
        }

        public async Task<IEnumerable<ExpertProfile>> GetAllExpertsAsync()
        {
            return await _profileRepo.GetAllAsync();
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

        #endregion
    }
}