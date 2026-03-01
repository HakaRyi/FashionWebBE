using Services.Response.UserReportResp;

namespace Services.Implements.UserReportImp
{
    public interface IUserReportService
    {
        Task<UserReportResponse> GetById(int id);
        Task<List<UserReportResponse>> GetAll();
    }
}
