using Mapster;
using Pgvector;
using Repositories.Dto;
using Repositories.Entities;
using Repositories.Repos.ItemRespos;
using Services.AI;
using Services.Implements.Auth;
using Services.Request.ItemReq;
using Services.Response.ItemResp;
using Services.Utils.File;
using System.Text.Json;

namespace Services.Implements.Items
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly IGeminiService _geminiService;
        private readonly IFileService _fileService;
        private readonly ICurrentUserService _currentUserService;


        public ItemService(IItemRepository itemRepo, IAiService aiService, IFileService fileService, IGeminiService geminiService, ICurrentUserService currentUserService)
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _fileService = fileService;
            _geminiService = geminiService;
            _currentUserService = currentUserService;
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

        public async Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto)
        {
            if (dto.File == null) throw new ArgumentException("File is required");

            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto);

            var newItem = dto.Adapt<Item>();
            newItem.ItemEmbedding = vectorObject;
            newItem.Status = ItemStatus.Active;

            string uploadedUrl = await _fileService.UploadAsync(dto.File);
            var imageRecord = new Image
            {
                ImageUrl = uploadedUrl,
                OwnerType = "Item",
                CreatedAt = DateTime.UtcNow
            };

            newItem.Images.Add(imageRecord);

            await _itemRepo.AddAsync(newItem);
            await _itemRepo.SaveChangesAsync();

            return newItem.Adapt<ItemResponseDto>();
        }

        public async Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt)
        {
            Vector queryVector = await _aiService.GetTextEmbeddingAsync(prompt);

            var candidates = await _itemRepo.GetByVectorSimilarityAsync(queryVector, limit: 10);

            return candidates.Adapt<List<ItemResponseDto>>();
        }

        public async Task<List<ItemResponseDto>> GetSmartRecommendationsAsync(SmartRecommendationRequestDto request)
        {

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
                
                TASK: Recommend ONE complementary fashion item that creates list good outfit with the reference item. 
                CRITICAL: Do NOT output metadata for the reference item. Output metadata ONLY for the RECOMMENDED item 
                (e.g., if the reference is UpperBody, suggest LowerBody or Footwear).";

                    scopeRequestForRepo.ReferenceCategory = referenceItem.Category;
                }
            }

            // 1. GỌI AI ĐÚNG 1 LẦN: Lấy Intent và bóc tách cấu trúc
            var intent = await _geminiService.AnalyzePromptAsync(taskInstruction);

            // 2. NHÚNG VECTOR: Lấy CleanPrompt dịch ra Vector
            Vector queryVector = await _aiService.GetTextEmbeddingAsync(intent.CleanPrompt);

            int currentAccountId = _currentUserService.GetRequiredUserId();

            // 3. HYBRID SEARCH: Lọc Scope + Lọc Metadata + Sắp xếp Vector (Đẩy hết gánh nặng cho Database)
            var candidates = await _itemRepo.GetHybridRecommendationsAsync(
                queryVector,
                intent,
                currentAccountId,
                scopeRequestForRepo
            );

            // Không cần hàm Refine nữa! Dữ liệu trả ra đã cực kỳ chính xác nhờ lọc Metadata (bước 2 ở Repo).
            if (!candidates.Any()) return new List<ItemResponseDto>();

            // Trả về thẳng cho Client
            return candidates.Adapt<List<ItemResponseDto>>();
        }
    }
}