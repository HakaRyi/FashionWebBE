using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.PostReq
{
    public class UpdatePostRequest
    {
        public string Tittle { get; set; }
        public string Content { get; set; }
        public List<IFormFile>? Images { get; set; }
        public bool IsExpertPost { get; set; }
    }
}
