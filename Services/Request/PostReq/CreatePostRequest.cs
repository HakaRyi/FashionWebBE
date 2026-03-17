using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Services.Request.PostReq
{
    public class CreatePostRequest
    {
        public string? Content { get; set; }
        public bool IsPublic { get; set; }
        public int? EventId { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}
