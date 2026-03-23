using Repositories.Entities;

namespace Repositories.Repos.Events
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId);
        Task AddAsync(Event @event);
        void Update(Event @event);
        void Delete(Event @event);
        Task<bool> SaveChangesAsync();
    }
}
