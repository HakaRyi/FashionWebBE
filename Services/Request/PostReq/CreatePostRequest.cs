using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.PostReq
{
    public class CreatePostRequest
    {
        public int AccountId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public int? EventId { get; set; }
        public List<IFormFile>? Images { get; set; } // Nhận file ảnh
    }
}
