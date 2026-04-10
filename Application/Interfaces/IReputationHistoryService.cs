using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Application.Response.ReputationHistoryResp.ReputationHistoryResponse;

namespace Application.Interfaces
{
    public interface IReputationHistoryService
    {
        Task<ExpertReputationSummaryDto> GetReputationDashboardAsync();
    }
}
