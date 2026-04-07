using Application.Request.ExpertRatingReq;
using Application.Response.EventResp;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IExpertRatingService
    {
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);

        Task<IEnumerable<EventCriterionResponse>> GetEventCriteriaForRating(int eventId);
    }
}
