using Repositories.Entities;

namespace Repositories.Repos.ExpertProfileRepos
{
    public interface IExpertProfileRepository
    {
        Task<ExpertProfile> GetById(int id);
    }
}
