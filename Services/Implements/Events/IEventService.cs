using Repositories.Entities;
using Services.Request.EventReq;
using Services.Request.ExpertRatingReq;

namespace Services.Implements.Events
{
    public interface IEventService
    {
        Task<Event?> GetEventDetailsAsync(int eventId);
        Task<IEnumerable<Event>> GetExpertEventsAsync(int expertId);
        Task<Event> CreateEventAndLockFundsAsync(CreateEventRequest dto);
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
        Task FinalizeEventAndDistributePrizesAsync(int eventId);
    }
}
