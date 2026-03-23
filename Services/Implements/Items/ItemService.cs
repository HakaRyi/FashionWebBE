using Pgvector;
using Repositories.Repos.ItemRespos;
using Repositories.Repos.WardrobeRepos;
using Services.AI;
using Services.Mappers;
using Services.Response.ItemResp;
using Services.Utils.File;

namespace Services.Implements.Items
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly FashionMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IWardrobeRepository wardrobeRepository;

        public ItemService(IItemRepository itemRepo, IAiService aiService, FashionMapper mapper, IFileService fileService, IWardrobeRepository wardrobeRepository)
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _mapper = mapper;
            _fileService = fileService;
            this.wardrobeRepository = wardrobeRepository;
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
    }
}
