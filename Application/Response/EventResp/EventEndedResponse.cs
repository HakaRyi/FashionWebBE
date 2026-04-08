using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.EventResp
{
    public class EventEndedResponse
    {
        public class PostRatingDetailResponse
        {
            public int PostId { get; set; }
            public string Title { get; set; }

            // Điểm tổng kết
            public double FinalScore { get; set; }
            public double CommunityScore { get; set; }
            public double ExpertTotalScore { get; set; }

            // Danh sách chi tiết từng giám khảo chấm
            public List<ExpertReviewDetail> ExpertReviews { get; set; } = new();
        }

        public class ExpertReviewDetail
        {
            public int ExpertId { get; set; }
            public string ExpertName { get; set; }
            public double TotalScoreGiven { get; set; }
            public string? Reason { get; set; }
            public DateTime RatedAt { get; set; }

            public List<CriterionScoreDetail> CriteriaScores { get; set; } = new();
        }

        public class CriterionScoreDetail
        {
            public string CriterionName { get; set; }
            public double Score { get; set; }
            public double WeightPercentage { get; set; }
        }
    }
}
