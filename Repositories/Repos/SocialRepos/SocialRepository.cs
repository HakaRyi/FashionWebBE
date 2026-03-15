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

        public async Task<bool> CheckIsLikedByUser(int userId, int postId)
        {
            return await _db.Reactions.AnyAsync(r => r.AccountId == userId && r.PostId == postId);
        }

        public Task<bool> CheckIsSharedByUser(int userId, int postId)
        {
            throw new NotImplementedException();
        }

        public async Task<int> Comment(Comment comment)
        {
            _db.Comments.Add(comment);
            return await _db.SaveChangesAsync();
        }

        public async Task<int> CreateReact(Reaction reaction)
        {
            _db.Reactions.Add(reaction);
            return await _db.SaveChangesAsync();
        }

        public async Task<bool> Delete(Comment comment)
        {
            _db.Comments.Remove(comment);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<List<Comment>> GetAllCommentByPostId(int postId)
        {
            return await _db.Comments
                .Include(c => c.Account)
                .Include(c => c.Post)
                .Where(c => c.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Reaction>> GetAllReactionByPostId(int postId)
        {
            return await _db.Reactions
                .Include(r => r.Account)
                .Include(r => r.Post)
                .Where(r => r.PostId == postId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Reaction> GetById(int reactId)
        {
            return await _db.Reactions
                .Include(r => r.Account)
                .Include(r => r.Post)
                .FirstOrDefaultAsync(r => r.ReactionId == reactId);
        }

        public async Task<Comment> GetCommentById(int id)
        {
            return await _db.Comments
                .Include(c => c.Account)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == id);
        }

        public async Task<Reaction> GetReactByAccIdAndPostId(int accId, int postId)
        {
            return await _db.Reactions
                 .Include(r => r.Account)
                 .Include(r => r.Post)
                 .FirstOrDefaultAsync(r => r.AccountId == accId && r.PostId == postId);
        }

        public async Task<bool> RemoveReaction(int reactId)
        {
            var reaction = await _db.Reactions.FindAsync(reactId);
            _db.Reactions.Remove(reaction);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<int> UpdateComment(Comment comment)
        {
            _db.Comments.Update(comment);
            return await _db.SaveChangesAsync();
        }

        public async Task<int> UpdateReact(Reaction reaction)
        {
            _db.Reactions.Update(reaction);
            return await _db.SaveChangesAsync();
        }
    }
}
