using Repositories.Dto.Common;
using Repositories.Dto.Wardrobe;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.WardrobeRepos;
using Services.Request.WardrobeReq;
using Services.Response.ItemResp;
using Services.Response.WardrobeResp;

namespace Services.Implements.Wardrobe
{
    public class WardrobeService : IWardrobeService
    {
        private readonly IWardrobeRepository _wardrobeRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IImageRepository _imageRepository;

        public WardrobeService(
            IWardrobeRepository wardrobeRepository,
            IItemRepository itemRepository,
            IAccountRepository accountRepository,
            IImageRepository imageRepository)
        {
            _wardrobeRepository = wardrobeRepository;
            _itemRepository = itemRepository;
            _accountRepository = accountRepository;
            _imageRepository = imageRepository;
        }

        public async Task<int> CreateAsync(WardrobeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingWardrobe = await _wardrobeRepository.GetByAccountIdAsync(request.AccountId);
            if (existingWardrobe != null)
                throw new InvalidOperationException("Tài khoản này đã có tủ đồ.");

            var wardrobe = new Repositories.Entities.Wardrobe
            {
                AccountId = request.AccountId,
                Name = request.Name?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            return await _wardrobeRepository.CreateWardrobe(wardrobe);
        }

        public async Task<List<WardrobeResponse>> GetAllAsync()
        {
            var wardrobes = await _wardrobeRepository.GetAll();

            return wardrobes.Select(w => new WardrobeResponse
            {
                WardrobeId = w.WardrobeId,
                AccountId = w.AccountId,
                Name = w.Name,
                CreatedAt = w.CreatedAt
            }).ToList();
        }

        public async Task<WardrobeResponse?> GetByAccountIdAsync(int accountId)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null) return null;

            return new WardrobeResponse
            {
                WardrobeId = wardrobe.WardrobeId,
                AccountId = wardrobe.AccountId,
                Name = wardrobe.Name,
                CreatedAt = wardrobe.CreatedAt
            };
        }

        public async Task<List<ItemDto>> GetMyWardrobeItemsAsync(int accountId)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return new List<ItemDto>();

            var items = await _itemRepository.GetByWardrobeIdAsync(wardrobe.WardrobeId);

            return items.Select(i => new ItemDto
            {
                ItemId = i.ItemId,
                ItemName = i.ItemName,
                Description = i.Description,
                MainColor = i.MainColor,
                Brand = i.Brand,
                Status = i.Status?.ToString(),
                ImageUrl = i.Images?
                    .OrderBy(img => img.CreatedAt)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault()
            }).ToList();
        }

        public async Task<PublicProfileDto?> GetPublicProfileAsync(int accountId)
        {
            var account = await _accountRepository.GetAccountById(accountId);
            if (account == null)
                return null;

            var avatar = await _imageRepository.GetNewestAvatarAsync(accountId);
            var totalPublicItems = await _itemRepository.CountPublicItemsByAccountIdAsync(accountId);

            return new PublicProfileDto
            {
                AccountId = account.Id,
                UserName = account.UserName,
                Description = account.Description,
                CountPost = account.CountPost,
                CountFollower = account.CountFollower,
                CountFollowing = account.CountFollowing,
                AvatarUrl = avatar?.ImageUrl,
                TotalPublicItems = totalPublicItems
            };
        }

        public async Task<PublicWardrobeResponseDto?> GetPublicWardrobeAsync(int accountId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 12;

            var account = await _accountRepository.GetAccountById(accountId);
            if (account == null)
                return null;

            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return null;

            var totalPublicItems = await _itemRepository.CountPublicItemsByAccountIdAsync(accountId);
            var items = await _itemRepository.GetPublicItemsByAccountIdAsync(accountId, page, pageSize);

            return new PublicWardrobeResponseDto
            {
                AccountId = accountId,
                WardrobeId = wardrobe.WardrobeId,
                TotalPublicItems = totalPublicItems,
                Items = new PagedResultDto<PublicWardrobeItemDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalPublicItems,
                    HasMore = page * pageSize < totalPublicItems
                }
            };
        }
    }
}