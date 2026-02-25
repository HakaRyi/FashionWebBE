using Repositories.Entities;

namespace Repositories.Repos.PostRepos
{
    public interface IPostRepository
    {
        Task AddPostAsync(Post post);
        Task<Post?> GetPostByIdAsync(int postId);
        Task<List<Post>> GetAllPostAsync();
        Task<List<Post>> GetAllMyPostAsync(int userId);
        Task UpdatePostAsync(Post post);
        Task DeletePostAsync(Post post);
    }
}