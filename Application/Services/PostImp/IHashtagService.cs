using Domain.Contracts.Social;

namespace Application.Services.PostImp
{
    public interface IHashtagService
    {
        Task<List<HashtagSuggestionDto>> GetSuggestionsAsync(string? query, int limit = 10);
    }
}