namespace Application.Response.PaymentResp
{
    public class PaymentCallbackResultResponse
    {
        public string OrderCode { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime ProcessedAt { get; set; }
    }
}