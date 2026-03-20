using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.PaymentReq
{
    public class CreateOrderRequest
    {
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
