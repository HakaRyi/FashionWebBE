using Application.Request.SearchReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.SearchImp
{
    public interface ISearchService
    {
        Task<List<UserSuggestionDto>> GetTopInfluencersAsync(string currentUserId);
        Task<List<SearchHistoryDto>> GetSearchHistoryAsync(int currentUserId);
        Task AddSearchHistoryAsync(int currentUserId, string keyword);
        Task ClearSearchHistoryAsync(int currentUserId);
        Task<List<UserSuggestionDto>> SearchUsersAsync(int currentUserId, string keyword);
    }
}
