using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Repos.AccountRepos;
using Repositories.Repos.FollowRepos;
using Services.Response.FollowResp;

namespace Services.Implements.Follow
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _followRepository;
        private readonly IAccountRepository _accountRepository;
        public FollowService(IFollowRepository followRepository, IAccountRepository accountRepository)
        {
            _followRepository = followRepository;
            _accountRepository = accountRepository;
        }

        public async Task<int> CountMyFollowers(int userId)
        {
            var follows = await _followRepository.GetFollowersByIdAsync(userId);
            return follows.Count;
        }

        public async Task<int> CountMyFollowing(int userId)
        {
            var follows = await _followRepository.GetFollowersByIdAsync(userId);
            return follows.Count;
        }

        public async Task<bool> FollowUserAsync(int userId, int followerId)
        {
            var follow = new Repositories.Entities.Follow
            {
                UserId = followerId,
                FollowerId = userId,
                CreatedAt = DateTime.UtcNow
            };
            var account = await _accountRepository.GetAccountById(userId);
            var follower = await _accountRepository.GetAccountById(followerId);
            account.CountFollowing += 1;
            follower.CountFollower += 1;
            var updateCountFollowing = await _accountRepository.UpdateAccount(account);
            var updateCountFollower = await _accountRepository.UpdateAccount(follower);
            var result = await _followRepository.FollowUserAsync(follow);
            if (result > 0 && updateCountFollowing > 0 && updateCountFollower > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<FollowResponse> GetFollowerByIdAsync(int userId, int followerId)
        {
            try
            {
                var follow = await _followRepository.GetFollowerByIdAsync(userId, followerId);
                var response = new FollowResponse
                {
                    UserId = follow.UserId,
                    FollowerId = follow.FollowerId,
                    FollowerName = follow.Follower.UserName,
                    //FollowerAvatar = follow.Follower.Avatar,
                    CreatedAt = follow.CreatedAt


                };
                return response;
            }
            catch
            {


            }
            return new FollowResponse();
        }

        public async Task<List<FollowResponse>> GetFollowersByIdAsync(int userId)
        {
            try
            {
                var follows = await _followRepository.GetFollowersByIdAsync(userId);
                var responses = follows.Select(f => new FollowResponse
                {
                    UserId = f.UserId,
                    FollowerId = f.FollowerId,
                    FollowerName = f.Follower.UserName,
                    //FollowerAvatar = f.Follower.Avatar,
                    CreatedAt = f.CreatedAt
                }).ToList();
                return responses;
            }
            catch
            {

                return new List<FollowResponse>();
            }
        }

        public async Task<bool> IsFollowingAsync(int userId, int followerId)
        {
            var follow = await _followRepository.GetFollowerByIdAsync(userId, followerId);
            if (follow != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UnfollowUserAsync(int userId, int followerId)
        {
            var result = await _followRepository.UnfollowUserAsync(userId, followerId);
                var account = await _accountRepository.GetAccountById(userId);
                var follower = await _accountRepository.GetAccountById(followerId);
                account.CountFollowing -= 1;
                follower.CountFollower -= 1;
                var updateCountFollowing = await _accountRepository.UpdateAccount(account);
                var updateCountFollower = await _accountRepository.UpdateAccount(follower);
            if (result > 0 && updateCountFollowing > 0 && updateCountFollower >0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
