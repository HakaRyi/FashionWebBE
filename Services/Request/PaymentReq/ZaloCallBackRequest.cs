using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.PaymentReq
{
    public class ZaloCallbackRequest
    {
        public string data { get; set; } = string.Empty;
        public string mac { get; set; } = string.Empty;
    }
}
