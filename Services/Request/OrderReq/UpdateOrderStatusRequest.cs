using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.OrderReq
{
    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = null!;
    }
}
