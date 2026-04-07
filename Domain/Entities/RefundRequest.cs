using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public partial class RefundRequest
    {
        public int RefundRequestId { get; set; }
        public int OrderId { get; set; }

        public string Reason { get; set; } = null!;
        public string ProofImage1 { get; set; } = null!;
        public string ProofImage2 { get; set; } = null!;

        public string Status { get; set; } = "PENDING";
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
