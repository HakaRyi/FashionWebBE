namespace Domain.Dto.Social.Report
{
    public class PostReportValidationInfoDto
    {
        public int PostId { get; set; }

        public int AccountId { get; set; }

        public string? Status { get; set; }

        public string? Visibility { get; set; }
    }
}