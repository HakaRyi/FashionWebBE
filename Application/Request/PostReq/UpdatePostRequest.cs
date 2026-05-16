using Microsoft.AspNetCore.Http;

namespace Application.Request.PostReq
{
    public class UpdatePostRequest
    {
        public string? Tittle { get; set; }
        public string? Content { get; set; }
        public List<IFormFile>? Images { get; set; }
        public bool? IsExpertPost { get; set; }
        public bool? IsPublish { get; set; }
    }
}
