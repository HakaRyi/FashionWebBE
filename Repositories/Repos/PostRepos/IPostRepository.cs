using Repositories.Entities;

namespace Repositories.Repos.PostRepos
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(int postId);
        Task<List<Post>> GetAllPublishedAsync();
        Task<List<Post>> GetAllByUserAsync(int userId);
        Task<List<Post>> GetFeedByCursorAsync(DateTime? cursor, int pageSize);
        Task AddAsync(Post post);
        Task Update(Post post);
        void Delete(Post post);
    }
}