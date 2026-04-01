using Repositories.Entities;
using Services.Request.EventReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.EventCreationImp
{
    public interface IEventCreationService
    {
        Task<Event> CreateEventAsync(CreateEventRequest dto);
        Task ActivateEventWithEscrowAsync(int eventId);
        Task ManualStartEventAsync(int eventId);
        Task CancelEventAsync(int eventId);
    }
}
