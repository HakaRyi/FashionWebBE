using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.AccountReq
{
    public class VerifyRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
