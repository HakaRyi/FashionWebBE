using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.ExpertReq
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
}
