using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ExpertProfileRepos;
using Repositories.Repos.ImageRepos;
using Services.Request.AccountReq;
using Services.Response.AccountRep;

namespace Services.Implements.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IExpertProfileRepository _expertProfileRepository;
        private readonly UserManager<Repositories.Entities.Account> _userManager;
        private readonly IImageRepository _imageRepository;

        public AccountService(
            IAccountRepository accountRepository,
            IExpertProfileRepository expertProfileRepository,
            IImageRepository imageRepository,
            UserManager<Repositories.Entities.Account> userManager)
        {
            _accountRepository = accountRepository;
            _expertProfileRepository = expertProfileRepository;
            _userManager = userManager;
            _imageRepository = imageRepository;

        }

        public async Task<int> CountAccount()
        {
            return await _userManager.Users.CountAsync();
        }

        public async Task<int> CountExpert()
        {
            var experts = await _accountRepository.GetFashionExperts();
            return experts.Count;
        }

        public async Task<AccountResponse?> GetAccountById(int accountId)
        {
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null) return null;

            var roles = await _userManager.GetRolesAsync(account);
            var avatar = await _imageRepository.GetNewestAvatar(account.Id);
            return new AccountResponse
            {
                Id = account.Id,
                Username = account.UserName,
                Email = account.Email,
                Avatar = avatar?.ImageUrl ?? null,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = account.CreatedAt,
                Status = account.Status,
                FollowerCount = account.CountFollower,
                FollowingCount = account.CountFollowing,
                PostCount = account.CountPost,
                Description = account.Description

            };
        }

        public async Task<List<FashionExpertResponse>> GetFashionExpert()
        {
            var experts = await _accountRepository.GetFashionExperts();
            return experts.Select(e => new FashionExpertResponse
            {
                Avatar = e.Avatars
                  .OrderByDescending(img => img.CreatedAt)
                  .Select(img => img.ImageUrl)
                  .FirstOrDefault() ?? null,
                AccountId = e.Id,
                ExpertProfileId = e.ExpertProfile?.ExpertProfileId ?? 0,
                FullName = e.UserName,
                Verified = e.ExpertProfile?.Verified ?? false,
                ExpertiseField = e.ExpertProfile?.ExpertiseField,
                Rating = e.ExpertProfile?.ExpertFile?.RatingAvg ?? 0,
                Description = e.Description,
                FollowerCount = e.CountFollower,
                FollowingCount = e.CountFollowing,
                PostCount = e.CountPost
            }).ToList();
        }

        public async Task<FashionExpertDetail> GetFashionExpertDetail(int id)
        {
            var account = await _userManager.FindByIdAsync(id.ToString());
            var expertProfile = await _expertProfileRepository.GetById(id);

            if (account == null || expertProfile == null) return new FashionExpertDetail();

            return new FashionExpertDetail
            {
                AccountId = account.Id,
                ExpertProfileId = expertProfile.ExpertProfileId,
                Username = account.UserName,
                Email = account.Email,
                CreatedAt = account.CreatedAt,
                Status = account.Status,
                Avatar = account.Avatars
                  .OrderByDescending(img => img.CreatedAt)
                  .Select(img => img.ImageUrl)
                  .FirstOrDefault() ?? null,
                YearsOfExperience = expertProfile.YearsOfExperience,
                Bio = expertProfile.Bio,
                Verified = expertProfile.Verified,
                CreatedAtProfile = expertProfile.CreatedAt,
                UpdatedAtProfile = expertProfile.UpdatedAt,
                FollowerCount = account.CountFollower,
                FollowingCount = account.CountFollowing,
                PostCount = account.CountPost,
                Description = account.Description
            };
        }

        public async Task<List<AccountResponse>> GetListAccount()
        {
            // Lấy list user từ UserManager
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var responses = new List<AccountResponse>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                responses.Add(new AccountResponse
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Avatar = user.Avatars
                      .OrderByDescending(img => img.CreatedAt)
                      .Select(img => img.ImageUrl)
                      .FirstOrDefault() ?? null,
                    Role = roles.FirstOrDefault() ?? "User",
                    CreatedAt = user.CreatedAt,
                    Status = user.Status,
                    FollowerCount = user.CountFollower,
                    FollowingCount = user.CountFollowing,
                    PostCount = user.CountPost,
                    Description = user.Description
                });
            }
            return responses;
        }

        public async Task<string> updateAccountRequest(int accountId, UpdateAccountRequest request)
        {
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null) return "User not found";

            // Cập nhật các field
            account.UserName = request.Username;
            //account.Avatar = request.Avatar;
            account.Status = request.Status;
            account.Email = request.Email;

            // UserManager sẽ lo việc SaveChanges và Validate
            var result = await _userManager.UpdateAsync(account);
            return result.Succeeded ? "Update success" : string.Join(", ", result.Errors.Select(e => e.Description));
        }
    }
}