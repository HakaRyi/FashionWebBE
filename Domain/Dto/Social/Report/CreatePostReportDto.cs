namespace Domain.Dto.Social.Report
{
    public class CreatePostReportDto
    {
        public int ReportTypeId { get; set; }

        public string? Reason { get; set; }
    }
}