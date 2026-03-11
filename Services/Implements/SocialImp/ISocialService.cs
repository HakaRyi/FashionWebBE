using Repositories.Entities;
using Services.Request.CommentReq;
using Services.Response.CommentResp;

namespace Services.Implements.SocialImp
{
    public interface ISocialService
    {
        // ===== LIKE =====
        Task<bool> ToggleLikeAsync(int userId, int postId);
        Task<bool> IsLikedAsync(int userId, int postId);
        Task<int> GetLikeCountAsync(int postId);

        // ===== COMMENT =====
        Task<int> CreateCommentAsync(CommentRequest request, int userId, int postId);
        Task<int> UpdateCommentAsync(int commentId, int userId, CommentRequest request);
        Task<bool> DeleteCommentAsync(int commentId, int userId);
        Task<Comment?> GetCommentByIdAsync(int commentId);
        Task<List<CommentResponse>> GetCommentsByPostIdAsync(int postId);
        Task<int> GetCommentCountAsync(int postId);
    }
}