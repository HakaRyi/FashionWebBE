using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Request.ExpertReq
{
    public class ExpertRequest
    {
        [Required]
        public string Style { get; set; }
        public string? StyleAesthetic { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }

        public string? EvidenceType { get; set; }
        public string? PortfolioUrl { get; set; }
        public IFormFile? File { get; set; }
    }

    public class ExpertRegistrationDto
    {
        public string? Style { get; set; }
        public string? StyleAesthetic { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Bio { get; set; }
        public string? EvidenceType { get; set; }
        public string? PortfolioUrl { get; set; }
    }

    public class ExpertProcessDto
    {
        public int FileId { get; set; }
        public string Status { get; set; }
        public string? Reason { get; set; }
    }
}
