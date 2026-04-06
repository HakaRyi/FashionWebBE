namespace Application.Request.ExpertRatingReq
{
    public class ExpertRatingRequest
    {
        public int PostId { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }

        public List<CriterionRatingDto> CriterionRatings { get; set; } = new List<CriterionRatingDto>();
    }

    public class CriterionRatingDto
    {
        public int EventCriterionId { get; set; }

        public double Score { get; set; }
    }
}
