using Repositories.Repos.WardrobeRepos;
using Services.Request.WardrobeReq;
using Services.Response.ItemResp;
using Services.Response.WardrobeResp;

namespace Services.Implements.Wardrobe
{
    public class WardrobeService : IWardrobeService
    {
        private readonly IWardrobeRepository repo;
        public WardrobeService(IWardrobeRepository repo)
        {
            this.repo = repo;
        }

        public async Task<int> Create(WardrobeRequest request)
        {
            var wardrobe = new Repositories.Entities.Wardrobe
            {
                AccountId = request.AccountId,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            return await repo.CreateWardrobe(wardrobe);
        }

        public async Task<List<WardrobeResponse>> GetAll()
        {
            var wardrobes = await repo.GetAll();
            return wardrobes.Select(w => new WardrobeResponse
            {
                WardrobeId = w.WardrobeId,
                AccountId = w.AccountId,
                Name = w.Name,
                CreatedAt = w.CreatedAt
            }).ToList();
        }

        public async Task<WardrobeResponse> GetById(int id)
        {
            var w = await repo.GetById(id);
            if (w == null) return new WardrobeResponse();

            return new WardrobeResponse
            {
                WardrobeId = w.WardrobeId,
                AccountId = w.AccountId,
                Name = w.Name,
                CreatedAt = w.CreatedAt
            };
        }

        public async Task<List<ItemDto>> GetMyWardrobeItemsAsync(int accountId)
        {
            var wardrobe = await repo.GetWardrobeByAccount(accountId);

            if (wardrobe == null || wardrobe.Items == null)
            {
                return new List<ItemDto>();
            }

            return wardrobe.Items.Select(i => new ItemDto
            {
                ItemId = i.ItemId,
                ItemName = i.ItemName,
                Description = i.Description,
                MainColor = i.MainColor,
                Brand = i.Brand,
                Status = i.Status.ToString(),
                ImageUrl = i.Images.FirstOrDefault().ImageUrl,
            }).ToList();
        }
    }
}
