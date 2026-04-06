using Domain.Entities;
using Application.Request.EventReq;

namespace Application.Interfaces
{
    public interface IEventCreationService
    {
        Task<Event> CreateEventAsync(CreateEventRequest dto);
        Task ActivateEventWithEscrowAsync(int eventId);
        Task ManualStartEventAsync(int eventId);
        Task CancelEventAsync(int eventId);
    }
}
