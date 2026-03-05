using Repositories.Entities;

namespace Repositories.Repos.ExpertFileRepos
{
    public interface IExpertFileRepository
    {
        Task<IEnumerable<ExpertFile>> GetAllAsync();
        Task<ExpertFile?> GetById(int id);
        Task<ExpertFile?> GetByProfileIdAsync(int profileId);
        Task<IEnumerable<ExpertFile>> GetStatusApplicationsAsync(string status);
        Task AddAsync(ExpertFile file);
        void Update(ExpertFile file);
        Task DeleteAsync(int id);
    }
}
