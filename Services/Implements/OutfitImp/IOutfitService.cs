using Microsoft.AspNetCore.Http;
using Repositories.Entities;
using Services.Request.OufitReq;
using Services.Response.OutfitResp;


namespace Services.Implements.OutfitImp
{
    public interface IOutfitService
    {
        Task<Outfit> CreateOutfitAsync(int accountId, string name, IFormFile imageFile);
        Task<OutfitResponseDto> SaveOutfitAsync(SaveOutfitRequestDto request);
        Task<List<OutfitResponseDto>> GetMyOutfitsAsync();
    }
}
