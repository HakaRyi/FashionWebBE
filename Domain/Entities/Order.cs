using Domain.Constants;

namespace Domain.Entities
{
    public partial class Order
    {
        public int OrderId { get; set; }

        public int BuyerId { get; set; }

        public int SellerId { get; set; }

        /// <summary>
        /// Optional code to show to users instead of raw OrderId.
        /// </summary>
        public string? OrderCode { get; set; }

        public decimal SubTotal { get; set; }

        public decimal ServiceFee { get; set; }

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = OrderStatus.PendingPayment;

        public string? Note { get; set; }

        public string? CancelReason { get; set; }

        public string? ShippingAddress { get; set; }

        public string? ReceiverName { get; set; }

        public string? ReceiverPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public virtual Account Buyer { get; set; } = null!;

        public virtual Account Seller { get; set; } = null!;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public virtual EscrowSession? EscrowSession { get; set; }

        public virtual RefundRequest? RefundRequest { get; set; }
    }
}