namespace Services.Request.OrderReq
{
    public class CreateOrderRequest
    {
        public int BuyerId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ServiceFee { get; set; }
        public string? Note { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public List<CreateOrderDetailRequest> Details { get; set; } = new();
    }
}
