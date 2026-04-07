using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Request.OrderReq
{
    public class CreateRefundRequest
    {
        public string Reason { get; set; } = null!;
        public string ProofImage1 { get; set; } = null!;
        public string ProofImage2 { get; set; } = null!;
    }
}
