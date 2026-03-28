using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.ExpertResp
{
    public class ExpertResponse
    {
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

}
