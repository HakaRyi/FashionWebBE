using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.WalletReq
{
    public class WalletRequest
    {
    }

    public class TopUpRequest
    {
        public decimal Amount { get; set; }
        public string OrderCode { get; set; } = null!;
        public string Provider { get; set; } = null!;
    }
}
