using Services.Request.ExpertRatingReq;
using Services.Response.EventResp;
using Services.Response.PostResp;

namespace Services.Implements.EventExpertSer
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
