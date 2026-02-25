using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;
using Repositories.Repos.WardrobeRepos;
using Services.Request.WardrobeReq;
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
    }
}
