using Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Application.Request.AccountReq;
using Application.Response.AccountRep;
using Application.Utils;
using Domain.Interfaces;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IExpertProfileRepository _expertProfileRepository;
        private readonly IPostRepository _postRepository;
        private readonly UserManager<Domain.Entities.Account> _userManager;
        private readonly IImageRepository _imageRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly ICacheService _cacheService;

        public AccountService(
            IAccountRepository accountRepository,
            IExpertProfileRepository expertProfileRepository,
            IImageRepository imageRepository,
            IPostRepository postRepository,
            UserManager<Domain.Entities.Account> userManager,
            ICurrentUserService currentUserService,
            ICloudStorageService cloudStorageService,
            ICacheService cacheService)
        {
            _accountRepository = accountRepository;
            _expertProfileRepository = expertProfileRepository;
            _userManager = userManager;
            _imageRepository = imageRepository;
            _postRepository = postRepository;
            _currentUserService = currentUserService;
            _cloudStorageService = cloudStorageService;
            _cacheService = cacheService;

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
            var avatar = await _imageRepository.GetNewestAvatarAsync(account.Id);
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
                Description = account.Description,
                IsOnline = account.IsOnline

            };
        }

        public async Task<AccountUserResponse?> GetUserAccountById(int accountId)
        {
            var account = await _accountRepository.GetAccountWithProfileAndAvatarsAsync(accountId);

            if (account == null) return null;

            var actualPostCount = await _postRepository.CountAccountPostsAsync(accountId);

            if (account.CountPost != actualPostCount)
            {
                account.CountPost = actualPostCount;
                _accountRepository.UpdateAccount(account);
            }

            var roles = await _userManager.GetRolesAsync(account);

            var avatarUrl = account.Avatars
                .OrderByDescending(img => img.CreatedAt)
                .FirstOrDefault()?.ImageUrl;

            var response = new AccountUserResponse
            {
                Id = account.Id,
                Username = account.UserName ?? string.Empty,
                Email = account.Email ?? string.Empty,
                Avatar = avatarUrl,
                Role = roles.FirstOrDefault() ?? "User",
                CreatedAt = account.CreatedAt,
                Status = account.Status,
                FollowerCount = account.CountFollower,
                FollowingCount = account.CountFollowing,
                PostCount = account.CountPost,
                Description = account.Description,
                IsOnline = account.IsOnline,
                IsExpert = account.ExpertProfile != null
            };

            if (account.ExpertProfile != null)
            {
                var profile = account.ExpertProfile;
                response.ReputationScore = profile.ReputationScore;
                response.ExpertiseField = profile.ExpertiseField;
                response.YearsOfExperience = profile.YearsOfExperience;
                response.Rating = profile.RatingAvg;
                response.Bio = profile.Bio;
                response.Verified = profile.Verified;
            }

            return response;
        }

        public async Task<AccountResponse?> GetAccountByMe()
        {
            var accountId = _currentUserService.GetUserId();
            if (accountId == null) return null;
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null) return null;
            var roles = await _userManager.GetRolesAsync(account);
            var avatar = await _imageRepository.GetNewestAvatarAsync(account.Id);
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
                Description = account.Description,
                IsOnline = account.IsOnline


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
                Rating = e.ExpertProfile?.RatingAvg ?? 0,
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
            //var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var users = await _accountRepository.GetAll();
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
                    Description = user.Description,
                    IsOnline = user.IsOnline
                });
            }
            return responses;
        }

        public async Task<string> updateAccountRequest(int accountId, UpdateAccountRequest request)
        {
            var account = await _userManager.FindByIdAsync(accountId.ToString());
            if (account == null) return "User not found";

            account.UserName = request.Username;
            account.Status = request.Status;
            account.Email = request.Email;

            var result = await _userManager.UpdateAsync(account);
            return result.Succeeded ? "Update success" : string.Join(", ", result.Errors.Select(e => e.Description));
        }

        public async Task<bool> CompleteOnboardingAsync(int accountId, OnboardingRequest request)
        {
            var user = await _userManager.FindByIdAsync(accountId.ToString());
            if (user == null) return false;

            user.UserName = request.UserName;
            user.NormalizedUserName = request.UserName.ToUpper();

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<int> updateProfile(UpdateProfileRequest request)
        {
            var currentUserId = _currentUserService.GetUserId()??0;
            if(currentUserId == 0) return -1;
            var user = await _userManager.FindByIdAsync(currentUserId.ToString());
            if (user == null) return 0;
            var existingEmailUser = await _userManager.FindByEmailAsync(request.Email);
            if(existingEmailUser != null && existingEmailUser.Id != user.Id) return -2;
            var existingUserName = await _userManager.FindByNameAsync(request.Username);
            if (existingUserName != null && existingUserName.Id != user.Id) return -3;
            user.UserName = request.Username ?? user.UserName;
            user.NormalizedUserName = (request.Username ?? user.UserName).ToUpper();
            user.Email = request.Email ?? user.Email;
            user.NormalizedEmail = (request.Email ?? user.Email).ToUpper();
            user.Description = request.Description ?? user.Description;
            string? finalAvatarUrl = null;
            if (request.Avatar != null && request.Avatar.Length > 0)
            {
                finalAvatarUrl = await _cloudStorageService.UploadImageAsync(request.Avatar);
            }
            if (!string.IsNullOrEmpty(finalAvatarUrl))
            {
                user.Avatars.Add(new Domain.Entities.Image
                {
                    ImageUrl = finalAvatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    OwnerType = "AccountAvatar",
                    AccountAvatarId = user.Id
                });
            }
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? user.Id : 0;

        }

        public async Task<AccountUserResponse?> GetMyProfileAsync()
        {
            var currentUserId = _currentUserService.GetUserId();
            if (currentUserId == null) return null;

            var accountId = currentUserId.Value;
            var cacheKey = $"my_profile_{accountId}";

            return await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var account = await _accountRepository.GetAccountWithProfileAndAvatarsAsync(accountId);
                    if (account == null) return null;
                    bool isChanged = false;

                    if (isChanged)
                    {
                        await _accountRepository.UpdateAccount(account);
                    }

                    var roles = await _userManager.GetRolesAsync(account);
                    var avatarUrl = account.Avatars.OrderByDescending(img => img.CreatedAt).FirstOrDefault()?.ImageUrl;

                    var response = new AccountUserResponse
                    {
                        Id = account.Id,
                        Username = account.UserName ?? string.Empty,
                        Email = account.Email ?? string.Empty,
                        Avatar = avatarUrl,
                        Role = roles.FirstOrDefault() ?? "User",
                        CreatedAt = account.CreatedAt,
                        Status = account.Status,
                        FollowerCount = account.CountFollower,
                        FollowingCount = account.CountFollowing,
                        PostCount = account.CountPost,
                        Description = account.Description,
                        IsOnline = account.IsOnline,
                        IsExpert = account.ExpertProfile != null
                    };

                    if (account.ExpertProfile != null)
                    {
                        var profile = account.ExpertProfile;
                        response.ReputationScore = profile.ReputationScore;
                        response.ExpertiseField = profile.ExpertiseField;
                        response.YearsOfExperience = profile.YearsOfExperience;
                        response.Rating = profile.RatingAvg;
                        response.Bio = profile.Bio;
                        response.Verified = profile.Verified;
                    }

                    return response;
                },
                TimeSpan.FromDays(7)
            );
        }
    }
}