using Domain.Entities;

namespace Domain.Interfaces

{
    public interface ISocialRepository
    {
        // ===== LIKE =====
        Task<bool> IsLikedAsync(int userId, int postId);
        Task<Reaction?> GetReactionAsync(int userId, int postId);
        Task AddReactionAsync(Reaction reaction);
        Task RemoveReactionAsync(Reaction reaction);
        Task<int> CountReactionAsync(int postId);

        // ===== COMMENT =====
        Task AddCommentAsync(Comment comment);
        Task UpdateCommentAsync(Comment comment);
        Task DeleteCommentAsync(Comment comment);
        Task<Comment?> GetCommentByIdAsync(int id);
        Task<List<Comment>> GetCommentsByPostIdAsync(int postId);
        Task<int> CountCommentAsync(int postId);

        Task SaveChangesAsync();
    }
}