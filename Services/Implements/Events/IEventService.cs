using Services.Response.DashboardResp;
using Services.Response.EventResp;

namespace Services.Implements.Events
{
    public interface IEventService
    {
        // Expert
        Task<IEnumerable<EventListDto>> GetMyCreatedEventsAsync();
        Task<IEnumerable<EventListDto>> GetAllEventsForExpertAsync();
        
        Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string period);

        // User / Public
        Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync();

        //admin
        Task<IEnumerable<EventAdminListDto>> GetAllEventsForAdminAsync();
        Task RejectEventAsync(int eventId, string reason);
        Task ApproveEventAsync(int eventId);

        // Common
        Task<EventDetailDto?> GetEventDetailsAsync(int eventId);

        Task<List<EventLeaderboardDto>> GetEventLeaderboardAsync(int eventId);
        Task<MyEventResultDetailDto?> GetMyResultDetailAsync(int eventId);
    }
}
