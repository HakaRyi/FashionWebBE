using Application.Interfaces;
using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Application.Utils;
using Application.Utils.File;
using Domain.Constants;
using Domain.Contracts.Wardrobe;
using Domain.Dto;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Pgvector;

namespace Application.Services.Items
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepo;
        private readonly IItemVariantRepository _itemVariantRepository;
        private readonly IAiService _aiService;
        private readonly IGeminiService _geminiService;
        private readonly IWardrobeRepository _wardrobeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IImageRepository _imageRepository;
        private readonly IRecommendationHistoryRepository _recommendationHistoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IItemSaveRepository _itemSaveRepo;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IUserProfileService _userProfileService;

        private readonly decimal _smartRecommendPrice = 2000m;

        public ItemService(
            IItemRepository itemRepo,
            IItemVariantRepository itemVariantRepository,
            IAiService aiService,
            IWardrobeRepository wardrobeRepository,
            ICurrentUserService currentUserService,
            ICloudStorageService cloudStorageService,
            IGeminiService geminiService,
            IImageRepository imageRepository,
            IRecommendationHistoryRepository recommendationHistoryRepository,
            IUnitOfWork unitOfWork,
            IItemSaveRepository itemSaveRepo,
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository,
            IUserProfileService userProfileService)
        {
            _itemRepo = itemRepo;
            _itemVariantRepository = itemVariantRepository;
            _aiService = aiService;
            _wardrobeRepository = wardrobeRepository;
            _currentUserService = currentUserService;
            _cloudStorageService = cloudStorageService;
            _geminiService = geminiService;
            _imageRepository = imageRepository;
            _recommendationHistoryRepository = recommendationHistoryRepository;
            _unitOfWork = unitOfWork;
            _itemSaveRepo = itemSaveRepo;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _userProfileService = userProfileService;
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
                throw new ArgumentException("PrimaryImageUrl is required.");

            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                throw new InvalidOperationException("User does not have a wardrobe.");

            Vector vectorObject = await _aiService.GetEmbeddingFromPhotoAsync(dto, dto.PrimaryImageUrl);

            var newItem = dto.Adapt<Item>();
            newItem.WardrobeId = wardrobe.WardrobeId;
            newItem.ItemEmbedding = vectorObject;
            newItem.Status = ItemStatus.Active;

            newItem.IsForSale = false;
            newItem.ListedPrice = null;
            newItem.Condition = null;
            newItem.PublishedAt = null;

            newItem.CreatedAt = DateTime.UtcNow;
            newItem.UpdateAt = DateTime.UtcNow;

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
            if (request == null)
                return new List<ItemResponseDto>();

            if (string.IsNullOrWhiteSpace(request.Prompt) &&
                (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return new List<ItemResponseDto>();
            }

            int currentAccountId = _currentUserService.GetRequiredUserId();

            var wallet = await _walletRepository.GetByAccountIdAsync(currentAccountId)
                ?? throw new KeyNotFoundException("Wallet not found.");

            var availableBalance = wallet.Balance - wallet.LockedBalance;
            if (availableBalance < _smartRecommendPrice)
                throw new InvalidOperationException("Insufficient wallet balance for smart recommendation.");

            string userContext = await BuildUserContextAsync(request);

            string taskInstruction = $@"
### ROLE: Expert Fashion Stylist

### USER CONTEXT:
{userContext}

### USER INPUT:
'{request.Prompt}'";

            var scopeRequestForRepo = request.Adapt<SmartRecommendationDto>();

            if (request.ReferenceItemId.HasValue && request.ReferenceItemId.Value > 0)
            {
                var referenceItem = await _itemRepo.GetByIdAsync(request.ReferenceItemId.Value);

                if (referenceItem != null)
                {
                    taskInstruction += $@"

### REFERENCE ITEM:
The user already owns or wears this item:
- Category: {referenceItem.Category}
- Color: {referenceItem.MainColor}
- Style: {referenceItem.Style}
- Material: {referenceItem.Material}

### TASK:
1. Recommend one complementary fashion item that creates a good outfit with the reference item.
2. Consider the user context when choosing the style, color, material, and category.
3. Do not output metadata for the reference item.
4. Output metadata only for the new recommended item.";

                    scopeRequestForRepo.ReferenceCategory = referenceItem.Category;
                }
            }
            else
            {
                taskInstruction += @"

### TASK:
Convert the user request into structured fashion search metadata.";
            }

            taskInstruction += @"

### CONSTRAINT:
- Do NOT include the reference item in the output.
- Be specific about material, style, category, and color.
- Output only the structured metadata string for searching.";

            var intent = await _geminiService.AnalyzePromptAsync(taskInstruction);
            Vector queryVector = await _aiService.GetTextEmbeddingAsync(intent.CleanPrompt);

            scopeRequestForRepo.IncludeSavedItems = request.IncludeSavedItems;

            var candidates = await _itemRepo.GetHybridRecommendationsAsync(
                queryVector,
                intent,
                currentAccountId,
                scopeRequestForRepo);

            if (!candidates.Any())
                return new List<ItemResponseDto>();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                wallet = await _walletRepository.GetByAccountIdAsync(currentAccountId)
                    ?? throw new KeyNotFoundException("Wallet not found.");

                availableBalance = wallet.Balance - wallet.LockedBalance;
                if (availableBalance < _smartRecommendPrice)
                    throw new InvalidOperationException("Insufficient balance.");

                decimal balanceBefore = wallet.Balance;

                wallet.Balance -= _smartRecommendPrice;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepository.Update(wallet);

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = null,
                    TransactionCode = $"RECOM-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Amount = _smartRecommendPrice,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Debit,
                    ReferenceType = TransactionReferenceType.AIRecommendation,
                    ReferenceId = null,
                    Description = "Smart outfit recommendation payment",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                };

                await _transactionRepository.AddAsync(transaction);

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

                return candidates.Adapt<List<ItemResponseDto>>();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<ItemResponseDto>> GetMyItemsAsync(int accountId)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return new List<ItemResponseDto>();

            var items = await _itemRepo.GetByWardrobeIdAsync(wardrobe.WardrobeId);
            return items.Adapt<List<ItemResponseDto>>();
        }

        public async Task<(IEnumerable<ItemResponseDto> Items, int TotalCount)> GetMyItemsAsync(
            int accountId,
            int page,
            int pageSize,
            string? search)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return (new List<ItemResponseDto>(), 0);

            var result = await _itemRepo.GetByWardrobeIdAsync2(
                wardrobe.WardrobeId,
                page,
                pageSize,
                search);

            var itemDtos = result.Items.Adapt<List<ItemResponseDto>>();

            return (itemDtos, result.TotalCount);
        }

        public async Task<IEnumerable<ItemResponseDto>> GetAllMyItemsAsync(int accountId)
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
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to update this item.");

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
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to delete this item.");

            if (item.ItemVariants.Any(v => v.ReservedQuantity > 0))
                throw new InvalidOperationException("Cannot delete item while some variants are reserved.");

            if (item.Images != null && item.Images.Any())
            {
                foreach (var img in item.Images)
                {
                    try
                    {
                        await _cloudStorageService.DeleteImageAsync(img.ImageUrl);
                    }
                    catch
                    {
                    }
                }
            }

            _itemRepo.Delete(item);
            await _itemRepo.SaveChangesAsync();
        }

        public async Task<ItemCommerceResponseDto> PublishItemForSaleAsync(
            int itemId,
            PublishItemForSaleRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdForUpdateAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to publish this item.");

            if (item.Status != ItemStatus.Active)
                throw new InvalidOperationException("Only active items can be published for sale.");

            if (request.ListedPrice <= 0)
                throw new InvalidOperationException("Listed price must be greater than 0.");

            var requestSkus = request.Variants
                .Select(v => v.Sku?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (requestSkus.Count != requestSkus.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                throw new InvalidOperationException("Duplicate SKU is not allowed.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                item.IsForSale = true;
                item.IsPublic = true;
                item.ListedPrice = request.ListedPrice;
                item.Condition = request.Condition;
                item.PublishedAt = DateTime.UtcNow;
                item.UpdateAt = DateTime.UtcNow;
                _itemRepo.Update(item);

                var variants = new List<ItemVariant>();

                foreach (var v in request.Variants)
                {
                    if (string.IsNullOrWhiteSpace(v.Sku))
                        throw new InvalidOperationException("SKU is required.");

                    if (v.Price <= 0)
                        throw new InvalidOperationException("Variant price must be greater than 0.");

                    if (v.StockQuantity < 0)
                        throw new InvalidOperationException("Stock quantity cannot be negative.");

                    bool skuExists = await _itemVariantRepository.ExistsSkuAsync(item.ItemId, v.Sku.Trim());
                    if (skuExists)
                        throw new InvalidOperationException($"SKU '{v.Sku}' already exists for this item.");

                    variants.Add(new ItemVariant
                    {
                        ItemId = item.ItemId,
                        Sku = v.Sku.Trim(),
                        SizeCode = string.IsNullOrWhiteSpace(v.SizeCode) ? null : v.SizeCode.Trim(),
                        Color = string.IsNullOrWhiteSpace(v.Color) ? item.MainColor : v.Color.Trim(),
                        Price = v.Price,
                        StockQuantity = v.StockQuantity,
                        ReservedQuantity = 0,
                        Status = v.StockQuantity > 0
                            ? ItemVariantStatus.Active
                            : ItemVariantStatus.OutOfStock
                    });
                }

                await _itemVariantRepository.AddRangeAsync(variants);
                await _unitOfWork.CommitAsync();

                return await BuildCommerceResponseAsync(item.ItemId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<ItemCommerceResponseDto> UnpublishItemAsync(int itemId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdForUpdateAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to unpublish this item.");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                item.IsForSale = false;
                item.PublishedAt = null;
                item.UpdateAt = DateTime.UtcNow;
                _itemRepo.Update(item);

                var variants = await _itemVariantRepository.GetByItemIdAsync(itemId);

                foreach (var variant in variants)
                {
                    var variantForUpdate =
                        await _itemVariantRepository.GetByIdForUpdateAsync(variant.ItemVariantId);

                    if (variantForUpdate == null)
                        continue;

                    if (variantForUpdate.ReservedQuantity > 0)
                        throw new InvalidOperationException("Cannot unpublish item while some variants are reserved.");

                    variantForUpdate.Status = ItemVariantStatus.Inactive;
                    _itemVariantRepository.Update(variantForUpdate);
                }

                await _unitOfWork.CommitAsync();

                return await BuildCommerceResponseAsync(item.ItemId);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ItemVariantResponseDto>> GetItemVariantsAsync(int itemId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to view this item's variants.");

            var variants = await _itemVariantRepository.GetByItemIdAsync(itemId);
            return variants.Select(MapVariantToResponse).ToList();
        }

        public async Task<ItemVariantResponseDto> CreateItemVariantAsync(
            int itemId,
            CreateItemVariantRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdForUpdateAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe == null || item.Wardrobe.AccountId != currentUserId)
                throw new UnauthorizedAccessException("You do not have permission to add variant for this item.");

            if (!item.IsForSale)
                throw new InvalidOperationException("Item must be published for sale before adding variants.");

            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new InvalidOperationException("SKU is required.");

            if (request.Price <= 0)
                throw new InvalidOperationException("Price must be greater than 0.");

            if (request.StockQuantity < 0)
                throw new InvalidOperationException("Stock quantity cannot be negative.");

            bool skuExists = await _itemVariantRepository.ExistsSkuAsync(itemId, request.Sku.Trim());
            if (skuExists)
                throw new InvalidOperationException("SKU already exists for this item.");

            var variant = new ItemVariant
            {
                ItemId = itemId,
                Sku = request.Sku.Trim(),
                SizeCode = string.IsNullOrWhiteSpace(request.SizeCode) ? null : request.SizeCode.Trim(),
                Color = string.IsNullOrWhiteSpace(request.Color) ? item.MainColor : request.Color.Trim(),
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                ReservedQuantity = 0,
                Status = request.StockQuantity > 0
                    ? ItemVariantStatus.Active
                    : ItemVariantStatus.OutOfStock
            };

            await _itemVariantRepository.AddAsync(variant);
            await _itemVariantRepository.SaveChangesAsync();

            return MapVariantToResponse(variant);
        }

        public async Task<ItemVariantResponseDto> UpdateItemVariantAsync(
            int itemVariantId,
            UpdateItemVariantRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            int currentUserId = _currentUserService.GetRequiredUserId();

            var variant = await _itemVariantRepository.GetByIdForUpdateAsync(itemVariantId);
            if (variant == null)
                throw new KeyNotFoundException("Variant not found.");

            if (variant.Item == null ||
                variant.Item.Wardrobe == null ||
                variant.Item.Wardrobe.AccountId != currentUserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to update this variant.");
            }

            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new InvalidOperationException("SKU is required.");

            if (request.Price <= 0)
                throw new InvalidOperationException("Price must be greater than 0.");

            if (request.StockQuantity < 0)
                throw new InvalidOperationException("Stock quantity cannot be negative.");

            if (request.StockQuantity < variant.ReservedQuantity)
                throw new InvalidOperationException("Stock quantity cannot be less than reserved quantity.");

            bool skuExists = await _itemVariantRepository.ExistsOtherSkuAsync(
                variant.ItemVariantId,
                variant.ItemId,
                request.Sku.Trim());

            if (skuExists)
                throw new InvalidOperationException("SKU already exists for this item.");

            variant.Sku = request.Sku.Trim();
            variant.SizeCode = string.IsNullOrWhiteSpace(request.SizeCode) ? null : request.SizeCode.Trim();
            variant.Color = string.IsNullOrWhiteSpace(request.Color) ? variant.Item.MainColor : request.Color.Trim();
            variant.Price = request.Price;
            variant.StockQuantity = request.StockQuantity;
            variant.Status = request.Status;

            if (variant.StockQuantity == 0 && variant.ReservedQuantity == 0)
            {
                variant.Status = ItemVariantStatus.OutOfStock;
            }
            else if (variant.Status == ItemVariantStatus.OutOfStock && variant.StockQuantity > 0)
            {
                variant.Status = ItemVariantStatus.Active;
            }

            _itemVariantRepository.Update(variant);
            await _itemVariantRepository.SaveChangesAsync();

            return MapVariantToResponse(variant);
        }

        public async Task DeleteItemVariantAsync(int itemVariantId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var variant = await _itemVariantRepository.GetByIdForUpdateAsync(itemVariantId);
            if (variant == null)
                throw new KeyNotFoundException("Variant not found.");

            if (variant.Item == null ||
                variant.Item.Wardrobe == null ||
                variant.Item.Wardrobe.AccountId != currentUserId)
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this variant.");
            }

            if (variant.ReservedQuantity > 0)
                throw new InvalidOperationException("Cannot delete a reserved variant.");

            _itemVariantRepository.Delete(variant);
            await _itemVariantRepository.SaveChangesAsync();
        }

        public async Task<List<PublicWardrobeItemDto>> GetMySavedItemsAsync()
        {
            int currentUserId = _currentUserService.GetRequiredUserId();
            return await _itemSaveRepo.GetMySavedItemDtosAsync(currentUserId);
        }

        public async Task SaveItemAsync(int itemId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var item = await _itemRepo.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("Item not found.");

            if (item.Wardrobe != null && item.Wardrobe.AccountId == currentUserId)
                throw new InvalidOperationException("You cannot save your own item.");

            var existingSave = await _itemSaveRepo.GetSaveItem(itemId, currentUserId);
            if (existingSave != null)
                throw new InvalidOperationException("Item already saved.");

            var savedItem = new SavedItem
            {
                AccountId = currentUserId,
                ItemId = itemId,
                SavedAt = DateTime.UtcNow
            };

            await _itemSaveRepo.SaveItem(savedItem);
            await _unitOfWork.CommitAsync();
        }

        public async Task UnsaveItemAsync(int itemId)
        {
            int currentUserId = _currentUserService.GetRequiredUserId();

            var savedItem = await _itemSaveRepo.GetSaveItem(itemId, currentUserId);
            if (savedItem == null)
                throw new KeyNotFoundException("Saved item not found.");

            await _itemSaveRepo.DeleteSaveItem(savedItem);
            await _unitOfWork.CommitAsync();
        }

        private async Task<string> BuildUserContextAsync(SmartRecommendationRequestDto request)
        {
            if (!request.UseMyStylePreferences && !request.UseMyPhysicalProfile)
                return "NONE";

            var profile = await _userProfileService.GetUserProfileAsync();
            if (profile == null)
                return "NONE";

            var age = profile.DateOfBirth.HasValue
                ? GetAge(profile.DateOfBirth.Value).ToString()
                : "N/A";

            var favoriteStyles = request.UseMyStylePreferences
                ? FormatList(profile.FavoriteStyles)
                : "Not specified";

            var favoriteColors = request.UseMyStylePreferences
                ? FormatList(profile.FavoriteColors)
                : "Not specified";

            var physique = request.UseMyPhysicalProfile
                ? $"Shape {profile.BodyShape}, SkinTone {profile.SkinTone}"
                : "Not specified";

            return $@"
            - Demographic: Gender {profile.Gender}, Age {age}
            - Style: {favoriteStyles}
            - Colors: {favoriteColors}
            - Physique: {physique}";
        }

        private static int GetAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
                age--;

            return age;
        }

        private static string FormatList(IEnumerable<string>? values)
        {
            if (values == null)
                return "Not specified";

            var list = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            return list.Any()
                ? string.Join(", ", list)
                : "Not specified";
        }

        private async Task<ItemCommerceResponseDto> BuildCommerceResponseAsync(int itemId)
        {
            var item = await _itemRepo.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException("Item not found.");

            var variants = await _itemVariantRepository.GetByItemIdAsync(itemId);

            return new ItemCommerceResponseDto
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                IsForSale = item.IsForSale,
                ListedPrice = item.ListedPrice,
                Condition = item.Condition,
                PublishedAt = item.PublishedAt,
                Variants = variants.Select(MapVariantToResponse).ToList()
            };
        }

        private static ItemVariantResponseDto MapVariantToResponse(ItemVariant variant)
        {
            return new ItemVariantResponseDto
            {
                ItemVariantId = variant.ItemVariantId,
                ItemId = variant.ItemId,
                Sku = variant.Sku,
                SizeCode = variant.SizeCode,
                Color = variant.Color,
                Price = variant.Price,
                StockQuantity = variant.StockQuantity,
                ReservedQuantity = variant.ReservedQuantity,
                Status = variant.Status,
                CreatedAt = variant.CreatedAt,
                UpdatedAt = variant.UpdatedAt
            };
        }
    }
}