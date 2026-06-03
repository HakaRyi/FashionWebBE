using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.EscrowStatusHistoryResp
{
    public class EscrowStatusHistoryResponse
    {
        public int EscrowStatusHistoryId { get; set; }
        public int EscrowSessionId { get; set; }
        public string FromStatus { get; set; } = null!;
        public string ToStatus { get; set; } = null!;
        public decimal AmountBefore { get; set; }
        public decimal AmountAfter { get; set; }
        public string? Reason { get; set; }
        public int? ChangedById { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
