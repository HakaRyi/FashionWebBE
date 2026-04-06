namespace Domain.Contracts.UserReport
{
    public class ReportTypeDto
    {
        public int ReportTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}