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

        public WardrobeService(
            IWardrobeRepository wardrobeRepository,
            IItemRepository itemRepository)
        {
            _wardrobeRepository = wardrobeRepository;
            _itemRepository = itemRepository;
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
    }
}