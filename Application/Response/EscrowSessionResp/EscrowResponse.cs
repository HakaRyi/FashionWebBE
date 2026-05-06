using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.EscrowSessionResp
{
    public class EscrowResponse
    {
        public int EscrowSessionId { get; set; }
        public int? EventId { get; set; }
        public string? EventTitle { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public int? ReceiverId { get; set; }
        public string? ReceiverName { get; set; }
        public decimal Amount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal FinalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
