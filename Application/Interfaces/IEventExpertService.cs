using Application.Request.ExpertRatingReq;
using Application.Response.EventResp;
using Application.Response.PostResp;

namespace Application.Interfaces
{
    public interface IEventExpertService
    {
        Task<bool> InviteExpertsAsync(int eventId, List<int> expertIds);
        Task<bool> RespondToInvitationAsync(int eventId, bool accept);
        Task<IEnumerable<EventListDto>> GetPendingInvitationsAsync();
        Task<IEnumerable<EventListDto>> GetEventsInvitedToRateAsync();
        Task<IEnumerable<PostReviewDto>> GetPostsForReviewAsync(int eventId);
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
    }
}
