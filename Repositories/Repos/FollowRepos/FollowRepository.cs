using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.FollowRepos
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
                .Include(f => f.Follower)
                .Include(f => f.User)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Follow>> GetFollowingsByIdAsync(int userId)
        {
            return await fashionDbContext.Follows
                 .Where(f => f.FollowerId == userId)
                 .Include(f => f.Follower)
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
    }
}
