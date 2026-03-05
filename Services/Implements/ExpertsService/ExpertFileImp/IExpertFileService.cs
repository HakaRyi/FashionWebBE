using Services.Response.AccountRep;

namespace Services.Implements.ExpertsService.ExpertFileImp
{
    public interface IExpertFileService
    {
        Task<ExpertFileResponse> GetById(int id);
    }
}
