using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Services.Request.GroupReq
{
    public class GroupRequest
    {
        public string? Name { get; set; }
        public IFormFile? Avatar { get; set; }
        public List<int>? MemberIds { get; set; }
    }
    public class EditGroupRequest
    {
        public string? Name { get; set; }
        public IFormFile? Avatar { get; set; }
    }
}
