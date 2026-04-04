namespace Application.Response.OrderResp
{
    public class OrderPaymentActionResponse
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Message { get; set; } = null!;
        public DateTime ProcessedAt { get; set; }
    }
}