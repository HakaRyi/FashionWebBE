using Application.Response.UserReportResp;

namespace Application.Services.UserReportImp
{
    public interface IUserReportService
    {
        Task<UserReportResponse> GetById(int id);
        Task<List<UserReportResponse>> GetAll();
    }
}
