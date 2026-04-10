using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.ReputationHistoryResp
{
    public class ReputationHistoryResponse
    {
        public class ExpertReputationSummaryDto
        {
            public int CurrentReputationScore { get; set; }
            public double? AverageRating { get; set; }
            public List<ReputationHistoryDto> History { get; set; } = new();
        }

        public class ReputationHistoryDto
        {
            public int PointChange { get; set; }
            public int PointAfterChange { get; set; }
            public string Reason { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
