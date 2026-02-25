namespace Services.Response.TransactionResp
{
    public class TransactionResponse
    {
        public int TransactionId { get; set; }

        public int AccountId { get; set; }
        public string AccountName { get; set; }

        public int PaymentId { get; set; }

        public int AmountCoin { get; set; }

        public string? Type { get; set; }

        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public int? BalanceAfter { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string? Status { get; set; }
    }
}
