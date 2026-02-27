using Microsoft.AspNetCore.Http;

namespace Services.Request.ImageReq
{
    public class UploadAvatarRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
