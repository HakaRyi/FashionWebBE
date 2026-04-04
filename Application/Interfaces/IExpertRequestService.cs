using Application.Response.AccountRep;

namespace Application.Interfaces
{
    public interface IExpertRequestService
    {
        Task<ExpertFileResponse> GetById(int id);
    }
}
