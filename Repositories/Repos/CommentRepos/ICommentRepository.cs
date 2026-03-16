using Repositories.Entities;

namespace Repositories.Repos.CommentRepos
{
    public interface ICommentRepository
    {
        Task<Comment?> GetByIdAsync(int commentId);

        Task<List<Comment>> GetAllByPostIdAsync(int postId);

        Task<List<Comment>> GetRootCommentsByPostIdAsync(int postId, int skip, int take);

        Task<int> CountRootCommentsByPostIdAsync(int postId);

        Task<List<Comment>> GetRepliesAsync(int parentId, int skip, int take);

        Task<int> CountRepliesAsync(int parentId);

        Task<Dictionary<int, int>> GetReplyCountsByParentIdsAsync(List<int> parentIds);

        Task<List<Comment>> GetCommentWithDirectRepliesAsync(int commentId);

        Task<int> CountByPostIdAsync(int postId);

        Task AddAsync(Comment comment);

        void Update(Comment comment);

        void Delete(Comment comment);

        void DeleteRange(List<Comment> comments);
    }
}