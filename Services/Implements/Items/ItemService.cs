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
using Services.Request.ItemRequest;
using Services.Response.ItemResp;
using Services.Utils;
using Services.Utils.File;

namespace Services.Implements.Items
{
    public class ItemService: IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly FashionMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IWardrobeRepository wardrobeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _uow;
        private readonly ICloudStorageService _cloudStorageService;

        public ItemService(IItemRepository itemRepo,
            IAiService aiService, FashionMapper mapper, 
            IFileService fileService,
            IWardrobeRepository wardrobeRepository, 
            ICurrentUserService currentUserService,
            IUnitOfWork uow,
            ICloudStorageService cloudStorageService)
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _mapper = mapper;
            _fileService = fileService;
            this.wardrobeRepository = wardrobeRepository;
            _currentUserService = currentUserService;
            _uow = uow;
            _cloudStorageService = cloudStorageService;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetAllItemsAsync()
        {
            var items = await _itemRepo.GetAllAsync();

            return items.Select(item => _mapper.ToResponse(item));
        }

        public async Task<ItemResponseDto?> GetItemByIdAsync(int id)
        {
            var item = await _itemRepo.GetByIdAsync(id);

            if (item == null) return null;

            return _mapper.ToResponse(item);
        }

        public async Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto, int accountId)
        {
            var wardrobe = await wardrobeRepository.GetById(accountId);
            //if (dto.File == null) throw new ArgumentException("File is required");
            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto.PrimaryImageUrl, dto.Description);

            var newItem = _mapper.ToEntity(dto);
            newItem.WardrobeId = wardrobe.WardrobeId;
            newItem.ItemEmbedding = vectorObject;
            newItem.Status = "Active";
            newItem.CreatedAt = DateTime.UtcNow;
            var imageRecord = new Repositories.Entities.Image
            {
                ImageUrl = dto.PrimaryImageUrl,
                OwnerType = "Item",
                CreatedAt = DateTime.UtcNow,
                Item = newItem
            };

            newItem.Images.Add(imageRecord);

            await _itemRepo.AddAsync(newItem);

            await _itemRepo.SaveChangesAsync();

            return _mapper.ToResponse(newItem);
        }

        public async Task<List<ItemResponseDto>> GetRecommendationsAsync(string prompt)
        {
            Vector queryVector = await _aiService.GetTextEmbeddingAsync(prompt);

            var candidates = await _itemRepo.GetByVectorSimilarityAsync(queryVector, limit: 10);

            return candidates.Select(c => _mapper.ToResponse(c)).ToList();
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
            item.Texture = request.Texture;
            item.Style = request.Style;
            item.Status = request.Status;
            item.Description = request.Description;
            item.Brand = request.Brand;
            item.Fabric = request.Fabric;
            item.Placement = request.Placement;
            item.Status = request?.Status;
            item.MainColor = request?.MainColor;
            item.Pattern = request?.Pattern;
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
