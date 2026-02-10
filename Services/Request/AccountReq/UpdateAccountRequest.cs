using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.AccountReq
{
    public class UpdateAccountRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public int Role { get; set; } 
        public string? Status { get; set; } 
    }
}
