namespace Services.Response.UserReportResp
{
    public class UserReportResponse
    {
        public int ReportId { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public int ReportTypeId { get; set; }
        public string ReportTypeName { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
