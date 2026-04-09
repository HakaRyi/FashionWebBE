using System.Linq;
using Application.Interfaces;
using Application.Request.WardrobeReq;
using Application.Response.ItemResp;
using Application.Response.WardrobeResp;
using Domain.Dto.Common;
using Domain.Dto.Wardrobe;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.Wardrobe
{
    public class WardrobeService : IWardrobeService
    {
        private readonly IWardrobeRepository _wardrobeRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IImageRepository _imageRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IItemSaveRepository _itemSaveRepository;

        public WardrobeService(
            IWardrobeRepository wardrobeRepository,
            IItemRepository itemRepository,
            IAccountRepository accountRepository,
            ICurrentUserService currentUserService,
            IImageRepository imageRepository,
            IItemSaveRepository itemSaveRepository)
        {
            _wardrobeRepository = wardrobeRepository;
            _itemRepository = itemRepository;
            _accountRepository = accountRepository;
            _imageRepository = imageRepository;
            _currentUserService = currentUserService;
            _itemSaveRepository = itemSaveRepository;
        }

        public async Task<int> CreateAsync(WardrobeRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingWardrobe = await _wardrobeRepository.GetByAccountIdAsync(request.AccountId);
            if (existingWardrobe != null)
                throw new InvalidOperationException("Tài khoản này đã có tủ đồ.");

            var wardrobe = new Domain.Entities.Wardrobe
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

        public async Task<List<ItemDto>> GetMyWardrobeItemsAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
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
            var currentUserId = _currentUserService.GetUserId()??0;
            if (currentUserId==0)
            {
                throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem tủ đồ công khai.");
            }

            var totalPublicItems = await _itemRepository.CountPublicItemsByAccountIdAsync(accountId);
            var items = await _itemRepository.GetPublicItemsByAccountIdAsync(accountId, page, pageSize);
            var savedItemIds = new HashSet<int>();
            if (currentUserId!=0)
            {
                var savedItems = await _itemSaveRepository.GetMySaveItems(currentUserId);
                savedItemIds = savedItems.Select(s => s.ItemId).ToHashSet();
            }

            foreach (var item in items)
            {
                item.IsSaved = savedItemIds.Contains(item.ItemId);
                item.IsOwner = currentUserId !=0 && currentUserId == accountId;
            }
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
        public async Task<List<WardrobeSearchResponseDto>> SearchWardrobeByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return new List<WardrobeSearchResponseDto>();

            var accounts = await _wardrobeRepository.SearchAccountWithWardrobeAsync(username);
            return accounts.Select(a => new WardrobeSearchResponseDto
            {
                WardrobeId = a.Wardrobe?.WardrobeId ?? 0,
                UserName = a.UserName,
                AvatarUrl = a.Avatars.OrderByDescending(v => v.CreatedAt)
                                     .Select(v => v.ImageUrl)
                                     .FirstOrDefault()
            }).Where(x => x.WardrobeId > 0).ToList();
        }
    }
}