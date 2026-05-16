using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class ReactionRepository : IReactionRepository
    {
        private readonly FashionDbContext _db;

        public ReactionRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Reaction?> GetAsync(int userId, int postId)
        {
            return await _db.Reactions
                .FirstOrDefaultAsync(r =>
                    r.AccountId == userId &&
                    r.PostId == postId);
        }

        public async Task<bool> IsLikedAsync(int userId, int postId)
        {
            return await _db.Reactions
                .AnyAsync(r =>
                    r.AccountId == userId &&
                    r.PostId == postId);
        }

        public async Task AddAsync(Reaction reaction)
        {
            await _db.Reactions.AddAsync(reaction);
        }

        public void Remove(Reaction reaction)
        {
            _db.Reactions.Remove(reaction);
        }

        public async Task<int> CountAsync(int postId)
        {
            return await _db.Reactions
                .CountAsync(r => r.PostId == postId);
        }
    }
}