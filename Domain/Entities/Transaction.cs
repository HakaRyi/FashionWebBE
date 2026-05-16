namespace Domain.Entities;

public partial class Transaction
{
    public int TransactionId { get; set; }
    public int WalletId { get; set; }
    public int? PaymentId { get; set; }
    public string TransactionCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Type { get; set; } = null!;
    public string ReferenceType { get; set; } = null!;
    public int? ReferenceId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = null!;
    public virtual Wallet Wallet { get; set; } = null!;
    public virtual Payment? Payment { get; set; }
}
