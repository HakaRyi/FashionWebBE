using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.ExpertRatingReq
{
    public class ExpertRatingRequest
    {
        public int PostId { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }
    }
}
