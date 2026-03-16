namespace Repositories.Dto.Social.Report
{
    public class UpdateReportStatusDto
    {
        public string Status { get; set; } = null!;

        public string? AdminNote { get; set; }
    }
}