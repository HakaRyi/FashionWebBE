using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.PrizeReq
{
    public class PrizeRequest
    {
        public int Ranked { get; set; }
        public decimal RewardAmount { get; set; }
    }
}
