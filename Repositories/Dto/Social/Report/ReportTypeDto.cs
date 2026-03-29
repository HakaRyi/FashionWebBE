namespace Repositories.Dto.Social.Report
{
    public class ReportTypeDto
    {
        public int ReportTypeId { get; set; }

        public string TypeName { get; set; } = null!;

        public string? Description { get; set; }
    }
}