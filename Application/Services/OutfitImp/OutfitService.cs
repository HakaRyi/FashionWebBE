using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Domain.Entities;
using Application.Request.OufitReq;
using Application.Response.OutfitResp;
using Application.Utils;
using Domain.Interfaces;


namespace Application.Services.OutfitImp
{
    public class OutfitService : IOutfitService
    {
        private readonly IOutfitRepository _outfitRepo;
        private readonly IItemRepository _itemRepo;
        private readonly ICloudStorageService _storageService;
        private readonly ICurrentUserService _currentUserService;

        public OutfitService(
            IOutfitRepository outfitRepo,
            IItemRepository itemRepo,
            ICloudStorageService storageService,
            ICurrentUserService currentUserService)
        {
            _outfitRepo = outfitRepo;
            _itemRepo = itemRepo;
            _storageService = storageService;
            _currentUserService = currentUserService;
        }

        //User TỰ TẠO bộ đồ và tự up ảnh
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

        //LƯU TỪ GỢI Ý (Chỉ lưu ItemId)
        public async Task<OutfitResponseDto> SaveOutfitAsync(SaveOutfitRequestDto request)
        {
            int accountId = _currentUserService.GetRequiredUserId();

            if (request.Items == null || !request.Items.Any())
            {
                throw new Exception("Bộ đồ phải có ít nhất 1 món đồ.");
            }

            var itemIds = request.Items.Select(x => x.ItemId).Distinct().ToList();

            var existingItems = await _itemRepo.GetItemsByIds(itemIds);

            if (existingItems.Count != itemIds.Count)
            {
                throw new Exception("Có món đồ không tồn tại hoặc đã bị xóa khỏi hệ thống.");
            }

            var outfit = new Outfit
            {
                AccountId = accountId,
                OutfitName = string.IsNullOrWhiteSpace(request.OutfitName)
                    ? $"Outfit {DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}"
                    : request.OutfitName,

                ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl,

                OutfitItems = request.Items.Select(i => new OutfitItem
                {
                    ItemId = i.ItemId,
                    Slot = string.IsNullOrWhiteSpace(i.Slot) ? "Unknown" : i.Slot
                }).ToList()
            };

            var savedOutfit = await _outfitRepo.CreateOutfitAsync(outfit);

            return new OutfitResponseDto
            {
                OutfitId = savedOutfit.OutfitId,
                OutfitName = savedOutfit.OutfitName,
                ImageUrl = savedOutfit.ImageUrl,
                CreatedAt = savedOutfit.CreatedAt,
                Items = request.Items.Select(i => new OutfitItemResponseDto
                {
                    ItemId = i.ItemId,
                    Slot = i.Slot
                }).ToList()
            };
        }

        public async Task<List<OutfitResponseDto>> GetMyOutfitsAsync()
        {
            int accountId = _currentUserService.GetRequiredUserId();
            var outfits = await _outfitRepo.GetUserOutfitsAsync(accountId);

            return outfits.Select(o => new OutfitResponseDto
            {
                OutfitId = o.OutfitId,
                OutfitName = o.OutfitName,
                ImageUrl = o.ImageUrl,
                CreatedAt = o.CreatedAt,
                Items = o.OutfitItems.Select(oi => new OutfitItemResponseDto
                {
                    ItemId = oi.ItemId,
                    ItemName = oi.Item.ItemName,
                    Category = oi.Item.Category,
                    Slot = oi.Slot,
                    ImageUrl = oi.Item.Images.FirstOrDefault()?.ImageUrl
                }).ToList()
            }).ToList();
        }
    }
}