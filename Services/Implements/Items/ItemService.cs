
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.WardrobeRepos;
using Repositories.UnitOfWork;
using Services.AI;
using Services.Implements.Auth;
using Services.Mappers;
using Services.Utils;
using Services.Utils.File;
﻿using Mapster;
using Repositories.Dto;
using Repositories.Entities;
using Repositories.Repos.ItemRespos;
using Services.AI;
using Services.Implements.Auth;
using System.Text.Json;
using Services.Request.ItemReq;
using Services.Response.ItemResp;
using Services.Request.ItemRequest;


namespace Services.Implements.Items
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly IGeminiService _geminiService;
        private readonly IFileService _fileService;
        private readonly IWardrobeRepository wardrobeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _uow;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly FashionMapper _mapper;
        public ItemService(IItemRepository itemRepo,
            IAiService aiService,
            FashionMapper mapper, 
            IFileService fileService,
            IWardrobeRepository wardrobeRepository, 
            ICurrentUserService currentUserService,
            IUnitOfWork uow,
            ICloudStorageService cloudStorageService,
            IGeminiService geminiService)
        
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _fileService = fileService;
            this.wardrobeRepository = wardrobeRepository;
            _currentUserService = currentUserService;
            _uow = uow;
            _cloudStorageService = cloudStorageService;
            _geminiService = geminiService;
            _currentUserService = currentUserService;
            _mapper = mapper;

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

        public async Task<ItemResponseDto> CreateFashionItemAsync(Request.ItemReq.ProductUploadDto dto, int accountId)
        {

            var wardrobe = await wardrobeRepository.GetById(accountId);
            if (dto.PrimaryImageUrl == null) throw new ArgumentException("File is required");

            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto,dto.PrimaryImageUrl);

            var newItem = dto.Adapt<Item>();
            newItem.WardrobeId = wardrobe.WardrobeId;
            newItem.ItemEmbedding = vectorObject;
            newItem.Status = ItemStatus.Active;

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

        public async Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountid)
        {
            var wardrobe = await wardrobeRepository.GetById(accountid);
            var items = await _itemRepo.GetByWardrobeIdAsync(wardrobe.WardrobeId);

            return items.Select(item => _mapper.ToResponse(item));
        }

        public async Task UpdateItem(int itemId, UpdateItemRequest request)
        {
            var currentUserId = _currentUserService.GetUserId()??0;
            if (currentUserId == 0) throw new Exception("user phai dang nhap");
            var item = await _itemRepo.GetByIdAsync(itemId);
            if (item == null) throw new Exception("ko thay item");
            if (item.Wardrobe.Account.Id != currentUserId) throw new Exception("ban ko co quyen sua do cua nguoi khac");
            item.ItemName = request.ItemName;
            item.ItemType = request.ItemType;
            item.Category = request.Category;
            item.Description = request.Description;
            item.Gender = request.Gender;
            item.SleeveLength = request.SleeveLength;
            item.Pattern = request.Pattern;
            item.SubCategory = request.SubCategory;
            item.Style = request.Style; 
            item.Fit = request.Fit;
            item.Neckline = request.Neckline;
            item.Status = request.Status;
            item.IsPublic  = request.IsPublic;
            item.MainColor = request.MainColor;
            item.Length = request.Length;
            item.Brand = request.Brand;
            item.Material = request.Material;
            item.SubColor = request.SubColor;
            _itemRepo.Update(item);
            await _uow.CommitAsync();
        }

        public async Task DeleteItem(int itemId)
        {
            var currentUserId = _currentUserService.GetUserId() ?? 0;
            if (currentUserId == 0) throw new Exception("user phai dang nhap");
            var item = await _itemRepo.GetByIdAsync(itemId);
            if (item == null) throw new Exception("ko thay item");
            if (item.Wardrobe.Account.Id != currentUserId) throw new Exception("ban ko co quyen xoa do cua nguoi khac");

            try
            {
                if (item.Images != null && item.Images.Any())
                {
                    foreach (var img in item.Images)
                    {
                        await _cloudStorageService.DeleteImageAsync(img.ImageUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi xóa ảnh trên Cloudinary: {ex.Message}");
            }

            _itemRepo.Delete(item);
            await _uow.CommitAsync();
        }
    }
}