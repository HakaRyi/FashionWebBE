using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Response.ItemResp;
using Application.Response.RecommendationResp;

namespace Application.Services.RecommendationImp
{
    public interface IRecommendationService
    {
        Task<IEnumerable<RecommendationHistoryResponseDto>> GetMyHistoryAsync();
        Task<List<ItemResponseDto>> GetHistoryDetailsAsync(int historyId);
    }
}
