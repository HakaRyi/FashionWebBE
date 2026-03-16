using Repositories.Entities;

namespace Repositories.Repos.ReactionRepos
{
    public interface IReactionRepository
    {
        Task<Reaction?> GetAsync(int userId, int postId);

        Task<bool> IsLikedAsync(int userId, int postId);

        Task AddAsync(Reaction reaction);

        void Remove(Reaction reaction);

        Task<int> CountAsync(int postId);
    }
}