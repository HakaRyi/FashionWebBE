using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repositories.Repos.FollowRepos;
using Services.Response.FollowResp;

namespace Services.Implements.Follow
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _followRepository;
        public FollowService(IFollowRepository followRepository)
        {
            _followRepository = followRepository;
        }

        public async Task<bool> FollowUserAsync(int userId, int followerId)
        {
            var follow = new Repositories.Entities.Follow
            {
                UserId = userId,
                FollowerId = followerId,
                CreatedAt = DateTime.UtcNow
            };
            var result = await _followRepository.FollowUserAsync(follow);
            if (result > 0)
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
                    FollowerAvatar = follow.Follower.Avatar,
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
                    FollowerAvatar = f.Follower.Avatar,
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
            if (result > 0)
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
