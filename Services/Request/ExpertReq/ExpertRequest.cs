using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.ExpertReq
{
    public class ExpertRequest
    {
        public int AccountId { get; set; }
        public string Style { get; set; }
        public string Bio { get; set; }
        public string EvidenceType { get; set; }
        public string? PortfolioUrl { get; set; }
        public IFormFile? File { get; set; }
    }
}
