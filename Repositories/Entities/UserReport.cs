using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class UserReport
{
    public int UserReportId { get; set; }

    public int PostId { get; set; }

    public int AccountId { get; set; }

    public int ReportTypeId { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Post Post { get; set; } = null!;

    public virtual ReportType ReportType { get; set; } = null!;
}
