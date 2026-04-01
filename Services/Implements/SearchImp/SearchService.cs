using Microsoft.AspNetCore.Identity;
using Repositories.Entities;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.FollowRepos;
using Repositories.Repos.ImageRepos;
using Repositories.Repos.SearchRepos;
using Services.Request.SearchReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.SearchImp
{
    public class SearchService : ISearchService
    {
        private readonly UserManager<Account> _userManager;
        private readonly ISearchHistoryRepository _searchHistoryRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IFollowRepository _followRepository;

        public SearchService(
            UserManager<Account> userManager,
            ISearchHistoryRepository searchHistoryRepository,
            IImageRepository imageRepository,
            IFollowRepository followRepository)
        {
            _userManager = userManager;
            _searchHistoryRepository = searchHistoryRepository;
            _imageRepository = imageRepository;
            _followRepository = followRepository;
        }

        public async Task<List<UserSuggestionDto>> GetTopInfluencersAsync(string currentUserId)
        {
            var usersQuery = _userManager.Users.ToList();

            var topUsers = usersQuery
                .OrderByDescending(u => u.CountFollower)
                .Take(10)
                .ToList();

            var targetUserIds = topUsers.Select(u => u.Id).ToList();
            var followingIds = await _followRepository.GetFollowingIdsAsync(int.Parse(currentUserId), targetUserIds);

            var suggestions = new List<UserSuggestionDto>();
            foreach (var u in topUsers)
            {
                var avatar = await _imageRepository.GetNewestAvatarAsync(u.Id);
                var isFollowing = followingIds.Contains(u.Id);

                suggestions.Add(new UserSuggestionDto
                {
                    AccountId = u.Id,
                    FullName = u.UserName,
                    Username = u.UserName,
                    AvatarUrl = avatar?.ImageUrl ?? string.Empty,
                    FollowerCount = u.CountFollower,
                    IsFollowing = isFollowing
                });
            }

            return suggestions;
        }

        public async Task<List<SearchHistoryDto>> GetSearchHistoryAsync(int currentUserId)
        {
            var histories = await _searchHistoryRepository.GetByAccountIdAsync(currentUserId);

            return histories
                .Take(10)
                .Select(sh => new SearchHistoryDto
                {
                    Id = sh.Id,
                    Keyword = sh.Keyword
                })
                .ToList();
        }

        public async Task AddSearchHistoryAsync(int currentUserId, string keyword)
        {
            var existing = await _searchHistoryRepository.GetByKeywordAsync(currentUserId, keyword);

            if (existing != null)
            {
                existing.CreatedAt = DateTime.UtcNow;
                await _searchHistoryRepository.UpdateAsync(existing);
            }
            else
            {
                var newHistory = new SearchHistory
                {
                    AccountId = currentUserId,
                    Keyword = keyword,
                    CreatedAt = DateTime.UtcNow
                };
                await _searchHistoryRepository.AddAsync(newHistory);
            }
        }

        public async Task ClearSearchHistoryAsync(int currentUserId)
        {
            await _searchHistoryRepository.DeleteAllByAccountIdAsync(currentUserId);
        }

        public async Task<List<UserSuggestionDto>> SearchUsersAsync(int currentUserId, string keyword)
        {
            var lowerKeyword = keyword.ToLower();
            var usersQuery = _userManager.Users
                .Where(u => u.UserName.ToLower().Contains(lowerKeyword))
                .ToList();

            var searchResults = usersQuery
                .Take(20)
                .ToList();

            var targetUserIds = searchResults.Select(u => u.Id).ToList();
            var followingIds = await _followRepository.GetFollowingIdsAsync(currentUserId, targetUserIds);

            var suggestions = new List<UserSuggestionDto>();
            foreach (var u in searchResults)
            {
                var avatar = await _imageRepository.GetNewestAvatarAsync(u.Id);
                var isFollowing = followingIds.Contains(u.Id);

                suggestions.Add(new UserSuggestionDto
                {
                    AccountId = u.Id,
                    FullName = u.UserName,
                    Username = u.UserName,
                    AvatarUrl = avatar?.ImageUrl ?? string.Empty,
                    FollowerCount = u.CountFollower,
                    IsFollowing = isFollowing
                });
            }

            return suggestions;
        }
    }
}
