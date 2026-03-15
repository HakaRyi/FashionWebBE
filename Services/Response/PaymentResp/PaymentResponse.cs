using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Response.PaymentResp
{
    public class PaymentResponse
    {
        public string OrderCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
