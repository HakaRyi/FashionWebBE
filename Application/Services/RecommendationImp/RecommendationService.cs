using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Response.ItemResp;
using Application.Response.RecommendationResp;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services.RecommendationImp
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IRecommendationHistoryRepository _historyRepo;
        private readonly ICurrentUserService _currentUserService;
        public RecommendationService(IRecommendationHistoryRepository historyRepo,
                                    ICurrentUserService currentUserService) 
        { 
            _historyRepo = historyRepo;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<RecommendationHistoryResponseDto>> GetMyHistoryAsync()
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var histories = await _historyRepo.GetMyRecommendationHistories(accountId);

            return histories.Select(h => new RecommendationHistoryResponseDto
            {
                Id = h.Id,
                Prompt = h.Prompt,
                CreatedAt = h.CreatedAt,
                ReferenceItemId = h.ReferenceItemId,
                ReferenceItemName = h.ReferenceItem?.ItemName,
                ReferenceItemImage = h.ReferenceItem?.Images.FirstOrDefault()?.ImageUrl
            });
        }

        public async Task<List<ItemResponseDto>> GetHistoryDetailsAsync(int historyId)
        {
            var accountId = _currentUserService.GetRequiredUserId();
            var history = await _historyRepo.GetRecommendationHistoryByIdAsync(historyId);

            if (history == null || history.AccountId != accountId)
                return new List<ItemResponseDto>();

            return history.RecommendedItems
                   .Select(x => x.Item) // Lấy ra Entity Item gốc
                   .Adapt<List<ItemResponseDto>>();
        }
    }
}
