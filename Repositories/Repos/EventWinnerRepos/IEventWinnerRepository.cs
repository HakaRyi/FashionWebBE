using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Repos.EventWinnerRepos
{
    public interface IEventWinnerRepository
    {
        Task AddAsync(EventWinner winner);
        Task AddRangeAsync(IEnumerable<EventWinner> winners);
        Task<IEnumerable<EventWinner>> GetByEventIdAsync(int eventId);
        Task<EventWinner?> GetByIdAsync(int eventWinnerId);
    }
}
