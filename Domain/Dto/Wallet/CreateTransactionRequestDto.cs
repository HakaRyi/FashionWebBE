namespace Domain.Dto.Wallet
{
    public class CreateTransactionRequestDto
    {
        public int WalletId { get; set; }
        public int? PaymentId { get; set; }

        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }

        public string Type { get; set; } = null!;
        public string ReferenceType { get; set; } = null!;
        public int? ReferenceId { get; set; }

        public string? Description { get; set; }
        public string Status { get; set; } = null!;
    }
}