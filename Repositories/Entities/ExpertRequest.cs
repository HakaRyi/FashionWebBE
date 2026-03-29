using System.Text.Json.Serialization;

namespace Repositories.Entities;

public partial class ExpertRequest
{
    public int ExpertFileId { get; set; }

    public int ExpertProfileId { get; set; }

    public string? ExpertiseField { get; set; }

    public string? StyleAesthetic { get; set; }

    public int? YearsOfExperience { get; set; }

    public string? Bio { get; set; }

    public string? CvUrl { get; set; }

    public string? CertificateUrl { get; set; }

    public string? LicenseUrl { get; set; }

    public string? IdentityProofUrl { get; set; }

    public string? Status { get; set; }

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    [JsonIgnore]
    public virtual ExpertProfile ExpertProfile { get; set; } = null!;
}
