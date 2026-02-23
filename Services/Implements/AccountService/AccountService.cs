using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ExpertProfileRepos;
using Services.Request.AccountReq;
using Services.Response.AccountRep;

namespace Services.Implements.AccountService
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IExpertProfileRepository _expertProfileRepository;
        private readonly UserManager<Account> _userManager;

        public AccountService(
            IAccountRepository accountRepository,
            IExpertProfileRepository expertProfileRepository,
            UserManager<Account> userManager)
        {
            _accountRepository = accountRepository;
            _expertProfileRepository = expertProfileRepository;
            _userManager = userManager;
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

            return new AccountResponse
            {
                Id = account.Id,
                Username = account.UserName,
                Email = account.Email,
                Avatar = account.Avatar,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = account.CreatedAt,
                Status = account.Status
            };
        }

        public async Task<List<FashionExpertResponse>> GetFashionExpert()
        {
            var experts = await _accountRepository.GetFashionExperts();

            return experts.Select(e => new FashionExpertResponse
            {
                Avatar = e.Avatar,
                AccountId = e.Id,
                ExpertProfileId = e.ExpertProfile?.ExpertProfileId ?? 0,
                FullName = e.UserName,
                Verified = e.ExpertProfile?.Verified ?? false,
                ExpertiseField = e.ExpertProfile?.ExpertiseField,
                Rating = e.ExpertProfile?.ExpertFile?.RatingAvg ?? 0
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
                Avatar = account.Avatar,
                YearsOfExperience = expertProfile.YearsOfExperience,
                Bio = expertProfile.Bio,
                Verified = expertProfile.Verified,
                CreatedAtProfile = expertProfile.CreatedAt,
                UpdatedAtProfile = expertProfile.UpdatedAt
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
                    Avatar = user.Avatar,
                    Role = roles.FirstOrDefault() ?? "User",
                    CreatedAt = user.CreatedAt,
                    Status = user.Status
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
            account.Avatar = request.Avatar;
            account.Status = request.Status;
            account.Email = request.Email;

            // UserManager sẽ lo việc SaveChanges và Validate
            var result = await _userManager.UpdateAsync(account);
            return result.Succeeded ? "Update success" : string.Join(", ", result.Errors.Select(e => e.Description));
        }
    }
}