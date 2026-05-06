using Application.Response.FollowResp;

namespace Application.Services.Follow
{
    public interface IFollowService
    {
        Task<bool> FollowUserAsync(int userId, int followerId);
        Task<bool> UnfollowUserAsync(int userId, int followerId);
        Task<List<FollowResponse>> GetFollowersByIdAsync(int userId);
        Task<List<FollowResponse>> GetFollowingsByIdAsync(int userId);
        Task<int> CountMyFollowers(int userId);
        Task<int> CountMyFollowing(int userId);
        Task<FollowResponse> GetFollowerByIdAsync(int userId, int followerId);
        Task<bool> IsFollowingAsync(int userId, int followerId);
        Task<List<ShareableUserResponse>> GetShareableUsersAsync(int userId);
    }
}
