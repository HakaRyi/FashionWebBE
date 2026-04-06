namespace Application.Response.UserReportResp
{
    public class ReviewUserReportResponseDto
    {
        public int UserReportId { get; set; }
        public int PostId { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? AdminNote { get; set; }

        public string? PostStatus { get; set; }
        public string? PostVisibility { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}