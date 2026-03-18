using Microsoft.EntityFrameworkCore;
using Repositories.Data;
using Repositories.Entities;

namespace Repositories.Repos.SocialRepos
{
    public class SocialRepository : ISocialRepository
    {
        private readonly FashionDbContext _db;

        public SocialRepository(FashionDbContext db)
        {
            _db = db;
        }

        // ================= LIKE =================

        public async Task<bool> IsLikedAsync(int userId, int postId)
        {
            return await _db.Reactions
                .AnyAsync(r => r.AccountId == userId && r.PostId == postId);
        }

        public async Task<Reaction?> GetReactionAsync(int userId, int postId)
        {
            return await _db.Reactions
                .FirstOrDefaultAsync(r => r.AccountId == userId && r.PostId == postId);
        }

        public async Task AddReactionAsync(Reaction reaction)
        {
            await _db.Reactions.AddAsync(reaction);
        }

        public Task RemoveReactionAsync(Reaction reaction)
        {
            _db.Reactions.Remove(reaction);
            return Task.CompletedTask;
        }

        public async Task<int> CountReactionAsync(int postId)
        {
            return await _db.Reactions
                .CountAsync(r => r.PostId == postId);
        }

        // ================= COMMENT =================

        public async Task AddCommentAsync(Comment comment)
        {
            await _db.Comments.AddAsync(comment);
        }

        public Task UpdateCommentAsync(Comment comment)
        {
            _db.Comments.Update(comment);
            return Task.CompletedTask;
        }

        public Task DeleteCommentAsync(Comment comment)
        {
            _db.Comments.Remove(comment);
            return Task.CompletedTask;
        }

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            return await _db.Comments
                .Include(c => c.Account) // cần để hiển thị avatar/name
                .FirstOrDefaultAsync(c => c.CommentId == id);
        }

        public async Task<List<Comment>> GetCommentsByPostIdAsync(int postId)
        {
            return await _db.Comments
                .Include(c => c.Account)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> CountCommentAsync(int postId)
        {
            return await _db.Comments
                .CountAsync(c => c.PostId == postId);
        }

        // ================= SAVE =================

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}