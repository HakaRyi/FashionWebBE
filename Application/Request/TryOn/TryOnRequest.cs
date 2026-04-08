using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Request.TryOn
{
    public class TryOnRequest
    {
        [FromForm(Name = "model_image")]
        public IFormFile ModelImage { get; set; }

        // Map key "cloth_image" từ Postman vào biến ClothImage
        // Lưu ý: Mình để là "cloth_image" (không có chữ e) cho khớp với code Python của bạn
        [FromForm(Name = "cloth_image")]
        public IFormFile ClothImage { get; set; }
        [FromForm(Name = "category")]
        public int? Category { get; set; }
    }
}
