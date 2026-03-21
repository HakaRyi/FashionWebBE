using Repositories.Entities;
using Services.Request.EventReq;
using Services.Request.ExpertRatingReq;
using Services.Response.DashboardResp;
using Services.Response.EventResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Events
{
    public interface IEventService
    {
        Task<IEnumerable<Event>> GetExpertEventsAsync(int expertId);
        Task<Event> CreateEventAsync(CreateEventRequest dto);
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
        Task FinalizeEventAndDistributePrizesAsync(int eventId);

        // Expert
        Task<IEnumerable<Event>> GetMyCreatedEventsAsync();
        Task<IEnumerable<Event>> GetEventsInvitedToRateAsync();
        Task ActivateEventWithEscrowAsync(int eventId);
        Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string period);

        // User / Public
        Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync();

        // Common
        Task<EventDetailDto?> GetEventDetailsAsync(int eventId);
        Task<IEnumerable<Post>> GetPostsByEventIdAsync(int eventId);
    }
}
