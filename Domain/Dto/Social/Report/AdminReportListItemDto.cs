namespace Domain.Dto.Social.Report
{
    public class AdminReportListItemDto
    {
        public int UserReportId { get; set; }
        public int PostId { get; set; }

        public int ReportedByAccountId { get; set; }
        public string ReportedByUserName { get; set; } = "";

        public int PostOwnerAccountId { get; set; }
        public string PostOwnerUserName { get; set; } = "";

        public string? PostStatus { get; set; }
        public string? PostVisibility { get; set; }

        public int ReportTypeId { get; set; }
        public string? ReportTypeName { get; set; }

        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = null!;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? AdminNote { get; set; }
    }
}