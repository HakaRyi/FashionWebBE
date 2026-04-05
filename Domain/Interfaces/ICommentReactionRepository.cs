using Domain.Entities;

namespace Domain.Interfaces

{
    public interface ICommentReactionRepository
    {
        Task<CommentReaction?> GetAsync(int userId, int commentId);

        Task<bool> IsReactedAsync(int userId, int commentId);

        Task AddAsync(CommentReaction reaction);

        void Remove(CommentReaction reaction);

        Task<int> CountAsync(int commentId);

        Task DeleteByCommentIdAsync(int commentId);

        Task<List<int>> GetUserReactedCommentIdsAsync(int userId, List<int> commentIds);
    }
}