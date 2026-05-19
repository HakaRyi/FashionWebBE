using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Response.OrderResp
{
    public class OrderAdminResponse
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string BuyerName { get; set; } = null!;
        public string SellerName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class OrderAdminListPagedResponse
    {
        public List<OrderAdminResponse> Orders { get; set; } = new List<OrderAdminResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
