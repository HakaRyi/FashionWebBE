namespace Domain.Entities
{
    public partial class Wallet
    {
        public int WalletId { get; set; }
        public int AccountId { get; set; }
        public decimal Balance { get; set; }
        public decimal LockedBalance { get; set; }
        public string Currency { get; set; } = "VND";
        public decimal? MonthlySpendingLimit { get; set; }
        public bool IsHardSpendingLimit { get; set; } = false;
        public decimal SpendingWarningThresholdPercent { get; set; } = 80;
        public DateTime UpdatedAt { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
