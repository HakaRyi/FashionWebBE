using Microsoft.AspNetCore.Http;

namespace Application.Request.ImageReq
{
    public class UploadAvatarRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
