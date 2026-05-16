using Domain.Entities;

namespace Domain.Interfaces

{
    public interface IExpertRequestRepository
    {
        Task<IEnumerable<ExpertRequest>> GetAllAsync();
        Task<ExpertRequest?> GetById(int id);
        Task<ExpertRequest?> GetByProfileIdAsync(int profileId);
        Task<IEnumerable<ExpertRequest>> GetListByProfileId(int profileId);
        Task<IEnumerable<ExpertRequest>> GetStatusApplicationsAsync(string status);
        Task<bool> AnyPendingRequestAsync(int profileId);
        Task AddAsync(ExpertRequest file);
        void Update(ExpertRequest file);
        Task DeleteAsync(int id);
    }
}
