using Repositories.Entities;

namespace Repositories.Repos.ExpertFileRepos
{
    public interface IExpertFileRepository
    {
        Task<ExpertFile> GetById(int id);
    }
}
