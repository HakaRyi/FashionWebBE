namespace Domain.Entities
{
    public partial class EscrowSession
    {
        public int EscrowSessionId { get; set; }
        public int? OrderId { get; set; }
        public int? EventId { get; set; }

        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }

        public decimal Amount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal FinalAmount => Amount - ServiceFee;

        public string Status { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public virtual Account Sender { get; set; } = null!;
        public virtual Account? Receiver { get; set; }
        public virtual Order? Order { get; set; }
        public virtual Event? Event { get; set; }
    }
}
