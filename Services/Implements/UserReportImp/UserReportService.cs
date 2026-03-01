using Repositories.Repos.UserReportRepos;
using Services.Response.UserReportResp;

namespace Services.Implements.UserReportImp
{
    public class UserReportService : IUserReportService
    {
        private readonly IUserReportRepository userReportRepository;
        public UserReportService(IUserReportRepository userReportRepository)
        {
            this.userReportRepository = userReportRepository;
        }

        public async Task<List<UserReportResponse>> GetAll()
        {
            var reports = await userReportRepository.GetUserReports();
            var reportResponses = reports.Select(report => new UserReportResponse
            {
                ReportId = report.UserReportId,
                PostId = report.PostId,
                AccountId = report.AccountId,
                ReportTypeId = report.ReportTypeId,
                ReportTypeName = report.ReportType.TypeName,
                Reason = report.Reason,
                CreatedAt = report.CreatedAt

            }).ToList();
            return reportResponses;

        }

        public async Task<UserReportResponse> GetById(int id)
        {
            var report = await userReportRepository.GetById(id);
            if (report == null)
            {
                return null;
            }
            var response = new UserReportResponse
            {
                ReportId = report.UserReportId,
                PostId = report.PostId,
                AccountId = report.AccountId,
                ReportTypeId = report.ReportTypeId,
                ReportTypeName = report.ReportType.TypeName,
                Reason = report.Reason,
                CreatedAt = report.CreatedAt
            };
            return response;

        }
    }
}
