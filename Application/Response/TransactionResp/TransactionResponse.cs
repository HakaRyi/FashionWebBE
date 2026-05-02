namespace Application.Response.TransactionResp
{
    public class TransactionResponse
    {
        public int TransactionId { get; set; }
        public int WalletId { get; set; }
        public string? UserName { get; set; }
        public int? PaymentId { get; set; }
        public string TransactionCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? Type { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
    }
}
