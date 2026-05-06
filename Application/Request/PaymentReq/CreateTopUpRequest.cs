namespace Application.Request.PaymentReq
{
    public class CreateTopUpRequest
    {
        public decimal Amount { get; set; }
        public string? Source { get; set; }
    }
}