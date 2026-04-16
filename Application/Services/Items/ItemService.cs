using Application.Interfaces;
using Mapster;
using Pgvector;
using Domain.Dto;
using Domain.Dto.Wardrobe;
using Domain.Entities;
using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Application.Utils;
using Application.Utils.File;
using Domain.Interfaces;

namespace Application.Services.Items
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly IGeminiService _geminiService;
        private readonly IWardrobeRepository _wardrobeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IImageRepository _imageRepository;
        private readonly IRecommendationHistoryRepository _recommendationHistoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IItemSaveRepository _itemSaveRepo;

        public ItemService(
            IItemRepository itemRepo,
            IAiService aiService,
            IWardrobeRepository wardrobeRepository,
            ICurrentUserService currentUserService,
            ICloudStorageService cloudStorageService,
            IGeminiService geminiService,
            IImageRepository imageRepository,
            IRecommendationHistoryRepository recommendationHistoryRepository,
            IUnitOfWork unitOfWork,
            IItemSaveRepository itemSaveRepo)
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _wardrobeRepository = wardrobeRepository;
            _currentUserService = currentUserService;
            _cloudStorageService = cloudStorageService;
            _geminiService = geminiService;
            _imageRepository = imageRepository;
            _recommendationHistoryRepository = recommendationHistoryRepository;
            _unitOfWork = unitOfWork;
            _itemSaveRepo = itemSaveRepo;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync()
        {
            var items = await _itemRepo.GetAllAsync();
            return items.Adapt<IEnumerable<ItemResponseDto>>();
        }

        public async Task<ItemResponseDto?> GetItemByIdAsync(int id)
        {
            var item = await _itemRepo.GetByIdAsync(id);
            return item?.Adapt<ItemResponseDto>();
        }

        public async Task<PublicWardrobeItemDetailDto?> GetPublicItemDetailAsync(int itemId)
        {
            var item = await _itemRepo.GetPublicItemDetailAsync(itemId);
            if (item == null)
                return null;

            var avatar = await _imageRepository.GetNewestAvatarAsync(item.AccountId);
            item.OwnerAvatarUrl = avatar?.ImageUrl;

            return item;
        }

        public async Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto, int accountId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.PrimaryImageUrl))
                throw new ArgumentException("PrimaryImageUrl là bắt buộc.");

            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                throw new InvalidOperationException("Người dùng chưa có tủ đồ.");

            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto, dto.PrimaryImageUrl);

            var newItem = dto.Adapt<Item>();
            newItem.WardrobeId = wardrobe.WardrobeId;
            newItem.ItemEmbedding = vectorObject;
            newItem.Status = ItemStatus.Active;
            newItem.CreatedAt = DateTime.UtcNow;

            var imageRecord = new Image
            {
                ImageUrl = dto.PrimaryImageUrl,
                OwnerType = "Item",
                CreatedAt = DateTime.UtcNow,
                Item = newItem
            };

            newItem.Images.Add(imageRecord);

            await _itemRepo.AddAsync(newItem);
            await _itemRepo.SaveChangesAsync();

            return newItem.Adapt<ItemResponseDto>();
        }

        public async Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return new List<ItemResponseDto>();

            Vector queryVector = await _aiService.GetTextEmbeddingAsync(prompt);
            var candidates = await _itemRepo.GetByVectorSimilarityAsync(queryVector, limit: 10);

            return candidates.Adapt<List<ItemResponseDto>>();
        }

        public async Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request)
        {
            Console.WriteLine($"DEBUG: IncludeMyWardrobe = {request.IncludeMyWardrobe}");
            if (request == null)
                return new List<ItemResponseDto>();

            if (string.IsNullOrWhiteSpace(request.Prompt) &&
                (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return new List<ItemResponseDto>();
            }

            string taskInstruction = $"Convert user request '{request.Prompt}' into structured metadata.";
            var scopeRequestForRepo = request.Adapt<SmartRecommendationDto>();

            if (request.ReferenceItemId.HasValue && request.ReferenceItemId.Value > 0)
            {
                var referenceItem = await _itemRepo.GetByIdAsync(request.ReferenceItemId.Value);

                if (referenceItem != null)
                {
                    taskInstruction = $@"
The user owns this reference item: A {referenceItem.MainColor} {referenceItem.Category} 
(Style: {referenceItem.Style}, Material: {referenceItem.Material}). 
User request: '{request.Prompt}'.

TASK: Recommend ONE complementary fashion item that creates a good outfit with the reference item.
CRITICAL: Do NOT output metadata for the reference item. Output metadata ONLY for the recommended item.";

                    scopeRequestForRepo.ReferenceCategory = referenceItem.Category;
                }
            }

            var intent = await _geminiService.AnalyzePromptAsync(taskInstruction);
            Vector queryVector = await _aiService.GetTextEmbeddingAsync(intent.CleanPrompt);

            int currentAccountId = _currentUserService.GetRequiredUserId();
            scopeRequestForRepo.IncludeSavedItems = request.IncludeSavedItems;
            var candidates = await _itemRepo.GetHybridRecommendationsAsync(
                queryVector,
                intent,
                currentAccountId,
                scopeRequestForRepo
            );

            if (!candidates.Any())
            {
                return new List<ItemResponseDto>();
            }
            else
            {
                var history = new RecommendationHistory
                {
                    AccountId = currentAccountId,
                    ReferenceItemId = request.ReferenceItemId,
                    Prompt = request.Prompt,
                    CreatedAt = DateTime.UtcNow,
                    RecommendedItems = candidates.Select(c => new RecommendationDetail
                    {
                        ItemId = c.ItemId
                    }).ToList()
                };
                await _recommendationHistoryRepository.AddAsync(history);
                await _unitOfWork.CommitAsync();
            }


                return candidates.Adapt<List<ItemResponseDto>>();
        }

        public async Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountId)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return new List<ItemResponseDto>();

            var items = await _itemRepo.GetByWardrobeIdAsync(wardrobe.WardrobeId);
            return items.Adapt<List<ItemResponseDto>>();
        }

        public async Task UpdateItemAsync(int itemId, UpdateItemRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdForUpdateAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Không tìm thấy item.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền sửa item này.");

            item.ItemName = request.ItemName;
            item.ItemType = request.ItemType;
            item.Category = request.Category;
            item.SubCategory = request.SubCategory;
            item.Description = request.Description;
            item.Gender = request.Gender;
            item.SleeveLength = request.SleeveLength;
            item.Pattern = request.Pattern;
            item.Style = request.Style;
            item.Fit = request.Fit;
            item.Size = request.Size;
            item.Neckline = request.Neckline;
            item.Status = request.Status;
            item.IsPublic = request.IsPublic;
            item.MainColor = request.MainColor;
            item.SubColor = request.SubColor;
            item.Length = request.Length;
            item.Brand = request.Brand;
            item.Material = request.Material;
            item.UpdateAt = DateTime.UtcNow;

            await _itemRepo.SaveChangesAsync();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdForUpdateAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Không tìm thấy item.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa item này.");

            if (item.Images != null && item.Images.Any())
            {
                foreach (var img in item.Images)
                {
                    try
                    {
                        await _cloudStorageService.DeleteImageAsync(img.ImageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Không thể xóa ảnh cloud: {ex.Message}");
                    }
                }
            }

            _itemRepo.Delete(item);
            await _itemRepo.SaveChangesAsync();
        }
    }
}