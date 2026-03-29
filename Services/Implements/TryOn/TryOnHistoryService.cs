using Repositories.Entities;
using Repositories.Repos.TryOn;
using Repositories.UnitOfWork;
using Services.Request.TryOn;
using Services.Response.TryOn;
using Services.Utils;

namespace Services.Implements.TryOn
{
    public class TryOnHistoryService : ITryOnHistoryService
    {
        private readonly ITryOnHistoryRepository _historyRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;

        public TryOnHistoryService(ITryOnHistoryRepository historyRepo, ICloudStorageService storageService, IUnitOfWork unitOfWork)
        {
            _historyRepo = historyRepo;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateTryOnHistoryAsync(int accountId, CreateHistoryTryOnRequest request)
        {
            var imageUrl = await _storageService.UploadImageAsync(request.Image);

            var newHistory = new TryOnHistory
            {
                AccountId = accountId,
                ImageUrl = imageUrl,
                Status = "Success",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _historyRepo.CreateTryOnHistoryAsync(newHistory);

            return result;
        }

        public async Task<List<TryOnHistoryResponse>> GetTryOnHistoryByAccountIdAsync(int accountId)
        {
            var histories = await _historyRepo.GetTryOnHistoryByAccountIdAsync(accountId);

            return histories.Select(h => new TryOnHistoryResponse
            {
                TryOnId = h.TryOnId,
                ImageUrl = h.ImageUrl,
                Status = h.Status,
                CreatedAt = h.CreatedAt
            }).ToList();
        }
    }
}
