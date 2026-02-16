using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Request.TryOn
{
    public class TryOnRequest
    {
        [FromForm(Name = "model_image")]
        public IFormFile ModelImage { get; set; }

        // Map key "cloth_image" từ Postman vào biến ClothImage
        // Lưu ý: Mình để là "cloth_image" (không có chữ e) cho khớp với code Python của bạn
        [FromForm(Name = "cloth_image")]
        public IFormFile ClothImage { get; set; }
    }
}
