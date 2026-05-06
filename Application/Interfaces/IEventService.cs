using Application.Request.EventReq;
using Application.Response.DashboardResp;
using Application.Response.EventResp;
using Application.Response.PostResp;
using Domain.Dto.Social.Post;

namespace Application.Interfaces
{
    public interface IEventService
    {
        // Expert
        Task<IEnumerable<EventListDto>> GetMyCreatedEventsAsync();

        Task<IEnumerable<EventListDto>> GetAllEventsForExpertAsync();

        Task<EventAnalyticsRawResponse> GetAnalyticsAsync(string period);

        // User / Public
        Task<IEnumerable<EventListDto>> GetAllEventsForUserAsync();

        //admin
        Task<IEnumerable<EventAdminListDto>> GetAllEventsForAdminAsync();
        Task RejectEventAsync(int eventId, string reason);
        Task ApproveEventAsync(int eventId);
        Task<bool> UpdateEventByAdmin(int eventId, UpdateEventRequestAdmin dto);

        // Common
        Task<EventDetailDto?> GetEventDetailsAsync(int eventId);

        Task<List<EventLeaderboardDto>> GetEventLeaderboardAsync(int eventId);

        Task<MyEventResultDetailDto?> GetMyResultDetailAsync(int eventId);

        Task<PostResponse> JoinEventByPostAsync(int accountId, CreatePostDto dto);
    }
}
