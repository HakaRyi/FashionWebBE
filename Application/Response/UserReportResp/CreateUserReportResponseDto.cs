namespace Application.Response.UserReportResp
{
    public class CreateUserReportResponseDto
    {
        public int UserReportId { get; set; }
        public int PostId { get; set; }
        public int AccountId { get; set; }

        public int ReportTypeId { get; set; }
        public string ReportTypeName { get; set; } = string.Empty;

        public string? Reason { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}