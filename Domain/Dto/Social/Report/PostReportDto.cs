namespace Domain.Dto.Social.Report
{
    public class PostReportDto
    {
        public int UserReportId { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public int ReportTypeId { get; set; }

        public string ReportTypeName { get; set; } = null!;

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = null!;
    }
}