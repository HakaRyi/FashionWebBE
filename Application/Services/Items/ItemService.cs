using Application.Interfaces;
using Application.Request.ItemReq;
using Application.Request.ItemRequest;
using Application.Response.ItemResp;
using Application.Utils;
using Application.Utils.File;
using Domain.Constants;
using Domain.Dto;
using Domain.Dto.Wardrobe;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;
using Pgvector;

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
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly decimal _smartRecommendPrice = 2000;
        private readonly IUserProfileService _userProfileService;


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
            IItemSaveRepository itemSaveRepo,
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository,
            IUserProfileService userProfileService)
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
            if (request == null)
                return new List<ItemResponseDto>();

            int currentAccountId = _currentUserService.GetRequiredUserId();

            if (string.IsNullOrWhiteSpace(request.Prompt) &&
                (!request.ReferenceItemId.HasValue || request.ReferenceItemId <= 0))
            {
                return new List<ItemResponseDto>();
            }

            var wallet = await _walletRepository.GetByAccountIdAsync(currentAccountId)
                ?? throw new KeyNotFoundException("Không tìm thấy ví.");

            var availableBalance = wallet.Balance - wallet.LockedBalance;
            if (availableBalance < _smartRecommendPrice)
                throw new InvalidOperationException("Số dư không đủ để sử dụng AI Stylist.");

            string userContext = "NONE";
            if (request.UseMyStylePreferences || request.UseMyPhysicalProfile)
            {
                var profile = await _userProfileService.GetUserProfileAsync();
                if (profile != null)
                {
                    var age = profile.DateOfBirth.HasValue ? (DateTime.Now.Year - profile.DateOfBirth.Value.Year).ToString() : "N/A";
                    userContext = $@"
                    - Demographic: Gender {profile.Gender}, Age {age}
                    - Style: {(request.UseMyStylePreferences ? string.Join(", ", profile.FavoriteStyles) : "Not specified")}
                    - Colors: {(request.UseMyStylePreferences ? string.Join(", ", profile.FavoriteColors) : "Not specified")}
                    - Physique: {(request.UseMyPhysicalProfile ? $"Shape {profile.BodyShape}, SkinTone {profile.SkinTone}" : "Not specified")}";
                }
            }

            string taskInstruction = $@"
                ### ROLE: Expert Fashion Stylist
                ### USER CONTEXT:
                {userContext}

                ### USER INPUT:
                '{request.Prompt}'";

            var scopeRequestForRepo = request.Adapt<SmartRecommendationDto>();

            if (request.ReferenceItemId.HasValue && request.ReferenceItemId > 0)
            {
                var referenceItem = await _itemRepo.GetByIdAsync(request.ReferenceItemId.Value);
                if (referenceItem != null)
                {
                    taskInstruction += $@"
                    ### REFERENCE ITEM (User is already wearing this):
                    - Category: {referenceItem.Category}
                    - Color: {referenceItem.MainColor}
                    - Style: {referenceItem.Style}
    
                    ### TASK: 
                    1. Analyze which item category would perfectly complement the REFERENCE ITEM.
                    2. Consider the USER CONTEXT to ensure it matches their body shape and age.
                    3. Output search metadata for the NEW recommended item only.";

                    scopeRequestForRepo.ReferenceCategory = referenceItem.Category;
                }
            }
            else
            {
                taskInstruction += "\n### TASK: Convert user request into search metadata matching their style/physique.";
            }

            taskInstruction += @"
                ### CONSTRAINT:
                - Do NOT include the Reference Item in the output.
                - Be specific about Material, Style, and Color.
                - Output ONLY the structured metadata string for searching.";

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 3.1 Trừ tiền và tạo lịch sử giao dịch
                wallet = await _walletRepository.GetByAccountIdAsync(currentAccountId)
                    ?? throw new KeyNotFoundException("Không tìm thấy ví.");

                availableBalance = wallet.Balance - wallet.LockedBalance;
                if (availableBalance < _smartRecommendPrice)
                    throw new InvalidOperationException("Số dư không đủ.");

                decimal balanceBefore = wallet.Balance;
                wallet.Balance -= _smartRecommendPrice;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepository.Update(wallet);

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = null,
                    // Sinh mã code random, nếu ông có hàm GenerateTransactionCode() thì xài hàm đó
                    TransactionCode = "SM_" + Guid.NewGuid().ToString("N")[..10].ToUpper(),
                    Amount = _smartRecommendPrice,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Debit,
                    ReferenceType = TransactionReferenceType.TryOn, 
                    ReferenceId = null,
                    Description = "Thanh toán AI Stylist (Smart Match)",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                };
                await _transactionRepository.AddAsync(transaction);

                var intent = await _geminiService.AnalyzePromptAsync(taskInstruction);
                Vector queryVector = await _aiService.GetTextEmbeddingAsync(intent.CleanPrompt);

                scopeRequestForRepo.IncludeSavedItems = request.IncludeSavedItems;
                var candidates = await _itemRepo.GetHybridRecommendationsAsync(
                    queryVector,
                    intent,
                    currentAccountId,
                    scopeRequestForRepo
                );
                if (candidates.Any())
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
                }
                await _unitOfWork.CommitAsync();

                return candidates.Adapt<List<ItemResponseDto>>();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<(IEnumerable<ItemResponseDto> Items, int TotalCount)> GetMyItemsAsync(int accountId, int page, int pageSize, string? search)
        {
            var wardrobe = await _wardrobeRepository.GetByAccountIdAsync(accountId);
            if (wardrobe == null)
                return (new List<ItemResponseDto>(), 0);

            var result = await _itemRepo.GetByWardrobeIdAsync2(wardrobe.WardrobeId, page, pageSize, search);
            var itemDtos = result.Items.Adapt<List<ItemResponseDto>>();

            return (itemDtos, result.TotalCount);
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