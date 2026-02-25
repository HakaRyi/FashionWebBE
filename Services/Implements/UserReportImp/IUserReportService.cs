using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Response.UserReportResp;

namespace Services.Implements.UserReportImp
{
    public interface IUserReportService
    {
        Task<UserReportResponse> GetById(int id);
        Task<List<UserReportResponse>> GetAll(); 
    }
}
