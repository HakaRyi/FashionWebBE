using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class EscrowStatusHistory
    {
        public int EscrowStatusHistoryId { get; set; }

        public int EscrowSessionId { get; set; }

        public string FromStatus { get; set; } = null!;

        public string ToStatus { get; set; } = null!;

        public decimal AmountBefore { get; set; }

        public decimal AmountAfter { get; set; }

        public string? Reason { get; set; }

        public int? ChangedById { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public virtual EscrowSession EscrowSession { get; set; } = null!;

        public virtual Account? ChangedBy { get; set; }
    }
}
