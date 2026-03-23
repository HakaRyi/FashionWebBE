namespace Services.Request.PaymentReq
{
    public class CreateOrderRequest
    {
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
