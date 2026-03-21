using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.EventExpertSer
{
    public interface IEventExpertService
    {
        Task<bool> InviteExpertsAsync(int eventId, List<int> expertIds);
        Task<bool> RespondToInvitationAsync(int eventId, bool accept);
        Task<IEnumerable<Event>> GetMyPendingInvitationsAsync();
    }
}
