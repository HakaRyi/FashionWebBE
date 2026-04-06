using Microsoft.AspNetCore.Http;

namespace Application.Request.OufitReq
{
    public class AddOutfitRequest
    {
        public string OutfitName { get; set; } = null!;
        public IFormFile Image { get; set; } = null!;
    }
}
