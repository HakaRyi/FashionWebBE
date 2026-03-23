namespace Services.Request.ExpertRatingReq
{
    public class ExpertRatingRequest
    {
        public int PostId { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }
    }
}
