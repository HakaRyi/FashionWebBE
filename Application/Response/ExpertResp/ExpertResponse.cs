namespace Application.Response.ExpertResp
{
    public class ExpertResponse
    {
    }

    public class ExpertManagementByAdminDto
    {
        public int ExpertProfileId { get; set; }
        public int AccountId { get; set; }
        public string? UserName { get; set; }
        public string? ExpertiseField { get; set; }
        public double? RatingAvg { get; set; }
        public int? ReputationScore { get; set; }
        public string? Bio { get; set; }
        public string? StyleAesthetic { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? Verified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ExpertFileByAdminDto> ExpertRequests { get; set; } = new List<ExpertFileByAdminDto>();
    }

    public class ExpertManagementDto
    {
        public int ExpertProfileId { get; set; }
        public int AccountId { get; set; }
        public string? UserName { get; set; }
        public string? ExpertiseField { get; set; }
        public double? RatingAvg { get; set; }
        public int? ReputationScore { get; set; }
        public string? Bio { get; set; }
        public string? StyleAesthetic { get; set; }
        public int? YearsOfExperience { get; set; }
        public bool? Verified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public ExpertFileDto? ExpertFile { get; set; }
    }

    public class ExpertFileDto
    {
        public int ExpertFileId { get; set; }
        public string? Status { get; set; }
        public string? CertificateUrl { get; set; }
        public string? LicenseUrl { get; set; }
        public string? Reason { get; set; }
    }

    public class ExpertFileByAdminDto
    {
        public int ExpertFileId { get; set; }
        public string? Status { get; set; }
        public string? CertificateUrl { get; set; }
        public string? LicenseUrl { get; set; }
        public string? Reason { get; set; }

        public int ExpertProfileId { get; set; }

        public string? ExpertiseField { get; set; }

        public string? StyleAesthetic { get; set; }

        public int? YearsOfExperience { get; set; }

        public string? Bio { get; set; }

        public string? CvUrl { get; set; }

        public string? IdentityProofUrl { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }

    public class ExpertApplicationStatusDto
    {
        public string Status { get; set; } = "None";
        public string? Reason { get; set; }
        public DateTime? ProcessedAt { get; set; }

        // Thêm các trường này để Frontend ReviewModal hiển thị
        public string? Style { get; set; }
        public string? StyleAesthetic { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public string? PortfolioUrl { get; set; }
    }
}
