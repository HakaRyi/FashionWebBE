namespace Repositories.Entities;

public partial class ReportType
{
    public int ReportTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();
}
