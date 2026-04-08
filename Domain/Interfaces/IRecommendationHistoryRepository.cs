using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IRecommendationHistoryRepository
    {
        Task AddAsync(RecommendationHistory history);
        Task<List<RecommendationHistory>> GetMyRecommendationHistories(int accountId);
        Task<RecommendationHistory> GetRecommendationHistoryByIdAsync(int id);
    }
}
