namespace Repositories.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int AccountId { get; set; }

    public decimal Amount { get; set; }

    public string? Provider { get; set; }

    public string? OrderCode { get; set; }

    public string? Status { get; set; }

    public string? ExternalTransactionId { get; set; } // mã giao dịch từ cổng thanh toán

    public DateTime? CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public virtual Account Account { get; set; } = null!;

    public virtual Transaction? Transaction { get; set; }
}
