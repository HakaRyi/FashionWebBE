using Microsoft.AspNetCore.Http;
using Repositories.Entities;
using Repositories.Repos.OutfitRepos;
using Services.Utils;

namespace Services.Implements.OutfitImp
{
    public class OutfitService : IOutfitService
    {
        private readonly IOutfitRepository _outfitRepo;
        private readonly ICloudStorageService _storageService;

        public OutfitService(IOutfitRepository outfitRepo, ICloudStorageService storageService)
        {
            _outfitRepo = outfitRepo;
            _storageService = storageService;
        }

        public async Task<Outfit> CreateOutfitAsync(int accountId, string name, IFormFile imageFile)
        {
            var imageUrl = await _storageService.UploadImageAsync(imageFile);

            var newOutfit = new Outfit
            {
                AccountId = accountId,
                OutfitName = name,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _outfitRepo.AddAsync(newOutfit);
            return newOutfit;
        }
    }
}
