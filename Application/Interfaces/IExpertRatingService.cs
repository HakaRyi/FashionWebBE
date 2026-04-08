using Application.Request.ExpertRatingReq;
using Application.Response.EventResp;
using Domain.Entities;
using static Application.Response.EventResp.EventEndedResponse;

namespace Application.Interfaces
{
    public interface IExpertRatingService
    {
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);

        Task<IEnumerable<EventCriterionResponse>> GetEventCriteriaForRating(int eventId);

        Task<PostRatingDetailResponse> GetPostRatingDetailsAsync(int postId);
    }
}
