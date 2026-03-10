using Pgvector;
using Repositories.Repos.ItemRespos;
using Services.AI;
using Services.Mappers;
using Services.Response.ItemResp;
using Services.Utils.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Items
{
    public class ItemService: IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IAiService _aiService;
        private readonly FashionMapper _mapper;
        private readonly IFileService _fileService;

        public ItemService(IItemRepository itemRepo, IAiService aiService, FashionMapper mapper, IFileService fileService)
        {
            _itemRepo = itemRepo;
            _aiService = aiService;
            _mapper = mapper;
            _fileService = fileService;
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

        public async Task<ItemResponseDto> CreateFashionItemAsync(ProductUploadDto dto)
        {
            //if (dto.File == null) throw new ArgumentException("File is required");
            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto.PrimaryImageUrl, dto.Description);

            var newItem = _mapper.ToEntity(dto);
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
    }
}
