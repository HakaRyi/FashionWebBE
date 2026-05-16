using Domain.Constants;

namespace Domain.Entities
{
    public partial class UserReport
    {
        public int UserReportId { get; set; }

        public int PostId { get; set; }

        public int AccountId { get; set; }

        public int ReportTypeId { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = ReportStatus.Pending;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedBy { get; set; }

        public string? AdminNote { get; set; }

        public virtual Account Account { get; set; } = null!;

        public virtual Post Post { get; set; } = null!;

        public virtual ReportType ReportType { get; set; } = null!;
    }
}