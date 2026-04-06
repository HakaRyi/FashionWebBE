namespace Application.Request.OrderReq
{
    public class CreateOrderDetailRequest
    {
        public int? ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? ItemName { get; set; }
    }
}
