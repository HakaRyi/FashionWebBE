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

    public class ExpertRegistrationDto
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string Style { get; set; }
        public string Social { get; set; }
        public string Bio { get; set; }
        public string? PortfolioUrl { get; set; }
        public string EvidenceType { get; set; }
    }
}
