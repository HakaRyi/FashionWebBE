using System;
using System.Collections.Generic;

namespace Repositories.Entities;

public partial class ExpertProfile
{
    public int ExpertProfileId { get; set; }

    public int AccountId { get; set; }

    public string? ExpertiseField { get; set; }

    public int? YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public bool? Verified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ExpertFile? ExpertFile { get; set; }
}
