using Microsoft.AspNetCore.Http;

namespace Services.Request.OufitReq
{
    public class AddOutfitRequest
    {
        public string OutfitName { get; set; } = null!;
        public IFormFile Image { get; set; } = null!;
    }
}
