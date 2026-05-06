using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly FashionDbContext _db;

        public CommentRepository(FashionDbContext db)
        {
            _db = db;
        }

        public async Task<Comment?> GetByIdAsync(int commentId)
        {
            return await _db.Comments
                .Include(c => c.Post)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Avatars)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }

        public async Task<List<Comment>> GetAllByPostIdAsync(int postId)
        {
            return await _db.Comments
                .AsNoTracking()
                .Include(c => c.Account)
                    .ThenInclude(a => a.Avatars)
                .Where(c => c.PostId == postId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Comment>> GetRootCommentsByPostIdAsync(int postId, int skip, int take)
        {
            if (skip < 0) skip = 0;
            if (take <= 0) take = 10;

            return await _db.Comments
                .AsNoTracking()
                .Include(c => c.Account)
                    .ThenInclude(a => a.Avatars)
                .Where(c => c.PostId == postId && c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountRootCommentsByPostIdAsync(int postId)
        {
            return await _db.Comments
                .AsNoTracking()
                .CountAsync(c => c.PostId == postId && c.ParentCommentId == null);
        }

        public async Task<List<Comment>> GetRepliesAsync(int parentId, int skip, int take)
        {
            if (skip < 0) skip = 0;
            if (take <= 0) take = 10;

            return await _db.Comments
                .AsNoTracking()
                .Include(c => c.Account)
                    .ThenInclude(a => a.Avatars)
                .Where(c => c.ParentCommentId == parentId)
                .OrderBy(c => c.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountRepliesAsync(int parentId)
        {
            return await _db.Comments
                .AsNoTracking()
                .CountAsync(c => c.ParentCommentId == parentId);
        }

        public async Task<Dictionary<int, int>> GetReplyCountsByParentIdsAsync(List<int> parentIds)
        {
            if (parentIds == null || parentIds.Count == 0)
                return new Dictionary<int, int>();

            return await _db.Comments
                .AsNoTracking()
                .Where(c =>
                    c.ParentCommentId.HasValue &&
                    parentIds.Contains(c.ParentCommentId.Value))
                .GroupBy(c => c.ParentCommentId!.Value)
                .Select(g => new
                {
                    ParentCommentId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.ParentCommentId, x => x.Count);
        }

        public async Task<List<Comment>> GetCommentWithDirectRepliesAsync(int commentId)
        {
            return await _db.Comments
                .Include(c => c.Post)
                .Where(c => c.CommentId == commentId || c.ParentCommentId == commentId)
                .ToListAsync();
        }

        public async Task<int> CountByPostIdAsync(int postId)
        {
            return await _db.Comments
                .AsNoTracking()
                .CountAsync(c => c.PostId == postId);
        }

        public async Task AddAsync(Comment comment)
        {
            await _db.Comments.AddAsync(comment);
        }

        public void Update(Comment comment)
        {
            _db.Comments.Update(comment);
        }

        public void Delete(Comment comment)
        {
            _db.Comments.Remove(comment);
        }

        public void DeleteRange(List<Comment> comments)
        {
            if (comments == null || comments.Count == 0)
                return;

            _db.Comments.RemoveRange(comments);
        }
    }
}