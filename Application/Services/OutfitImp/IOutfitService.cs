using Application.Request.OufitReq;
using Application.Response.OutfitResp;
using Domain.Entities;
using Microsoft.AspNetCore.Http;


namespace Application.Services.OutfitImp
{
    public interface IOutfitService
    {
        Task<Outfit> CreateOutfitAsync(int accountId, string name, IFormFile imageFile);
        Task<OutfitResponseDto> SaveOutfitAsync(SaveOutfitRequestDto request);
        Task<List<OutfitResponseDto>> GetMyOutfitsAsync();
    }
}
