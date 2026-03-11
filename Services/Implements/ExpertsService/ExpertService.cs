using Repositories.Entities;
using Repositories.Repos.ExpertFileRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.UnitOfWork;
using Services.Response.ExpertResp;

namespace Services.Implements.Experts
{
    public class ExpertService : IExpertService
    {
        private readonly IExpertProfileRepository _profileRepo;
        private readonly IExpertFileRepository _fileRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ExpertService(
            IExpertProfileRepository profileRepo,
            IExpertFileRepository fileRepo,
            IUnitOfWork unitOfWork)
        {
            _profileRepo = profileRepo;
            _fileRepo = fileRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> RegisterExpertAsync(ExpertRegistrationDto dto)
        {
            try
            {
                // 1. Check if profile exists
                var profile = await _profileRepo.GetByAccountIdAsync(dto.AccountId);

                if (profile == null)
                {
                    profile = new ExpertProfile
                    {
                        AccountId = dto.AccountId,
                        ExpertiseField = dto.Style,
                        Bio = dto.Bio,
                        Verified = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _profileRepo.AddAsync(profile);

                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    profile.ExpertiseField = dto.Style;
                    profile.Bio = dto.Bio;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _profileRepo.Update(profile);
                }

                var file = await _fileRepo.GetByProfileIdAsync(profile.ExpertProfileId);
                bool isNewFile = (file == null);

                if (isNewFile)
                {
                    file = new ExpertFile
                    {
                        ExpertProfileId = profile.ExpertProfileId,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                file.Status = "Pending";
                var evidenceType = dto.EvidenceType?.ToLower();

                if (evidenceType == "pdf")
                {
                    file.CertificateUrl = dto.PortfolioUrl;
                }
                else if (evidenceType == "link")
                {
                    file.LicenseUrl = dto.PortfolioUrl;
                }

                if (isNewFile)
                {
                    await _fileRepo.AddAsync(file);
                }
                else
                {
                    _fileRepo.Update(file);
                }

                var result = await _unitOfWork.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi đăng ký chuyên gia: {ex.Message}", ex);
            }
        }

        public async Task<ExpertProfile?> GetProfileByAccountId(int accountId)
        {
            return await _profileRepo.GetByAccountIdAsync(accountId);
        }

        #region Admin Logic
        public async Task<bool> ReviewApplicationAsync(int fileId, bool isApproved, string? feedback)
        {
            var file = await _fileRepo.GetById(fileId);
            if (file == null) return false;

            file.Status = isApproved ? "Approved" : "Rejected";

            _fileRepo.Update(file);

            if (isApproved)
            {
                var profile = await _profileRepo.GetById(file.ExpertProfileId);
                if (profile != null)
                {
                    profile.Verified = true;
                    profile.UpdatedAt = DateTime.UtcNow;
                    _profileRepo.Update(profile);
                }
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ExpertFile>> GetPendingApplicationsAsync()
        {
            var status = "Pending";
            return await _fileRepo.GetStatusApplicationsAsync(status);
        }

        public async Task<bool> ProcessApplicationAsync(int fileId, string status, string? reason)
        {
            var file = await _fileRepo.GetById(fileId);
            if (file == null) return false;

            // Update File Status (Approved / Rejected)
            file.Status = status;
            _fileRepo.Update(file);

            // If approved, verify the profile
            if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                var profile = await _profileRepo.GetById(file.ExpertProfileId);
                if (profile != null)
                {
                    profile.Verified = true;
                    _profileRepo.Update(profile);
                }
            }

            return await _unitOfWork.SaveChangesAsync() > 0;
        }

#endregion

        #region General Retrieval

        public async Task<ExpertProfile?> GetProfileByAccountIdAsync(int accountId)
        {
            return await _profileRepo.GetByAccountIdAsync(accountId);
        }

        public async Task<IEnumerable<ExpertProfile>> GetAllVerifiedExpertsAsync()
        {
            var all = await _profileRepo.GetAllAsync();
            return all.Where(p => (bool)p.Verified);
        }

        public async Task<bool> DeleteExpertProfileAsync(int profileId)
        {
            await _profileRepo.DeleteAsync(profileId);
            return await _unitOfWork.SaveChangesAsync() > 0;
        }

        #endregion
    }
}