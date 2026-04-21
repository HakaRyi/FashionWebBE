using Application.Interfaces;
using Application.Response.AiResp;
using Application.Response.TryOn;
using Application.Utils;
using Domain.Constants;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Application.Services.TryOn
{
    public class TryOnService : ITryOnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ootdUrl;
        private readonly string _aiPredictUrl;
        private readonly decimal _tryOnPrice;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IWalletRepository _walletRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ITryOnHistoryRepository _tryOnHistoryRepository;
        private readonly ICloudStorageService _cloudStorageService;

        public TryOnService(
            HttpClient httpClient,
            IConfiguration config,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IWalletRepository walletRepository,
            ITransactionRepository transactionRepository,
            ITryOnHistoryRepository tryOnHistoryRepository,
            ICloudStorageService cloudStorageService)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(15);

            _ootdUrl = config["AISettings:OOTDUrl"]
                ?? throw new Exception("Thiếu cấu hình AISettings:OOTDUrl");

            _aiPredictUrl = config["AISettings:Fashin_PredictionUrl"]
                ?? throw new Exception("Thiếu cấu hình AISettings:Fashin_PredictionUrl");

            _tryOnPrice = decimal.TryParse(config["TryOnSettings:Price"], out var price)
                ? price
                : 5000m;

            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _tryOnHistoryRepository = tryOnHistoryRepository;
            _cloudStorageService = cloudStorageService;
        }

        public async Task<Stream> ProcessTryOnAsync(
            IFormFile modelImage,
            IFormFile clothImage,
            int? category)
        {
            if (modelImage == null || modelImage.Length == 0)
                throw new ArgumentException("Ảnh người mẫu không hợp lệ.");

            if (clothImage == null || clothImage.Length == 0)
                throw new ArgumentException("Ảnh quần áo không hợp lệ.");

            var userId = _currentUserService.GetRequiredUserId();

            var wallet = await _walletRepository.GetByAccountIdAsync(userId)
                ?? throw new KeyNotFoundException("Không tìm thấy ví.");

            var availableBalance = wallet.Balance - wallet.LockedBalance;
            if (availableBalance < _tryOnPrice)
                throw new InvalidOperationException("Số dư không đủ.");

            await CheckSpendingLimitAsync(wallet, _tryOnPrice);

            int finalCategory = category ?? await GetClothCategoryAsync(clothImage);

            var resultBytes = await CallTryOnAI(modelImage, clothImage, finalCategory);

            var fileName = $"tryon_{userId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
            string imageUrl;

            using (var uploadStream = new MemoryStream(resultBytes))
            {
                imageUrl = await _cloudStorageService.UploadImageFromStreamAsync(
                    uploadStream,
                    fileName);
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                wallet = await _walletRepository.GetByAccountIdAsync(userId)
                    ?? throw new KeyNotFoundException("Không tìm thấy ví.");

                availableBalance = wallet.Balance - wallet.LockedBalance;
                if (availableBalance < _tryOnPrice)
                    throw new InvalidOperationException("Số dư không đủ.");

                await CheckSpendingLimitAsync(wallet, _tryOnPrice);

                decimal balanceBefore = wallet.Balance;

                wallet.Balance -= _tryOnPrice;
                wallet.UpdatedAt = DateTime.UtcNow;
                _walletRepository.Update(wallet);

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    PaymentId = null,
                    TransactionCode = GenerateTransactionCode(),
                    Amount = _tryOnPrice,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance,
                    Type = TransactionType.Debit,
                    ReferenceType = TransactionReferenceType.TryOn,
                    ReferenceId = null,
                    Description = "Thanh toán thử đồ AI",
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Success
                };

                await _transactionRepository.AddAsync(transaction);

                var history = new TryOnHistory
                {
                    AccountId = userId,
                    ImageUrl = imageUrl,
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow
                };

                await _tryOnHistoryRepository.AddAsync(history);

                await _unitOfWork.CommitAsync();

                return new MemoryStream(resultBytes);
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<TryOnInfoResponse> GetTryOnInfoAsync()
        {
            var userId = _currentUserService.GetRequiredUserId();

            var wallet = await _walletRepository.GetByAccountIdAsync(userId)
                ?? throw new KeyNotFoundException("Không tìm thấy ví của người dùng.");

            var availableBalance = wallet.Balance - wallet.LockedBalance;

            return new TryOnInfoResponse
            {
                TryOnPrice = _tryOnPrice,
                Balance = wallet.Balance,
                LockedBalance = wallet.LockedBalance,
                AvailableBalance = availableBalance,
                CanTryOn = availableBalance >= _tryOnPrice,
                Message = availableBalance >= _tryOnPrice
                    ? "Đủ số dư để thử đồ."
                    : "Số dư không đủ để thử đồ."
            };
        }

        private async Task CheckSpendingLimitAsync(Wallet wallet, decimal debitAmount)
        {
            if (wallet == null)
                throw new KeyNotFoundException("Ví không tồn tại.");

            if (debitAmount <= 0)
                throw new ArgumentException("Số tiền chi không hợp lệ.");

            if (!wallet.MonthlySpendingLimit.HasValue || wallet.MonthlySpendingLimit.Value <= 0)
                return;

            var now = DateTime.UtcNow;

            decimal spentThisMonth = await _transactionRepository.GetMonthlyDebitTotalAsync(
                wallet.WalletId,
                now.Month,
                now.Year);

            decimal projectedSpent = spentThisMonth + debitAmount;
            decimal limitAmount = wallet.MonthlySpendingLimit.Value;

            if (wallet.IsHardSpendingLimit && projectedSpent > limitAmount)
            {
                throw new InvalidOperationException(
                    $"You have exceeded your monthly spending limit. " +
                    $"Spent so far: {spentThisMonth:N0} VND, " +
                    $"try-on cost: {debitAmount:N0} VND, " +
                    $"limit: {limitAmount:N0} VND.");
            }
        }

        private async Task<byte[]> CallTryOnAI(
            IFormFile modelImage,
            IFormFile clothImage,
            int categoryId)
        {
            using var content = new MultipartFormDataContent();

            content.Add(await ToStreamContent(modelImage), "model_image", modelImage.FileName);
            content.Add(await ToStreamContent(clothImage), "cloth_image", clothImage.FileName);
            content.Add(new StringContent(categoryId.ToString()), "category");

            var response = await _httpClient.PostAsync(_ootdUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI try-on error: {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task<StreamContent> ToStreamContent(IFormFile file)
        {
            var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var content = new StreamContent(ms);
            content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType);

            return content;
        }

        private async Task<int> GetClothCategoryAsync(IFormFile clothImage)
        {
            using var content = new MultipartFormDataContent();
            using var ms = new MemoryStream();

            await clothImage.CopyToAsync(ms);
            ms.Position = 0;

            var img = new ByteArrayContent(ms.ToArray());
            img.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(clothImage.ContentType)
                    ? "application/octet-stream"
                    : clothImage.ContentType);

            content.Add(img, "file", clothImage.FileName);

            var response = await _httpClient.PostAsync(_aiPredictUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI predict error: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();

            var prediction = JsonSerializer.Deserialize<AIFashionDetectReponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (prediction == null)
                throw new Exception("Không đọc được kết quả phân loại ảnh.");

            if (!prediction.IsClothing)
                throw new Exception("Ảnh quần áo không hợp lệ.");

            return MapLabelToCategory(prediction.Label ?? string.Empty);
        }

        private int MapLabelToCategory(string label)
        {
            label = label.Trim().ToLowerInvariant();

            if (new[] { "pants", "jeans", "skirt" }.Any(label.Contains))
                return 1;

            if (new[] { "dress", "gown" }.Any(label.Contains))
                return 2;

            return 0;
        }

        private static string GenerateTransactionCode()
        {
            return $"TRYON-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        }
    }
}