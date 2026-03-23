using Microsoft.AspNetCore.Http;
using Repositories.Entities;

namespace Services.Implements.OutfitImp
{
    public interface IOutfitService
    {
        Task<Outfit> CreateOutfitAsync(int accountId, string name, IFormFile imageFile);
    }
}
