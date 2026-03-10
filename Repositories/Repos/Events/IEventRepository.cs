using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
