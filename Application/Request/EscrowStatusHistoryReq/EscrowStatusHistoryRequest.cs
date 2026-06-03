using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Request.EscrowStatusHistoryReq
{
    public class EscrowStatusHistoryRequest
    {
    }

    public class CreateEscrowStatusHistoryRequest
    {
        [Required]
        public int EscrowSessionId { get; set; }
        [Required]
        public string FromStatus { get; set; } = null!;
        [Required]
        public string ToStatus { get; set; } = null!;
        [Required]
        public decimal AmountBefore { get; set; }
        [Required]
        public decimal AmountAfter { get; set; }
        public string? Reason { get; set; }
        public int? ChangedById { get; set; }
    }
}
