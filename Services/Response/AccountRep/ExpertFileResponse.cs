namespace Services.Response.AccountRep
{
    public class ExpertFileResponse
    {
        public int ExpertFileId { get; set; }

        public int ExpertProfileId { get; set; }

        public string? CertificateUrl { get; set; }

        public string? LicenseUrl { get; set; }

        public string? Bio { get; set; }

        public double? RatingAvg { get; set; }

        public int? ExperienceYears { get; set; }

        public bool? Verified { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
