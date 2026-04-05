using Application.Request.ExpertRatingReq;

namespace Application.Interfaces
{
    public interface IExpertRatingService
    {
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
    }
}
