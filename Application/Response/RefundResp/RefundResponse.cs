using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.RefundResp
{
    public class RefundRequestResponse
    {
        public int RefundRequestId { get; set; }
        public int OrderId { get; set; }
        public string Reason { get; set; } = null!;
        public string ProofImage1 { get; set; } = null!;
        public string ProofImage2 { get; set; } = null!;
        public string? ItemImage { get; set; }
        public string Status { get; set; } = null!;
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
