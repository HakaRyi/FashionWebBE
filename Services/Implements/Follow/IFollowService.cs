using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Response.FollowResp;

namespace Services.Implements.Follow
{
    public interface IFollowService
    {
        Task<bool> FollowUserAsync(int userId, int followerId);
        Task<bool> UnfollowUserAsync(int userId, int followerId);
        Task<List<FollowResponse>> GetFollowersByIdAsync(int userId);
        Task<int> CountMyFollowers(int userId);
        Task<int> CountMyFollowing(int userId);
        Task<FollowResponse> GetFollowerByIdAsync(int userId, int followerId);
        Task<bool> IsFollowingAsync(int userId, int followerId);
    }
}
