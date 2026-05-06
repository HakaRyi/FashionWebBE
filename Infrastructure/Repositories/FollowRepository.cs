using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class FollowRepository : IFollowRepository
    {
        private readonly FashionDbContext fashionDbContext;

        public FollowRepository(FashionDbContext fashionDbContext)
        {
            this.fashionDbContext = fashionDbContext;
        }

        public async Task<int> FollowUserAsync(Follow follow)
        {
            fashionDbContext.Follows.Add(follow);
            return await fashionDbContext.SaveChangesAsync();
        }

        public async Task<Follow> GetFollowerByIdAsync(int userId, int followerId)
        {
            return await fashionDbContext.Follows
                .Include(f => f.Follower)
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.UserId == userId && f.FollowerId == followerId);
        }

        public async Task<List<Follow>> GetFollowersByIdAsync(int userId)
        {
            return await fashionDbContext.Follows
                .Where(f => f.UserId == userId)
                .Include(f => f.Follower).ThenInclude(f => f.Avatars)
                .Include(f => f.User)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Follow>> GetFollowingsByIdAsync(int userId)
        {
            return await fashionDbContext.Follows
                 .Where(f => f.FollowerId == userId)
                 .Include(f => f.Follower).ThenInclude(f => f.Avatars)
                 .Include(f => f.User)
                 .OrderBy(f => f.CreatedAt)
                 .ToListAsync();
        }

        public async Task<int> UnfollowUserAsync(int userId, int followerId)
        {
            var follow = await GetFollowerByIdAsync(userId, followerId);
            fashionDbContext.Follows.Remove(follow);
            return await fashionDbContext.SaveChangesAsync();
        }

        public async Task<bool> IsFollowingAsync(int followerId, int targetUserId)
        {
            return await fashionDbContext.Follows
                .AnyAsync(f => f.FollowerId == followerId && f.UserId == targetUserId);
        }

        public async Task<HashSet<int>> GetFollowingIdsAsync(int followerId, List<int> targetUserIds)
        {
            var followingIds = await fashionDbContext.Follows
                .Where(f => f.FollowerId == followerId && targetUserIds.Contains(f.UserId))
                .Select(f => f.UserId)
                .ToListAsync();

            return new HashSet<int>(followingIds);
        }

        public async Task<List<int>> GetShareableUserIdsAsync(int accountId)
        {
            var followerIds = await fashionDbContext.Follows
                .Where(f => f.UserId == accountId)
                .Select(f => f.FollowerId)
                .ToListAsync();

            var followingIds = await fashionDbContext.Follows
                .Where(f => f.FollowerId == accountId)
                .Select(f => f.UserId)
                .ToListAsync();

            return followerIds
                .Union(followingIds)
                .Where(x => x != accountId)
                .Distinct()
                .ToList();
        }
    }
}
