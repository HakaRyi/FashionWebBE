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
        //generall
        Task<Event?> GetByIdAsync(int id);
        Task<IEnumerable<Event>> GetAllAsync(string[]? statuses = null);
        Task AddAsync(Event @event);
        void Update(Event @event);
        void Delete(Event @event);

        //admin

        //user
        Task<IEnumerable<Event>> GetPublicEventsAsync();

        //expert
        Task<IEnumerable<Event>> GetExpertRelatedEventsAsync(int expertId);
        Task<IEnumerable<Event>> GetAnalyticsDataAsync(int creatorId, DateTime startDate);
        Task<IEnumerable<Event>> GetAllByCreatorIdAsync(int creatorId);

    }
}
