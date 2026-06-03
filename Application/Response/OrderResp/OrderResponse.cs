namespace Application.Response.OrderResp
{
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public string? OrderCode { get; set; }

        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = "Unknown";

        public int SellerId { get; set; }
        public string SellerName { get; set; } = "Unknown";

        public decimal SubTotal { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = null!;
        public string? Note { get; set; }
        public string? CancelReason { get; set; }

        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public List<OrderStatusHistoryResponse> StatusHistories { get; set; } = new();
        public List<OrderDetailResponse> OrderDetails { get; set; } = new();
    }

    public class OrderStatusHistoryResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = null!;
        public DateTime ChangedAt { get; set; }
        public string ActorType { get; set; } = null!;
        public int? ChangedById { get; set; }
        public string? Note { get; set; }
    }
}