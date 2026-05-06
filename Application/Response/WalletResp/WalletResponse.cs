namespace Application.Response.WalletResp
{
    public class WalletResponse
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public string? Currency { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    public class WalletDashboardResponse
    {
        public WalletSummaryDto Wallet { get; set; } = null!;
        public List<WalletTransactionDto> Transactions { get; set; } = new();
    }

    public class WalletSummaryDto
    {
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public string Currency { get; set; } = null!;
    }

    public class WalletTransactionDto
    {
        public int TransactionId { get; set; }
        public string TransactionCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Type { get; set; }           // Credit/Debit
        public string ReferenceType { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? PaymentProvider { get; set; }
    }

}

