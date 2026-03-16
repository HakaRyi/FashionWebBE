using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.CommentReactionRepos
{
    public class CommentReactionRepository : ICommentReactionRepository
    {
        private readonly FashionDbContext _db;

        public CommentReactionRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<CommentReaction?> GetAsync(int userId, int commentId)
        {
            return await _db.CommentReactions
                .FirstOrDefaultAsync(r =>
                    r.AccountId == userId &&
                    r.CommentId == commentId);
        }

        public async Task<bool> IsReactedAsync(int userId, int commentId)
        {
            return await _db.CommentReactions
                .AnyAsync(r =>
                    r.AccountId == userId &&
                    r.CommentId == commentId);
        }

        public async Task AddAsync(CommentReaction reaction)
        {
            await _db.CommentReactions.AddAsync(reaction);
        }

        public void Remove(CommentReaction reaction)
        {
            _db.CommentReactions.Remove(reaction);
        }

        public async Task<int> CountAsync(int commentId)
        {
            return await _db.CommentReactions
                .CountAsync(r => r.CommentId == commentId);
        }

        public async Task DeleteByCommentIdAsync(int commentId)
        {
            var reactions = await _db.CommentReactions
                .Where(r => r.CommentId == commentId)
                .ToListAsync();

            if (reactions.Count > 0)
            {
                _db.CommentReactions.RemoveRange(reactions);
            }
        }

        public async Task<List<int>> GetUserReactedCommentIdsAsync(int userId, List<int> commentIds)
        {
            if (commentIds == null || commentIds.Count == 0)
                return new List<int>();

            return await _db.CommentReactions
                .Where(r => r.AccountId == userId && commentIds.Contains(r.CommentId))
                .Select(r => r.CommentId)
                .ToListAsync();
        }
    }
}