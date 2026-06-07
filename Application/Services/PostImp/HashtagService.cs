using Domain.Contracts.Social;
using Domain.Interfaces;

namespace Application.Services.PostImp
{
    public class HashtagService : IHashtagService
    {
        private readonly IHashtagRepository _hashtagRepo;
        private readonly ITrendingTopicRepository _trendingRepo;

        public HashtagService(
            IHashtagRepository hashtagRepo,
            ITrendingTopicRepository trendingRepo)
        {
            _hashtagRepo = hashtagRepo;
            _trendingRepo = trendingRepo;
        }

        public async Task<List<HashtagSuggestionDto>> GetSuggestionsAsync(string? query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var trendingTopics = await _trendingRepo.GetTrendingTopicsAsync(limit, isAdmin: false);

                return trendingTopics.Select(x => new HashtagSuggestionDto
                {
                    HashtagId = x.HashtagId,
                    Name = x.Keyword,
                    IsTrending = true
                }).ToList();
            }

            var searchResults = await _hashtagRepo.SearchTagsAsync(query, limit);

            var trendingTopicsList = await _trendingRepo.GetTrendingTopicsAsync(50, isAdmin: false);
            var trendingIds = trendingTopicsList.Select(t => t.HashtagId).ToHashSet();

            return searchResults.Select(x => new HashtagSuggestionDto
            {
                HashtagId = x.HashtagId,
                Name = x.Name,
                UsageCount = x.UsageCount,
                IsTrending = trendingIds.Contains(x.HashtagId)
            }).ToList();
        }
    }
}