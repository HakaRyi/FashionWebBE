using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IFollowRepository
    {
        Task<int> FollowUserAsync(Follow follow);
        Task<int> UnfollowUserAsync(int userId, int followerId);
        Task<List<Follow>> GetFollowersByIdAsync(int userId);
        Task<List<Follow>> GetFollowingsByIdAsync(int userId);
        Task<Follow> GetFollowerByIdAsync(int userId, int followerId);
        Task<HashSet<int>> GetFollowingIdsAsync(int followerId, List<int> targetUserIds);
        Task<bool> IsFollowingAsync(int followerId, int targetUserId);
    }
}
