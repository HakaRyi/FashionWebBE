using Services.Request.ExpertRatingReq;

namespace Services.Implements.ExpertRatingImp
{
    public interface IExpertRatingService
    {
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
    }
}
