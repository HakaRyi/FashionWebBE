using Application.Interfaces;
using Application.Response.FollowResp;
using Domain.Interfaces;

namespace Application.Services.Follow
{
    public class FollowService : IFollowService
    {
        private readonly IFollowRepository _followRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        public FollowService(IFollowRepository followRepository
            , IAccountRepository accountRepository
            , IUnitOfWork unitOfWork
            )
        {
            _followRepository = followRepository;
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
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

        public async Task<bool> FollowUserAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId) return false;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var follow = new Domain.Entities.Follow
                {
                    UserId = targetUserId,
                    FollowerId = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                var currentUserAccount = await _accountRepository.GetAccountById(currentUserId);
                var targetUserAccount = await _accountRepository.GetAccountById(targetUserId);

                if (currentUserAccount == null || targetUserAccount == null) return false;

                currentUserAccount.CountFollowing += 1;
                targetUserAccount.CountFollower += 1;

                await _accountRepository.UpdateAccount(currentUserAccount);
                await _accountRepository.UpdateAccount(targetUserAccount);

                await _followRepository.FollowUserAsync(follow);

                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return false;
            }
        }

        public async Task<FollowResponse> GetFollowerByIdAsync(int userId, int followerId)
        {
            try
            {
                var f = await _followRepository.GetFollowerByIdAsync(userId, followerId);
                var response = new FollowResponse
                {
                    FollowingId = f.UserId,
                    FollowerId = f.FollowerId,
                    FollowerName = f.Follower.UserName,
                    FollowerAvatar = f.Follower.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    FollowingAvatar = f.User.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    CreatedAt = f.CreatedAt,
                    FollowingName = f.User.UserName

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
                    FollowingId = f.UserId,
                    FollowerId = f.FollowerId,
                    FollowerName = f.Follower.UserName,
                    FollowerAvatar = f.Follower.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    FollowingAvatar = f.User.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    CreatedAt = f.CreatedAt,
                    FollowingName = f.User.UserName
                }).ToList();
                return responses;
            }
            catch
            {

                return new List<FollowResponse>();
            }
        }
        public async Task<List<FollowResponse>> GetFollowingsByIdAsync(int userId)
        {
            try
            {
                var follows = await _followRepository.GetFollowingsByIdAsync(userId);
                var responses = follows.Select(f => new FollowResponse
                {
                    FollowingId = f.UserId,
                    FollowerId = f.FollowerId,
                    FollowerName = f.Follower.UserName,
                    FollowerAvatar = f.Follower.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    FollowingAvatar = f.User.Avatars
                          .OrderByDescending(img => img.CreatedAt)
                          .Select(img => img.ImageUrl)
                          .FirstOrDefault() ?? null,
                    CreatedAt = f.CreatedAt,
                    FollowingName = f.User.UserName
                }).ToList();
                return responses;
            }
            catch
            {

                return new List<FollowResponse>();
            }
        }

        public async Task<bool> IsFollowingAsync(int followerId, int targetUserId)
        {
            var isFollowing = await _followRepository.IsFollowingAsync(followerId, targetUserId);
            return isFollowing;
        }

        public async Task<bool> UnfollowUserAsync(int currentUserId, int targetUserId)
        {
            if (currentUserId == targetUserId) return false;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var currentUserAccount = await _accountRepository.GetAccountById(currentUserId);
                var targetUserAccount = await _accountRepository.GetAccountById(targetUserId);

                if (currentUserAccount == null || targetUserAccount == null) return false;

                currentUserAccount.CountFollowing = Math.Max(0, currentUserAccount.CountFollowing - 1);
                targetUserAccount.CountFollower = Math.Max(0, targetUserAccount.CountFollower - 1);

                await _accountRepository.UpdateAccount(currentUserAccount);
                await _accountRepository.UpdateAccount(targetUserAccount);
                await _followRepository.UnfollowUserAsync(targetUserId, currentUserId);

                await _unitOfWork.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                return false;
            }
        }
    }
}
