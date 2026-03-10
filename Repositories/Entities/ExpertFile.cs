namespace Repositories.Entities;

public partial class ExpertFile
{
    public int ExpertFileId { get; set; }

    public int ExpertProfileId { get; set; }

    public string? CvUrl { get; set; }

    public string? CertificateUrl { get; set; }

    public string? LicenseUrl { get; set; }

    public string? IdentityProofUrl { get; set; }

    public double? RatingAvg { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ExpertProfile ExpertProfile { get; set; } = null!;
}
