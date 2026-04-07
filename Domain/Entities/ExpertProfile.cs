namespace Domain.Entities;

public partial class ExpertProfile
{
    public int ExpertProfileId { get; set; }

    public int AccountId { get; set; }

    public string? ExpertiseField { get; set; }

    public string? StyleAesthetic { get; set; }

    public int? YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public bool? Verified { get; set; }

    public double? RatingAvg { get; set; }

    public int? ReputationScore { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<ExpertRequest> ExpertRequests { get; set; } = new List<ExpertRequest>();
    public virtual ICollection<ReputationHistory> ReputationHistories { get; set; } = new List<ReputationHistory>();
}
