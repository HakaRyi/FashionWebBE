using Repositories.Entities;

namespace Repositories.Repos.PostRepos
{
    public interface IPostRepository
    {
        Task<Post?> GetByIdAsync(int postId);
        Task<List<Post>> GetAllPublishedAsync();
        Task<List<Post>> GetAllPostAsync();
        Task<List<Post>> GetAllByUserAsync(int userId);
        Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId);
        Task<List<Post>> GetAllPendingAdminPostAsync();
        Task AddAsync(Post post);
        Task Update(Post post);
        void Delete(Post post);
    }
}