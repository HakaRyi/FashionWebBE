using Repositories.Entities;
using Services.Request.EventReq;

namespace Services.Implements.EventCreationImp
{
    public interface IEventCreationService
    {
        Task<Event> CreateEventAsync(CreateEventRequest dto);
        Task ActivateEventWithEscrowAsync(int eventId);
        Task ManualStartEventAsync(int eventId);
    }
}
