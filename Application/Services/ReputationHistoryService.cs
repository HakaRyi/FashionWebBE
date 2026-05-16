using Application.Interfaces;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Response.ReputationHistoryResp.ReputationHistoryResponse;

namespace Application.Services
{
    public class ReputationHistoryService : IReputationHistoryService
    {
        private readonly IReputationHistoryRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public ReputationHistoryService(IReputationHistoryRepository repository, ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        public async Task<ExpertReputationSummaryDto> GetReputationDashboardAsync()
        {
            int currentExpertId = _currentUserService.GetRequiredUserId();

            // 1. Tìm profile của expert dựa trên accountId
            var expertProfile = await _repository.GetFullReputationDataByAccountIdAsync(currentExpertId);

            if (expertProfile == null)
                throw new Exception("Expert profile does not exist.");

            var summary = new ExpertReputationSummaryDto
            {
                CurrentReputationScore = expertProfile.ReputationScore ?? 0,
                AverageRating = expertProfile.RatingAvg,
                History = expertProfile.ReputationHistories
                    .OrderByDescending(h => h.CreatedAt)
                    .Select(h => new ReputationHistoryDto
                    {
                        PointChange = h.PointChange,
                        PointAfterChange = h.CurrentPoint,
                        Reason = h.Reason,
                        CreatedAt = h.CreatedAt
                    }).ToList()
            };

            return summary;
        }
    }
}
