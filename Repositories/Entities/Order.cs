using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Entities
{
    public partial class Order
    {
        public int OrderId { get; set; }

        public int BuyerId { get; set; }
        public int SellerId { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ServiceFee { get; set; } 
        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
        public string? Note { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public virtual Account Buyer { get; set; } = null!;
        public virtual Account Seller { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual EscrowSession? EscrowSession { get; set; }
    }
}
