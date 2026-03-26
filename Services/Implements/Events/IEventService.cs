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
        Task<Event> CreateEventAsync(CreateEventRequest dto);
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
        Task FinalizeAndAwardEventAsync(int eventId);

        // Expert
        Task<IEnumerable<EventListDto>> GetMyCreatedEventsAsync();
        Task<IEnumerable<EventListDto>> GetAllEventsForExpertAsync();
        Task ActivateEventWithEscrowAsync(int eventId);
        Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string period);
        Task ManualStartEventAsync(int eventId);

        // User / Public
        Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync();

        //admin
        Task<IEnumerable<EventAdminListDto>> GetAllEventsForAdminAsync();
        Task RejectEventAsync(int eventId, string reason);
        Task ApproveEventAsync(int eventId);

        // Common
        Task<EventDetailDto?> GetEventDetailsAsync(int eventId);
    }
}
