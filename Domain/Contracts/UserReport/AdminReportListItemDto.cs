namespace Domain.Contracts.UserReport
{
    public class AdminReportListItemDto
    {
        public int UserReportId { get; set; }
        public int PostId { get; set; }

        public int ReportedByAccountId { get; set; }
        public string ReportedByUserName { get; set; } = string.Empty;

        public int PostOwnerAccountId { get; set; }
        public string PostOwnerUserName { get; set; } = string.Empty;

        public string? PostStatus { get; set; }
        public string? PostVisibility { get; set; }

        public string? ThumbnailUrl { get; set; }

        public int ReportTypeId { get; set; }
        public string ReportTypeName { get; set; } = string.Empty;

        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime? ReviewedAt { get; set; }
        public int? ReviewedBy { get; set; }
        public string? AdminNote { get; set; }
    }
}