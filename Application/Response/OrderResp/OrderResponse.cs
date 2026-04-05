namespace Application.Response.OrderResp
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = null!;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public decimal SubTotal { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderDetailResponse> OrderDetails { get; set; } = new List<OrderDetailResponse>();
    }
}
