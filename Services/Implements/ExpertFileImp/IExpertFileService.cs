using Services.Response.AccountRep;

namespace Services.Implements.ExpertFileImp
{
    public interface IExpertFileService
    {
        Task<ExpertFileResponse> GetById(int id);
    }
}
