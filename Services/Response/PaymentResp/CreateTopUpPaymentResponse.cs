namespace Application.Response.PaymentResp
{
    public class CreateTopUpPaymentResponse
    {
        public int PaymentId { get; set; }
        public string OrderCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Provider { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? PaymentUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}