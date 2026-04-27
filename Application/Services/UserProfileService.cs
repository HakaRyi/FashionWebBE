using Application.Interfaces;
using Application.Request.AccountReq;
using Application.Response.AccountRep;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IPhysicalProfileRepository _physicalRepo;
        private readonly IUserPreferenceRepository _prefRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuthService _authService;

        public UserProfileService(
            IAccountRepository accountRepo,
            IPhysicalProfileRepository physicalRepo,
            IUserPreferenceRepository prefRepo,
            ICurrentUserService currentUser,
            IAuthService authService)
        {
            _accountRepo = accountRepo;
            _physicalRepo = physicalRepo;
            _prefRepo = prefRepo;
            _currentUser = currentUser;
            _authService = authService;
        }

        public async Task<UserProfileResponseDto?> GetUserProfileAsync()
        {
            var accountId = GetCurrentUserId();

            // Lấy thông tin Account (Dùng hàm đã Include Avatars/ExpertProfile nếu cần)
            var account = await _accountRepo.GetAccountById(accountId);
            if (account == null) return null;

            // Lấy số đo hiện tại
            var physical = await _physicalRepo.GetCurrentByAccountIdAsync(accountId);

            // Lấy danh sách sở thích
            var preferences = await _prefRepo.GetByAccountIdAsync(accountId);

            return new UserProfileResponseDto
            {
                Id = account.Id,
                Email = account.Email,
                UserName = account.UserName,
                Gender = account.Gender,
                DateOfBirth = account.DateOfBirth,
                HasCompletedOnboarding = account.HasCompletedOnboarding,

                Height = physical?.Height,
                Weight = physical?.Weight,
                Waist = physical?.Waist,
                Hip = physical?.Hip,
                Bust = physical?.Bust,
                BodyShape = physical?.BodyShape,
                SkinTone = physical?.SkinTone,

                FavoriteStyles = preferences
                    .Where(p => p.PreferenceType == PreferenceTypes.Style)
                    .Select(p => p.Value ?? "").ToList(),
                FavoriteColors = preferences
                    .Where(p => p.PreferenceType == PreferenceTypes.Color)
                    .Select(p => p.Value ?? "").ToList(),
                FavoriteMaterials = preferences
                    .Where(p => p.PreferenceType == PreferenceTypes.Material)
                    .Select(p => p.Value ?? "").ToList(),
                FavoriteBrands = preferences
                    .Where(p => p.PreferenceType == PreferenceTypes.Brand)
                    .Select(p => p.Value ?? "").ToList()
            };
        }

        // --- 2. HÀM ONBOARDING (LẦN ĐẦU) ---
        public async Task<AuthResponse> CompleteOnboardingAsync(OnboardingRequestDto request)
        {
            var accountId = GetCurrentUserId();
            var account = await _accountRepo.GetAccountById(accountId);
            if (account == null) return new AuthResponse { Success = false };

            account.Gender = request.Gender;
            account.HasCompletedOnboarding = true;
            await _accountRepo.UpdateAccount(account);

            await UpdatePhysicalInternalAsync(accountId, request.Height, request.Weight, request.Waist, request.Hip, request.Bust, request.BodyShape, request.SkinTone);
            await UpdatePreferencesInternalAsync(accountId, request.FavoriteStyles, request.FavoriteColors, request.FavoriteMaterials, request.FavoriteBrands);

            var accessToken = await _authService.GenerateAccessToken(account);

            return new AuthResponse
            {
                Success = true,
                AccessToken = accessToken,
                HasCompletedOnboarding = true
            };
        }

        // --- 3. HÀM EDIT (CẬP NHẬT SAU NÀY) ---
        public async Task<bool> UpdateUserProfileAsync(UpdateUserProfileDto request)
        {
            var accountId = GetCurrentUserId();
            var account = await _accountRepo.GetAccountById(accountId);
            if (account == null) return false;

            // Cập nhật thông tin cơ bản
            account.Gender = request.Gender;
            account.DateOfBirth = request.DateOfBirth;
            await _accountRepo.UpdateAccount(account);

            // Cập nhật Physical & Preferences
            await UpdatePhysicalInternalAsync(accountId, request.Height, request.Weight, request.Waist, request.Hip, request.Bust, request.BodyShape, request.SkinTone);
            await UpdatePreferencesInternalAsync(accountId, request.FavoriteStyles, request.FavoriteColors, request.FavoriteMaterials, request.FavoriteBrands);

            return true;
        }

        // --- CÁC HÀM TRỢ GIÚP NỘI BỘ (REUSABLE LOGIC) ---

        private async Task UpdatePhysicalInternalAsync(int accountId, double? h, double? w, double? waist, double? hip, double? bust, string? shape, string? skin)
        {
            // Chỉ tạo bản ghi mới nếu có ít nhất 1 thông số được gửi lên
            if (h.HasValue || w.HasValue || waist.HasValue || hip.HasValue || bust.HasValue || !string.IsNullOrEmpty(shape))
            {
                var profile = new PhysicalProfile
                {
                    AccountId = accountId,
                    Height = h,
                    Weight = w,
                    Waist = waist,
                    Hip = hip,
                    Bust = bust,
                    BodyShape = shape,
                    SkinTone = skin
                };
                await _physicalRepo.AddAsync(profile);
            }
        }

        private async Task UpdatePreferencesInternalAsync(int accountId, List<string>? styles, List<string>? colors, List<string>? materials, List<string>? brands)
        {
            // Dùng hàm Replace để dọn sạch data cũ theo từng loại
            if (styles != null)
                await _prefRepo.ReplacePreferencesAsync(accountId, PreferenceTypes.Style, styles);

            if (colors != null)
                await _prefRepo.ReplacePreferencesAsync(accountId, PreferenceTypes.Color, colors);
            if (materials != null)
                await _prefRepo.ReplacePreferencesAsync(accountId, PreferenceTypes.Material, materials);

            if (brands != null)
                await _prefRepo.ReplacePreferencesAsync(accountId, PreferenceTypes.Brand, brands);
        
        }

        private int GetCurrentUserId()
        {
            var id = _currentUser.GetUserId();
            if (id == null || id == 0) throw new UnauthorizedAccessException("User context is missing.");
            return (int)id;
        }
    }
}