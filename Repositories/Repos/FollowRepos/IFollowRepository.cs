using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Entities;

namespace Repositories.Repos.FollowRepos
{
    public interface IFollowRepository
    {
        Task<int> FollowUserAsync(Follow follow);
        Task<int> UnfollowUserAsync(int userId, int followerId);
        Task<List<Follow>> GetFollowersByIdAsync(int userId);
        Task<Follow> GetFollowerByIdAsync(int userId, int followerId);
    }
}
