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
