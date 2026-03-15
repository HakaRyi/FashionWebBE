using Services.Response.AccountRep;

namespace Services.Implements.ExpertsService.ExpertRequestImp
{
    public interface IExpertRequestService
    {
        Task<ExpertFileResponse> GetById(int id);
    }
}
