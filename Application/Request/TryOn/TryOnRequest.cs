using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Request.TryOn
{
    public class TryOnRequest
    {
        [FromForm(Name = "model_image")]
        public IFormFile ModelImage { get; set; }

        [FromForm(Name = "cloth_image")]
        public IFormFile ClothImage { get; set; }
        [FromForm(Name = "category")]
        public int? Category { get; set; }
    }
}