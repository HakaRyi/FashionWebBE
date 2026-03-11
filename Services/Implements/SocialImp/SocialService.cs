using Repositories.Entities;
using Repositories.Repos.SocialRepos;
using Services.Request.CommentReq;
using Services.Response.CommentResp;

namespace Services.Implements.SocialImp
{
    public class SocialService : ISocialService
    {
        private readonly ISocialRepository _repo;

        public SocialService(ISocialRepository repo)
        {
            _repo = repo;
        }

        // ================= LIKE =================

        public async Task<bool> ToggleLikeAsync(int userId, int postId)
        {
            var existing = await _repo.GetReactionAsync(userId, postId);

            if (existing != null)
            {
                await _repo.RemoveReactionAsync(existing);
                await _repo.SaveChangesAsync();
                return false; // unlike
            }

            var reaction = new Reaction
            {
                AccountId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddReactionAsync(reaction);
            await _repo.SaveChangesAsync();
            return true; // liked
        }

        public Task<bool> IsLikedAsync(int userId, int postId)
        {
            return _repo.IsLikedAsync(userId, postId);
        }

        public Task<int> GetLikeCountAsync(int postId)
        {
            return _repo.CountReactionAsync(postId);
        }

        // ================= COMMENT =================

        public async Task<int> CreateCommentAsync(
            CommentRequest request,
            int userId,
            int postId)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new Exception("Comment content cannot be empty.");

            var comment = new Comment
            {
                AccountId = userId,
                PostId = postId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddCommentAsync(comment);
            await _repo.SaveChangesAsync();

            return comment.CommentId;
        }

        public async Task<int> UpdateCommentAsync(
            int commentId,
            int userId,
            CommentRequest request)
        {
            var comment = await _repo.GetCommentByIdAsync(commentId);

            if (comment == null)
                throw new Exception("Comment not found.");

            if (comment.AccountId != userId)
                throw new Exception("You are not allowed to edit this comment.");

            comment.Content = request.Content.Trim();
            comment.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateCommentAsync(comment);
            await _repo.SaveChangesAsync();

            return comment.CommentId;
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _repo.GetCommentByIdAsync(commentId);

            if (comment == null)
                throw new Exception("Comment not found.");

            if (comment.AccountId != userId)
                throw new Exception("You are not allowed to delete this comment.");

            await _repo.DeleteCommentAsync(comment);
            await _repo.SaveChangesAsync();

            return true;
        }

        public Task<Comment?> GetCommentByIdAsync(int commentId)
        {
            return _repo.GetCommentByIdAsync(commentId);
        }

        public async Task<List<CommentResponse>> GetCommentsByPostIdAsync(int postId)
        {
            var comments = await _repo.GetCommentsByPostIdAsync(postId);

            return comments.Select(c => new CommentResponse
            {
                CommentId = c.CommentId,
                PostId = c.PostId,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                AccountId = c.AccountId,
                Username = c.Account.UserName
            }).ToList();
        }

        public Task<int> GetCommentCountAsync(int postId)
        {
            return _repo.CountCommentAsync(postId);
        }
    }
}