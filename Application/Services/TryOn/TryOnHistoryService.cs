using Application.Interfaces;
using Application.Request.TryOn;
using Application.Response.TryOn;
using Application.Utils;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services.TryOn
{
    public class TryOnHistoryService : ITryOnHistoryService
    {
        private readonly ITryOnHistoryRepository _historyRepo;
        private readonly ICloudStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;

        public TryOnHistoryService(
            ITryOnHistoryRepository historyRepo,
            ICloudStorageService storageService,
            IUnitOfWork unitOfWork)
        {
            _historyRepo = historyRepo;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> CreateTryOnHistoryAsync(int accountId, CreateHistoryTryOnRequest request)
        {
            if (request == null || request.Image == null || request.Image.Length == 0)
                throw new ArgumentException("The try-on result image is invalid.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var imageUrl = await _storageService.UploadImageAsync(request.Image);

                var newHistory = new TryOnHistory
                {
                    AccountId = accountId,
                    ImageUrl = imageUrl,
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow
                };

                await _historyRepo.AddAsync(newHistory);
                await _unitOfWork.CommitAsync();

                return newHistory.TryOnId;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
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

        public async Task DeleteTryOnHistoryAsync(int accountId, int tryOnId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var history = await _historyRepo.GetByIdAsync(tryOnId);

                if (history == null)
                    throw new KeyNotFoundException("Try-on history not found.");

                if (history.AccountId != accountId)
                    throw new UnauthorizedAccessException("You are not allowed to delete this try-on history.");

                if (!string.IsNullOrWhiteSpace(history.ImageUrl))
                {
                    await _storageService.DeleteImageAsync(history.ImageUrl);
                }

                _historyRepo.Remove(history);

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}