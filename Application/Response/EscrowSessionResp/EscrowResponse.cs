namespace Application.Response.EscrowSessionResp
{
    public class EscrowResponse
    {
        public int EscrowSessionId { get; set; }
        public int? EventId { get; set; }
        public string? EventTitle { get; set; }
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
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
        public List<EscrowHistoryDto> StatusHistories { get; set; } = new();
    }

    public class EscrowHistoryDto
    {
        public int EscrowStatusHistoryId { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public decimal AmountBefore { get; set; }
        public decimal AmountAfter { get; set; }
        public string? Reason { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}